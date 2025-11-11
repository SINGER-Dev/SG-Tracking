using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using WebApplication5.Managers;
using WebApplication5.Model;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace WebApplication5.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class MasterRoleController : ControllerBase
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2;
		DataTable dt = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1, myConnectionString2, myConnectionString3;
		public string ApplicationID;

		private readonly ILogger<LoginController> _logger;

		public MasterRoleController(ILogger<LoginController> logger)
		{
			this.secretKey = secretKey;
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			myConnectionString3 = _configuration.GetConnectionString("strConnString3");
			ApplicationID = _configuration.GetConnectionString("ApplicationID");
			_logger = logger;
		}

		[HttpGet]
		public string Main([FromBody] MasterRoleParameter MasterRoleParameter)
		{
			string accessToken = Request.Headers["Authorization"];
			if (string.IsNullOrEmpty(accessToken))
			{
				// Try X-Authorization header
				if (Request.Headers.ContainsKey("X-Authorization"))
				{
					accessToken = Request.Headers["X-Authorization"].FirstOrDefault();
				}
			}
			IAuthService expirationChecker = new JWTService();
			bool isExpired = expirationChecker.IsTokenExpired(accessToken);

			if (isExpired)
			{
				Error error = new Error();
				error.StatusCode = "4011";
				error.Message = "Token has been revoked.";
				string json = JsonConvert.SerializeObject(error);
				var jsonParsed = JObject.Parse(json);
				jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
				json = jsonParsed.ToString();
				return json;
			}
			else
			{
				strConnString = myConnectionString2;
				try
				{
                    connection.ConnectionString = myConnectionString3;
					connection.Open();


					string team = "";
					if (MasterRoleParameter.team.ToString().Trim() != "")
					{
						team = " AND [Auth_Roles].RoleName like '%" + MasterRoleParameter.team.ToString().Trim() + "%'";
					}

					string company = "";
					if (MasterRoleParameter.company.ToString().Trim() != "")
					{
						company = " AND E.COMPANY = '" + MasterRoleParameter.company.ToString().Trim() + "'";
					}

					strSQL = "  SELECT [Auth_Roles].RoleID,[Auth_Roles].RoleName,[Auth_Roles].RoleDescription" +
							 "  FROM [SG-AUTHORIZE].[dbo].[Auth_UserRoles] "+
							 "  LEFT JOIN [SG-AUTHORIZE].[dbo].[Auth_Roles] ON [Auth_Roles].RoleID = [Auth_UserRoles].RoleID "+
							 "  LEFT JOIN [SG-AUTHORIZE].[dbo].[Auth_Users] ON [Auth_Users].UserID = [Auth_UserRoles].UserID "+
							 "  LEFT JOIN [SG-MASTER].[dbo].[MS_EMPLOYEE_ALL_COMP] E ON [Auth_Users].EMP_CODE = E.EMP_CODE "+
							 "  WHERE [Auth_UserRoles].[ApplicationID] = @ApplicationID " +
							 company +
							 team +
							 "  GROUP BY [Auth_Roles].RoleID,[Auth_Roles].RoleName,[Auth_Roles].RoleDescription " +
							 "  ORDER BY [Auth_Roles].RoleName ASC";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.CommandType = CommandType.Text;

					sqlCommand.Parameters.Add("@ApplicationID", SqlDbType.Int);
					sqlCommand.Parameters["@ApplicationID"].Value = ApplicationID;

					//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
					dtAdapter.SelectCommand = sqlCommand;

					dtAdapter.Fill(dt);
					connection.Close();
					if (dt.Rows.Count > 0)
					{
						MasterRole MasterRole = new MasterRole();
						MasterRole.StatusCode = "200";
						MasterRole.Message = "Success.";

						List<MasterRoleList> MasterRoleListMaster = new List<MasterRoleList>();
						foreach (DataRow row in dt.Rows)
						{

							MasterRoleList MasterRoleList = new MasterRoleList();
							string[] RoleName = row["RoleName"].ToString().Split('-');
							if (RoleName.Length > 1)
							{
								MasterRoleList.role = RoleName[1].ToUpper();
							}
							else
							{
								MasterRoleList.role = row["RoleName"].ToString().ToUpper();
							}

							MasterRoleList.description = row["RoleDescription"].ToString();
							MasterRoleListMaster.Add(MasterRoleList);
						}
						MasterRole.Payload = MasterRoleListMaster;
						string json = JsonConvert.SerializeObject(MasterRole);

						return json;
					}
					else
					{
						Error error = new Error();
						error.StatusCode = "4044";
						error.Message = "Data not found.";
						string json = JsonConvert.SerializeObject(error);
						var jsonParsed = JObject.Parse(json);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						json = jsonParsed.ToString();
						return json;
					}
				}
				catch (Exception ex)
				{
					Error error = new Error();
					error.StatusCode = "500";
					error.Message = "Internal Server Error.";
					string json = JsonConvert.SerializeObject(error);

                    _logger.LogError("Error: " + ex.Message + " StackTrace: " + ex.StackTrace);
                    var jsonParsed = JObject.Parse(json);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					json = jsonParsed.ToString();
					return json;
				}
			}
		}
	}
}
