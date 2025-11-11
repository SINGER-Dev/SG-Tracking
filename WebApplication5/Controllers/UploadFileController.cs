using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using WebApplication5.Managers;
using WebApplication5.Model;
using Microsoft.Extensions.Logging;
using System.Text;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class UploadFileController : ControllerBase
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

		public UploadFileController(ILogger<LoginController> logger)
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

		[HttpPost]

		public async Task<string> Main([FromForm] IFormFile file , [FromForm] string contract , [FromForm]  string record_call_id, [FromForm] string file_name, [FromForm] string file_desc)
		{
			Error Error = new Error();
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

			int attach_ref_id ;
			string json = String.Empty;

			if (isExpired)
			{
				
				Error.StatusCode = "4011";
				Error.Message = "Token has been revoked.";
				json = JsonConvert.SerializeObject(Error);
				var jsonParsed = JObject.Parse(json);
				jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
				json = jsonParsed.ToString();

				return json;
			}
			else
			{
				try
				{

					if (file_name.Trim() == "" || file_desc.Trim() == "")
					{
						Error.StatusCode = "4001";
						Error.Message = "Invalid Data Format.";
						json = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(json);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						json = jsonParsed.ToString();

						return json;
					}

					/*			if (file == null || file.Length == 0)
								{
									return "No file uploaded.";
								}

								// Here, you can handle the uploaded file, save it, process it, etc.

								return "File uploaded successfully.";*/

					long fileSizeLimitBytes = 10 * 1024 * 1024; // 10 MB (10 * 1024 * 1024 bytes)
					long fileSizeBytes = file.Length;
					string fileExtension = Path.GetExtension(file.FileName);


					string logFilePath2 = Path.Combine(Directory.GetCurrentDirectory(), "logfile.txt");
					FileStream fileStream2 = null;
					try
					{
						fileStream2 = new FileStream(logFilePath2, FileMode.Append);

						using (StreamWriter writer = new StreamWriter(fileStream2, Encoding.UTF8))
						{
							// Write a timestamp and log message
							string logMessage = $"[{DateTime.Now}] : " + fileExtension.ToLower();
							writer.WriteLine(logMessage);
						}
					}
					catch (Exception logEx)
					{
						_logger.LogError($"Failed to write to log file {logFilePath2}: {logEx.Message}");
						// ไม่ throw exception เพราะ log ไม่ใช่ส่วนสำคัญ
					}
					finally
					{
						fileStream2?.Dispose();
					}


					if (fileSizeBytes <= fileSizeLimitBytes)
					{

						if (fileExtension.ToLower() == ".jpg" || fileExtension.ToLower() == ".png" || fileExtension.ToLower() == ".jpeg" || fileExtension.ToLower() == ".pdf")
						{
							if (file != null && file.Length > 0)
							{
								ValidateCenter ValidateCenter = new ValidateCenter();
								bool chk_record_contract = ValidateCenter.CHK_RECORD_CONTRACT(contract, record_call_id);
								if (!chk_record_contract)
								{
									Error.StatusCode = "4032";
									Error.Message = "Record_call_id and Contract do not match.";
									json = JsonConvert.SerializeObject(Error);
									var jsonParsed = JObject.Parse(json);
									jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
									json = jsonParsed.ToString();

									return json;
								}
								else
								{
									strConnString2 = myConnectionString2;

									connection.ConnectionString = strConnString2;
									connection.Open();
									TimeZoneInfo thailandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
									strSQL = "INSERT INTO [dbo].[ARM_M_TRACK_FILE]"
											+ "([ARM_RECORD_CALL_ID] "
											+ ",[FILE_NAME_RECORD] "
											+ ",[content_type] "
											+ ",[file_size] "
											+ ",[CREATED_DATE] "
											+ ",[file_name_add] "
											+ ",[file_desc] "
											+ ",[FILE_NAME]) "
											+ "OUTPUT Inserted.ARM_TRACK_FILE_ID VALUES("
											+ "@ARM_RECORD_CALL_ID"
											+ ",@FILE_NAME_RECORD"
											+ ",@content_type"
											+ ",@file_size"
											+ ",@CREATED_DATE"
											+ ",@file_name_add"
											+ ",@file_desc"
											+ ",@FILE_NAME"
											+ ")";

									sqlCommand = new SqlCommand(strSQL, connection);
									sqlCommand.CommandType = CommandType.Text;

									sqlCommand.Parameters.Add("@ARM_RECORD_CALL_ID", SqlDbType.Int);
									sqlCommand.Parameters["@ARM_RECORD_CALL_ID"].Value = record_call_id;

									string FILE_NAME_RECORD = Guid.NewGuid().ToString() + fileExtension.ToLower();
									sqlCommand.Parameters.Add("@FILE_NAME_RECORD", SqlDbType.VarChar);
									sqlCommand.Parameters["@FILE_NAME_RECORD"].Value = FILE_NAME_RECORD;

									sqlCommand.Parameters.Add("@FILE_NAME", SqlDbType.VarChar);
									sqlCommand.Parameters["@FILE_NAME"].Value = file.FileName;

									sqlCommand.Parameters.Add("@file_name_add", SqlDbType.VarChar);
									sqlCommand.Parameters["@file_name_add"].Value = file_name;

									sqlCommand.Parameters.Add("@file_desc", SqlDbType.VarChar);
									sqlCommand.Parameters["@file_desc"].Value = file_desc;


									sqlCommand.Parameters.Add("@file_size", SqlDbType.VarChar);
									sqlCommand.Parameters["@file_size"].Value = fileSizeBytes.ToString();
									DateTime utcNow = DateTime.UtcNow;
									sqlCommand.Parameters.AddWithValue("@CREATED_DATE", TimeZoneInfo.ConvertTimeFromUtc(utcNow, thailandTimeZone));

									sqlCommand.Parameters.Add("@content_type", SqlDbType.VarChar);
									sqlCommand.Parameters["@content_type"].Value = file.ContentType.ToString();

								
									
									attach_ref_id = (int)sqlCommand.ExecuteScalar();

									sqlCommand.Parameters.Clear();
									

									if (attach_ref_id != null)
									{

										strSQL = "UPDATE [dbo].[ARM_M_TRACK_FILE] SET "
											+ " [EncryptedID] = @EncryptedID "
											+ " WHERE ARM_TRACK_FILE_ID = @ARM_TRACK_FILE_ID ";

										sqlCommand = new SqlCommand(strSQL, connection);
										sqlCommand.CommandType = CommandType.Text;
										var encryptTextServices = new EncryptTextService();
										string encryptedText = encryptTextServices.EncryptText(attach_ref_id.ToString(), secretKeyString);

										sqlCommand.Parameters.Add("@EncryptedID", SqlDbType.VarChar);
										sqlCommand.Parameters["@EncryptedID"].Value = encryptedText.ToString();

										sqlCommand.Parameters.Add("@ARM_TRACK_FILE_ID", SqlDbType.VarChar);
										sqlCommand.Parameters["@ARM_TRACK_FILE_ID"].Value = attach_ref_id.ToString();
										sqlCommand.ExecuteScalar();
										sqlCommand.Parameters.Clear();

										// แก้ไขการจัดการ file path
										var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "FileUpload");
										
										// สร้าง directory ถ้ายังไม่มี
										if (!Directory.Exists(uploadDirectory))
										{
											try
											{
												Directory.CreateDirectory(uploadDirectory);
											}
											catch (Exception dirEx)
											{
												_logger.LogError($"Failed to create directory {uploadDirectory}: {dirEx.Message}");
												throw new Exception($"Cannot create upload directory: {dirEx.Message}");
											}
										}

										var filePath = Path.Combine(uploadDirectory, FILE_NAME_RECORD);
										
										try
										{
											using (var stream = new FileStream(filePath, FileMode.Create))
											{
												await file.CopyToAsync(stream);
											}
										}
										catch (Exception fileEx)
										{
											_logger.LogError($"Failed to save file {filePath}: {fileEx.Message}");
											throw new Exception($"Cannot save uploaded file: {fileEx.Message}");
										}

										List<UploadFileResponePayload> UpdateReturnMaster = new List<UploadFileResponePayload>();
										UploadFileRespone UploadFileRespone = new UploadFileRespone();
										UploadFileResponePayload UploadFileResponePayload = new UploadFileResponePayload();


										UploadFileResponePayload.attach_ref_id = attach_ref_id.ToString();
										UpdateReturnMaster.Add(UploadFileResponePayload);
										UploadFileRespone.StatusCode = "200";
										UploadFileRespone.Message = "Success.";
										UploadFileRespone.Payload = UpdateReturnMaster;

										json = JsonConvert.SerializeObject(UploadFileRespone);
									}
									connection.Close();
								}
							}
							else
							{
								Error error = new Error();
								error.StatusCode = "4043";
								error.Message = "The file not found.";
								json = JsonConvert.SerializeObject(error);
								var jsonParsed = JObject.Parse(json);
								jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
								json = jsonParsed.ToString();
							}
						}
						else
						{
							string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logfile.txt");
							FileStream fileStream = null;
							try
							{
								fileStream = new FileStream(logFilePath, FileMode.Append); 

								using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
								{
									// Write a timestamp and log message
									string logMessage = $"[{DateTime.Now}] : " + fileExtension.ToLower();
									writer.WriteLine(logMessage);
								}
							}
							catch (Exception logEx)
							{
								_logger.LogError($"Failed to write to log file {logFilePath}: {logEx.Message}");
								// ไม่ throw exception เพราะ log ไม่ใช่ส่วนสำคัญ
							}
							finally
							{
								fileStream?.Dispose();
							}


							Error error = new Error();
							error.StatusCode = "4002";
							error.Message = "The file has an invalid extension.";
							json = JsonConvert.SerializeObject(error);
							var jsonParsed = JObject.Parse(json);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							json = jsonParsed.ToString();
						}

					}
					else
					{
						Error error = new Error();
						error.StatusCode = "4003";
						error.Message = "File size exceeds the limit.";
						json = JsonConvert.SerializeObject(error);
						var jsonParsed = JObject.Parse(json);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						json = jsonParsed.ToString();
					}


				}
				catch (Exception ex)
				{
					_logger.LogError($"UploadFile Error: {ex.Message}");
					_logger.LogError($"Stack Trace: {ex.StackTrace}");
					
					Error error = new Error();
					error.StatusCode = "500";
					error.Message = ex.Message;
					json = JsonConvert.SerializeObject(error);
					var jsonParsed = JObject.Parse(json);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					json = jsonParsed.ToString();

				}
			}

			return json;
		}

		static string GetFileExtension(IFormFile file)
		{
			// Use the ContentType property to get the MIME type of the file
			// The ContentType contains the MIME type along with the file extension
			string contentType = file.ContentType;

			// The ContentType property may contain extra parameters separated by semicolon (;)
			// We need to split the value and take the first part to get the MIME type
			string[] contentTypeParts = contentType.Split('/');
			string fileExtension = contentTypeParts[1];
			return fileExtension;
		}

		
	}
}
