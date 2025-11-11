using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using WebApplication5.Managers;
using WebApplication5.Model;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class RemoveFileController : Controller
	{
		JsonResult result = null;
		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter2 = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2, api_key, secretKeyString;
		DataSet ds = new DataSet();
		DataTable dt = new DataTable();
		DataTable dt2 = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1, myConnectionString2, strWeb;
		public string soapEndpoint;
		private readonly ILogger<LoginController> _logger;
		public RemoveFileController(ILogger<LoginController> logger)
		{
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			strWeb = _configuration.GetConnectionString("strWeb");
			api_key = _configuration.GetConnectionString("api_key");
			secretKeyString = _configuration.GetConnectionString("secretKeyString");
			soapEndpoint = _configuration.GetConnectionString("soapEndpoint");
			_logger = logger;
		}

		[HttpGet]
		public async Task<string> Main([FromBody] RemoveFileModel RemoveFileModel)
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

			string json = String.Empty;
			Error Error = new Error();
			var jsonReturn = "";

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
				try
				{
					ValidateCenter ValidateCenter = new ValidateCenter();
					bool chk_record_contract_file = ValidateCenter.CHK_RECORD_CONTRACT_FILE(RemoveFileModel.contract, RemoveFileModel.record_call_id, RemoveFileModel.attach_ref_id);

					if (!chk_record_contract_file)
					{
						Error.StatusCode = "4033";
						Error.Message = "Record_call_id, Contract and File do not match.";
						json = JsonConvert.SerializeObject(Error);
						var jsonParsed = JObject.Parse(json);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						json = jsonParsed.ToString();

						return json;
					}


					strConnString2 = myConnectionString2;

					connection.ConnectionString = strConnString2;
					connection.Open();

					strSQL = "ARM_DELETE_TRACK_FILE";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.CommandType = CommandType.StoredProcedure;

					//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
					sqlCommand.Parameters.AddWithValue("ARM_TRACK_FILE_ID", RemoveFileModel.attach_ref_id);

					dtAdapter2.SelectCommand = sqlCommand;

					dtAdapter2.Fill(dt2);
					connection.Close();


					string filePath = @"FileUpload\"+ dt2.Rows[0]["FILE_NAMES"].ToString();
					if (System.IO.File.Exists(filePath))
					{
						//System.IO.File.Delete(filePath);
						Error.StatusCode = "200";
						Error.Message = "Success.";
						jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(jsonReturn);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed.ToString();
					}
					else
					{
						Error.StatusCode = "500";
						Error.Message = "Internal Server Error.";
						jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(jsonReturn);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed.ToString();
					}

				}
				catch (Exception ex)
				{
					Error.StatusCode = "500";
					Error.Message = "Internal Server Error.";
					jsonReturn = JsonConvert.SerializeObject(Error);

					var jsonParsed = JObject.Parse(jsonReturn);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					jsonReturn = jsonParsed.ToString();

				}
			}
			return jsonReturn;
		}
	}
}
