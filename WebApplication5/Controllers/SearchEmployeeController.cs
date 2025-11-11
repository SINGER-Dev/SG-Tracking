using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
using System.Text.Json;
using System.Xml.Linq;
using WebApplication5.Managers;
using WebApplication5.Model;
using System.IdentityModel.Tokens.Jwt;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class SearchEmployeeController : ControllerBase
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand , sqlCommand2;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strSQL2, strConnString2, strConnString3 , ApplicationID, secretKeyString;

		
		public string myConnectionString1;
		public string myConnectionString2 , myConnectionString3;


		private readonly ILogger<LoginController> _logger;

		public SearchEmployeeController(ILogger<LoginController> logger)
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
			secretKeyString = _configuration.GetConnectionString("secretKeyString");
			_logger = logger;
		}

		[HttpPost]
		public string Main([FromBody] SearchEmployee SearchEmployee)
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
			var jsonReturn = "";
			Error Error = new Error();

			if (isExpired)
			{

				Error.StatusCode = "4011";
				Error.Message = "Token has been revoked.";
				jsonReturn = JsonConvert.SerializeObject(Error);

				var jsonParsed = JObject.Parse(jsonReturn);
				jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
				jsonReturn = jsonParsed.ToString();

				return jsonReturn;
			}
			else
			{
				strConnString2 = myConnectionString2;
				strConnString3 = myConnectionString3;
				ValidateCenter ValidateCenter = new ValidateCenter();
				try
				{
					//if(SearchEmployee.employee_id.ToString().Trim() != "")
					//{
					//	bool chk_employee = true;
					//	List<string> data_val = new List<string>();
						
					//	bool chkCenterEmployee = ValidateCenter.CHK_USER_TRACKING(SearchEmployee.employee_id.ToString());
					//	if (!chkCenterEmployee)
					//	{
					//		chk_employee = false;
					//		data_val.Add(SearchEmployee.employee_id.ToString());
					//	}

					//	if (!chk_employee)
					//	{
					//		Error.StatusCode = "4042";
					//		Error.Message = "Employee Not Found.";
					//		jsonReturn = JsonConvert.SerializeObject(Error);

					//		var jsonParsed = JObject.Parse(jsonReturn);
					//		jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					//		jsonReturn = jsonParsed.ToString();
					//		return jsonReturn;
					//	}
					//}

					string team = "";
					if (SearchEmployee.team.ToString().Trim() != "")
					{
						team = " AND [Auth_Roles].RoleDescription like '%" + SearchEmployee.team.ToString().Trim() + "%'";
					}

					string filter_text = "";
					if (SearchEmployee.filter_text.ToString().Trim() != "")
					{
						filter_text = " AND (T2.[EMP_NAME_THA]+T2.[EMP_SUR_THA] LIKE '%" + SearchEmployee.filter_text.ToString().Trim().Replace(" ","") + "%'"
							+ " OR T2.[EMP_NAME_ENG]+T2.[EMP_SUR_ENG] LIKE '%" + SearchEmployee.filter_text.ToString().Trim().Replace(" ", "") + "%'"
							+ " )";
					}

					string emp_text = "";
					if (SearchEmployee.employee_id.ToString().Trim() != "")
					{
						emp_text = " AND [Auth_Users].[EMP_CODE] = '" + SearchEmployee.employee_id.ToString().Trim() + "'";
					}

					connection.ConnectionString = strConnString3;
					connection.Open();
					strSQL = "	SELECT * FROM ( SELECT "
							 + " T2.[COMPANY],T2.[EMP_CODE],T2.[EMP_NAME_THA] + ' ' + T2.[EMP_SUR_THA] AS EMP_NAME "
							 + " ,T2.[POSITION],T2.[EMP_CARD_ID] "
							 + " ,T2.[EMP_MOBILE_NO] "
							 + " ,T2.[EMP_NAME_ENG],T2.[EMP_SUR_ENG],T2.[EMP_NAME_THA],T2.[EMP_SUR_THA] "
							 + " ,STUFF((SELECT ',' + RoleDescription "
													+ " FROM [SG-AUTHORIZE].[dbo].[Auth_UserRoles] "
													+ " LEFT JOIN [SG-AUTHORIZE].[dbo].[Auth_Roles] ON [Auth_Roles].[RoleID] = [Auth_UserRoles].RoleID "
													+ " WHERE [Auth_UserRoles].UserID = [Auth_Users].UserID "
													+ " AND [Auth_UserRoles].Status = '1' "
													+ team
													+ " AND [Auth_UserRoles].[ApplicationID] = @ApplicationID "
													+ " FOR XML PATH('')), 1, 1, '') as RoleDescription "
							 + " FROM [SG-AUTHORIZE].[dbo].[Auth_Users] "
							 + " LEFT JOIN [SG-MASTER].[dbo].[MS_EMPLOYEE_ALL_COMP] T2 ON T2.EMP_CODE = [Auth_Users].EMP_CODE "
                             + " WHERE T2.[DEG_CODE] <> 'ลาออก' "
							 + " AND T2.[COMPANY] = @company"
							 + emp_text
							 + filter_text
							 + " ) A WHERE A.RoleDescription IS NOT NULL ORDER BY EMP_NAME_THA ASC";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.Parameters.AddWithValue("@company", SearchEmployee.company.ToString().Trim());

                    if (!string.IsNullOrWhiteSpace(SearchEmployee.applicationID))
                    {
                        sqlCommand.Parameters.AddWithValue("@ApplicationID", SearchEmployee.applicationID.ToString().Trim());
                    }
                    else
                    {
                        sqlCommand.Parameters.AddWithValue("@ApplicationID", ApplicationID);
                    }

					sqlCommand.CommandType = CommandType.Text;
					dtAdapter3.SelectCommand = sqlCommand;
					DataTable dt2 = new DataTable();
					dtAdapter3.Fill(dt2);
					sqlCommand.Parameters.Clear();
					connection.Close();
					var encryptTextServices = new EncryptTextService();
					if (dt2.Rows.Count > 0)
					{
						bool chkCenterEmployee2 = false;
						SearchEmpReturn SearchEmpReturn = new SearchEmpReturn();
						List<DataSearchEmpReturn> DataSearchEmpReturnMaster = new List<DataSearchEmpReturn>();
						
						foreach (DataRow row in dt2.Rows)
						{
							//bool chkCenterEmployee = ValidateCenter.CHK_USER_TRACKING(row["EMP_CODE"].ToString());
                            bool chkCenterEmployee = true;

                            if (chkCenterEmployee)
							{
								chkCenterEmployee2 = true;
								List<string> list = new List<string>();


								string[] RoleDescriptions = row["RoleDescription"].ToString().Split(',');
								foreach (string RoleDescription in RoleDescriptions)
								{
									string[] arrayTXT = RoleDescription.ToString().Split('-');
									string firstTXT = arrayTXT[0].ToString();
									if (arrayTXT.Length == 2)
									{
										string secondTXT = arrayTXT[1].ToString().ToUpper();
										list.Add(firstTXT + "-" + secondTXT);
									}
									else
									{
										list.Add(firstTXT);
									}
								}

								DataSearchEmpReturn DataSearchEmpReturn = new DataSearchEmpReturn();
								DataSearchEmpReturn.Role = list.ToArray();
								DataSearchEmpReturn.employee_id = row["EMP_CODE"].ToString();
								DataSearchEmpReturn.first_name = row["EMP_NAME_ENG"].ToString();
								DataSearchEmpReturn.last_name = row["EMP_SUR_ENG"].ToString();
								DataSearchEmpReturn.first_name_th = row["EMP_NAME_THA"].ToString();
								DataSearchEmpReturn.last_name_th = row["EMP_SUR_THA"].ToString();
								DataSearchEmpReturn.mobile_no = encryptTextServices.EncryptText(row["EMP_MOBILE_NO"].ToString(), secretKeyString);

								DataSearchEmpReturnMaster.Add(DataSearchEmpReturn);


							}
							
						}

						if (!chkCenterEmployee2)
						{
							Error.StatusCode = "4042";
							Error.Message = "Employee Not Found.";
							jsonReturn = JsonConvert.SerializeObject(Error);

							var jsonParsed = JObject.Parse(jsonReturn);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							jsonReturn = jsonParsed.ToString();
						}
						else
						{
							SearchEmpReturn.StatusCode = "200";
							SearchEmpReturn.Message = "Success.";
							SearchEmpReturn.Payload = DataSearchEmpReturnMaster;
							jsonReturn = JsonConvert.SerializeObject(SearchEmpReturn);
						}
						
					}
					else
					{
						Error.StatusCode = "4042";
						Error.Message = "Employee Not Found.";
						jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(jsonReturn);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed.ToString();
						
					}



					return jsonReturn;


				}
				catch (Exception ex)
				{
					Error.StatusCode = "500";
					Error.Message = "Internal Server Error.";
					jsonReturn = JsonConvert.SerializeObject(Error);

					var jsonParsed = JObject.Parse(jsonReturn);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					jsonReturn = jsonParsed.ToString();
					return jsonReturn;
				}
			}
		}
	}
}
