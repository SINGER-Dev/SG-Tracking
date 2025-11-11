using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Data;

using WebApplication5.Managers;
using WebApplication5.Model;

using Microsoft.AspNetCore.StaticFiles;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class TaskInfoController : ControllerBase
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
		public string soapEndpoint, strWebFile;
		private readonly ILogger<LoginController> _logger;
		public TaskInfoController(ILogger<LoginController> logger)
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
			strWebFile = _configuration.GetConnectionString("strWebFile");

			_logger = logger;
		}
		[HttpPost]
		public string Main([FromBody] TaskInfoModel TaskInfoModel)
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
					var encryptTextServices = new EncryptTextService();
					bool chk_contract = true;
					List<string> data2_val = new List<string>();
					bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(TaskInfoModel.contract);
					if (!chkCenterContract)
					{
						chk_contract = false;
						data2_val.Add(TaskInfoModel.contract);
					}

					if (!chk_contract)
					{
						Error.StatusCode = "4041";
						Error.Message = "Contract Not Found.";
						Error.Payload = data2_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else
					{
						strConnString2 = myConnectionString2;

						connection.ConnectionString = strConnString2;
						connection.Open();

						strSQL = "ARM_GET_TRACK_DETAIL";
						sqlCommand = new SqlCommand(strSQL, connection);
						sqlCommand.CommandType = CommandType.StoredProcedure;

						//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
						sqlCommand.Parameters.AddWithValue("ARM_ACC_NO", TaskInfoModel.contract);
						sqlCommand.Parameters.AddWithValue("ARM_RECORD_CALL_ID", TaskInfoModel.record_call_id);
						dtAdapter2.SelectCommand = sqlCommand;

						dtAdapter2.Fill(dt2);
						sqlCommand.Parameters.Clear();

						TaskInfo TaskInfo = new TaskInfo();
						Payload Payload = new Payload();
						List<Payload> PayloadMaster = new List<Payload>();
						List<attchments> AttchmentsMaster = new List<attchments>();

						if (dt2.Rows.Count > 0)
						{
							Payload.contract = dt2.Rows[0]["ARM_ACC_NO"].ToString();
							Payload.track_address = dt2.Rows[0]["TRACK_ADDRESS"].ToString();
							Payload.lat = dt2.Rows[0]["ARM_LATITUDE"].ToString();
							Payload.lng = dt2.Rows[0]["ARM_LONGITUDE"].ToString();
							Payload.plandate = dt2.Rows[0]["ARM_WORKPLAN_TIME"].ToString();
							Payload.tel = dt2.Rows[0]["ARM_CUSTOMER_TEL"].ToString();
							Payload.track_code = dt2.Rows[0]["ARM_TRACK_CODE"].ToString();
							Payload.detail = dt2.Rows[0]["ARM_RECORD_CALL_DETAIL"].ToString();
							Payload.custappointDate = dt2.Rows[0]["ARM_PAYMENT_DATE"].ToString();
							Payload.appointmentAmt = dt2.Rows[0]["ARM_PAYMENT_AMT"].ToString();
							Payload.datecollect = dt2.Rows[0]["ARM_DATE_COLLECT"].ToString();

							strSQL = "ARM_GET_TRACK_FILE";
							sqlCommand = new SqlCommand(strSQL, connection);
							sqlCommand.CommandType = CommandType.StoredProcedure;

							//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
							sqlCommand.Parameters.AddWithValue("ARM_RECORD_CALL_ID", TaskInfoModel.record_call_id);
							dtAdapter3.SelectCommand = sqlCommand;

							dtAdapter3.Fill(dt3);
							if (dt3.Rows.Count > 0)
							{

								foreach (DataRow row in dt3.Rows)
								{
									attchments attchments = new attchments();


									attchments.attach_ref_id = row["ARM_TRACK_FILE_ID"].ToString();
									attchments.file_name = row["FILE_NAME"].ToString();
									attchments.file_name2 = row["file_name_add"].ToString();
									attchments.file_desc = row["file_desc"].ToString();
									attchments.content_type = row["content_type"].ToString();
									attchments.url = strWebFile + "/TaskInfo/Attachment?encryptedText=" + row["EncryptedID"].ToString();
									attchments.file_size = row["file_size"].ToString();
									AttchmentsMaster.Add(attchments);
								}
							}
							connection.Close();
							Payload.attchments = AttchmentsMaster;
							PayloadMaster.Add(Payload);
							TaskInfo.StatusCode = "200";
							TaskInfo.Message = "Success.";
							TaskInfo.Payload = PayloadMaster;

							jsonReturn = JsonConvert.SerializeObject(TaskInfo);
						}
						else
						{
							Error.StatusCode = "4044";
							Error.Message = "Data not found.";
							jsonReturn = JsonConvert.SerializeObject(Error);

							var jsonParsed = JObject.Parse(jsonReturn);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							jsonReturn = jsonParsed.ToString();
							return jsonReturn;
						}
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
					return jsonReturn;

				}
			}
			return jsonReturn;
		}

		[HttpGet]
		[Route("Attachment")]
		public async Task<IActionResult> DownloadFile(string encryptedText)
		{
			string encodedParameter = encryptedText.Replace(" ", "+");

			var encryptTextServices = new EncryptTextService();
			string bid = encryptTextServices.DecryptText(encodedParameter.ToString(), secretKeyString);

			connection.ConnectionString = myConnectionString2;
			connection.Open();
			strSQL = " SELECT [ARM_TRACK_FILE_ID]"
					  + ",[ARM_RECORD_CALL_ID]"
					  + ",[FILE_NAME]"
					  + ",[FILE_NAME_RECORD]"
					  + ",[content_type]"
					  + ",[url]"
					  + ",[file_size]"
					  + ",[CREATED_DATE]"
					  + ",[CREATED_USER]"
					  + ",[UPDATED_DATE]"
					  + ",[UPDATED_USER]"
					  + ",[F_ACTIVE]"
					  + " FROM [AR_COLLECTION].[dbo].[ARM_M_TRACK_FILE]"
					  + " WHERE ARM_TRACK_FILE_ID = @ARM_TRACK_FILE_ID"
					  + " AND F_ACTIVE = 'Y'";
			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.Parameters.AddWithValue("@ARM_TRACK_FILE_ID", bid);
			sqlCommand.CommandType = CommandType.Text;
			dtAdapter.SelectCommand = sqlCommand;
			DataTable dt = new DataTable();
			dtAdapter.Fill(dt);
			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count > 0)
			{

				var filePath = Path.Combine(Directory.GetCurrentDirectory(), "FileUpload", dt.Rows[0]["FILE_NAME_RECORD"].ToString());
				var provider = new FileExtensionContentTypeProvider();
				if (!provider.TryGetContentType(filePath, out var contentType))
				{
					contentType = "application/octet-stream";
				}
				var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
				
				string content_type = dt.Rows[0]["content_type"].ToString();

				if (content_type == "image/png")
				{
					return new PhysicalFileResult(filePath, "image/png");
				} else if (content_type == "image/jpeg")
				{
					return new PhysicalFileResult(filePath, "image/jpeg");
				}else if (content_type == "application/pdf")
				{
					return new PhysicalFileResult(filePath, "application/pdf");
				}
				else
				{
					return File(bytes, contentType, Path.GetFileName(filePath));
				}
				
				

				
			}
			else
			{
				return NotFound("Resource not found");
			}
			
		}


	}
}
