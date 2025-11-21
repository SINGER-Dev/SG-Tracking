using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using WebApplication5.Managers;
using WebApplication5.Model;
using System.Reflection;

namespace WebApplication5.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private string secretKey;

        SqlConnection connection = new SqlConnection();
        SqlCommand sqlCommand;
        SqlDataAdapter dtAdapter = new SqlDataAdapter();
        SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
        String strConnString, strSQL, strConnString2, api_key, secretKeyString, strConnString3, SecretKey;
        int ApplicationID;
        DataTable dt = new DataTable();
        DataTable dt3 = new DataTable();
        public string myConnectionString1;
        public string myConnectionString2, myConnectionString3;


        private readonly ILogger<LoginController> _logger;

        public UserController(ILogger<LoginController> logger)
        {
            this.secretKey = secretKey;
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
            IConfiguration _configuration = builder.Build();
            myConnectionString1 = _configuration.GetConnectionString("strConnString1");
            myConnectionString2 = _configuration.GetConnectionString("strConnString2");
            myConnectionString3 = _configuration.GetConnectionString("strConnString3");
            _logger = logger;
        }

        [HttpPost]
        [Route("/user/add")]
        public async Task<IActionResult> AddUser(AddUserRq addUserRq)
        {
            _logger.LogDebug("AddUser called with parameters: {@AddUserRq}", addUserRq);

            try
            {
                if (string.IsNullOrWhiteSpace(addUserRq.UserName))
                {
                    return StatusCode(500,new Error
                    {
                        StatusCode = "500",
                        Message = "Required fields are missing "
                    });
                }

                // ✅ เรียกใช้ CheckUserExists ก่อน insert
                bool userExists = await CheckUserExists(addUserRq.UserName);

                if (userExists)
                {
                    return StatusCode(500,new Error  // 409 Conflict
                    {
                        StatusCode = "500",
                        Message = "Username already exists"
                    });
                }

                using (var connection = new SqlConnection(myConnectionString3))
                {
                    await connection.OpenAsync();

                    // Begin transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert User
                            string strSQL = @"INSERT INTO [Auth_Users]
                                   ([UserName], [FullName], [Password], 
                                    [Email], [EMP_CODE], [Status], [DateAdd])
                             OUTPUT INSERTED.UserID, INSERTED.UserName, INSERTED.Email
                             VALUES
                                   (@UserName, @FullName, @Password, 
                                    @Email, @EmpCode, '1', GETDATE())";

                            int newUserId = 0;
                            string userName = "";
                            string email = "";

                            using (var sqlCommand = new SqlCommand(strSQL, connection, transaction))
                            {
                                sqlCommand.Parameters.AddWithValue("@UserName", addUserRq.UserName);
                                sqlCommand.Parameters.AddWithValue("@FullName", addUserRq.FullName);
                                sqlCommand.Parameters.AddWithValue("@Password", addUserRq.UserName);
                                sqlCommand.Parameters.AddWithValue("@Email", addUserRq.Email);
                                sqlCommand.Parameters.AddWithValue("@EmpCode", addUserRq.UserName);

                                using (var reader = await sqlCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        newUserId = Convert.ToInt32(reader["UserID"]);
                                        userName = reader["UserName"].ToString();
                                        email = reader["Email"].ToString();
                                    }
                                }
                            }

                            // ✅ เพิ่ม UserApplication ถ้ามี ApplicationId
                            if (!string.IsNullOrWhiteSpace(addUserRq.ApplicationId))
                            {
                                // 1) จัดการตาราง Auth_UserApplications
                                string checkSQL = @"
                                                        SELECT COUNT(1)
                                                        FROM [Auth_UserApplications]
                                                        WHERE [UserID] = @UserID
                                                        AND [ApplicationID] = @ApplicationID";

                                using (var checkCommand = new SqlCommand(checkSQL, connection, transaction))
                                {
                                    checkCommand.Parameters.AddWithValue("@UserID", newUserId);
                                    checkCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);

                                    int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                                    if (count == 0)
                                    {
                                        // ✅ ยังไม่มี → Insert
                                        string appSQL = @"
                                                        INSERT INTO [Auth_UserApplications]
                                                                ([UserID], [ApplicationID], [Status], [DateAdd])
                                                        VALUES  (@UserID, @ApplicationID, '1', GETDATE())";

                                        using (var appCommand = new SqlCommand(appSQL, connection, transaction))
                                        {
                                            appCommand.Parameters.AddWithValue("@UserID", newUserId);
                                            appCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);

                                            await appCommand.ExecuteNonQueryAsync();
                                        }
                                    }
                                    else
                                    {
                                        // ✅ มีอยู่แล้ว → Update ให้กลับมาเป็น active
                                        string updateSQL = @"
                                                    UPDATE [Auth_UserApplications]
                                                       SET [Status]   = '1',
                                                           [DateEdit] = GETDATE()
                                                     WHERE [UserID] = @UserID
                                                       AND [ApplicationID] = @ApplicationID";

                                        using (var updateCommand = new SqlCommand(updateSQL, connection, transaction))
                                        {
                                            updateCommand.Parameters.AddWithValue("@UserID", newUserId);
                                            updateCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);

                                            await updateCommand.ExecuteNonQueryAsync();
                                        }
                                    }
                                }

                                // 2) ✅ จัดการ Auth_UserSite ถ้ามี DepCode (และเรารู้ App แล้ว)
                                if (!string.IsNullOrWhiteSpace(addUserRq.DepCode))
                                {
                                    string checkSiteSQL = @"
                                                            SELECT COUNT(1)
                                                            FROM [SG-AUTHORIZE].dbo.Auth_UserSite
                                                            WHERE [UserID] = @UserID
                                                            AND [ApplicationID] = @ApplicationID";

                                    using (var checkSiteCommand = new SqlCommand(checkSiteSQL, connection, transaction))
                                    {
                                        checkSiteCommand.Parameters.AddWithValue("@UserID", newUserId);
                                        checkSiteCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);

                                        int siteCount = Convert.ToInt32(await checkSiteCommand.ExecuteScalarAsync());

                                        if (siteCount == 0)
                                        {
                                            // ✅ ยังไม่มี → Insert
                                            string siteSQL = @"
                                                                INSERT INTO [SG-AUTHORIZE].dbo.Auth_UserSite
                                                                        ([UserID], [ApplicationID], [AreaCode], [DepCode],
                                                                         [Status], [DateAdd])
                                                                VALUES  (@UserID, @ApplicationID, @AreaCode, @DepCode,
                                                                         '1', GETDATE())";

                                            using (var siteCommand = new SqlCommand(siteSQL, connection, transaction))
                                            {
                                                siteCommand.Parameters.AddWithValue("@UserID", newUserId);
                                                siteCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);
                                                siteCommand.Parameters.AddWithValue("@AreaCode", addUserRq.AreaCode ?? ""); // ถ้าคุณมีฟิลด์นี้
                                                siteCommand.Parameters.AddWithValue("@DepCode", addUserRq.DepCode);

                                                await siteCommand.ExecuteNonQueryAsync();
                                            }
                                        }
                                        else
                                        {
                                            // ✅ มีแล้ว → Update ให้เป็น active และอัปเดต DepCode ด้วย
                                            string updateSiteSQL = @"
                                                                        UPDATE [SG-AUTHORIZE].dbo.Auth_UserSite
                                                                           SET [Status]    = '1',
                                                                               [DepCode]   = @DepCode,
                                                                               [AreaCode]  = @AreaCode,
                                                                               [DateEdit] = GETDATE()
                                                                         WHERE [UserID] = @UserID
                                                                           AND [ApplicationID] = @ApplicationID";

                                            using (var updateSiteCommand = new SqlCommand(updateSiteSQL, connection, transaction))
                                            {
                                                updateSiteCommand.Parameters.AddWithValue("@UserID", newUserId);
                                                updateSiteCommand.Parameters.AddWithValue("@ApplicationID", addUserRq.ApplicationId);
                                                updateSiteCommand.Parameters.AddWithValue("@DepCode", addUserRq.DepCode);
                                                updateSiteCommand.Parameters.AddWithValue("@AreaCode", addUserRq.AreaCode ?? "");

                                                await updateSiteCommand.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }
                                }
                            }

                            // Commit transaction
                            await transaction.CommitAsync();

                            return Ok(new
                            {
                                StatusCode = "200",
                                Message = "User added successfully",
                                UserId = newUserId,
                                UserName = userName,
                                Email = email,
                                ApplicationAdded = !string.IsNullOrWhiteSpace(addUserRq.ApplicationId)
                            });
                        }
                        catch (Exception)
                        {
                            // Rollback on error
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the actual exception for debugging
                // _logger.LogError(ex, "Error adding user");

                Error error = new Error
                {
                    StatusCode = "500",
                    Message = ex.ToString()
                };
                return StatusCode(500, error); // Return proper IActionResult
            }
        }

        [HttpPost]
        [Route("/user/verify")]
        public async Task<bool> CheckUserExists(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return false;
                }

                using (var connection = new SqlConnection(myConnectionString3))
                {
                    await connection.OpenAsync();

                    string strSQL;
                    SqlCommand sqlCommand;

                    // ถ้าส่งมาแค่ userName
                    strSQL = @"SELECT COUNT(1) 
                          FROM [Auth_Users] 
                          WHERE [UserName] = @UserName";

                    sqlCommand = new SqlCommand(strSQL, connection);
                    sqlCommand.Parameters.AddWithValue("@UserName", userName);

                    int count = Convert.ToInt32(await sqlCommand.ExecuteScalarAsync());
                    return count > 0; // true = มี user แล้ว, false = ยังไม่มี
                }
            }
            catch (Exception ex)
            {
                // Log error
                _logger?.LogError(ex, "Error checking user existence");
                throw; // หรือ return true เพื่อความปลอดภัย (ถือว่ามีแล้ว)
            }
        }
    }
}
