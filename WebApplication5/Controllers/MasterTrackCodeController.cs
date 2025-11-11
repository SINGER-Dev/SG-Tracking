using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text;
using WebApplication5.Managers;
using WebApplication5.Model;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class MasterTrackCodeController : ControllerBase
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2;
		DataTable dt = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1;
		public string myConnectionString2;


		private readonly ILogger<LoginController> _logger;

		public MasterTrackCodeController(ILogger<LoginController> logger)
		{
			this.secretKey = secretKey;
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			_logger = logger;
		}

		[HttpGet]
		public string Main()
		{
			// ========================================
			// 🚀 START: MasterTrackCodeController
			// ========================================
			
			// เพิ่มการเขียน log ลงใน logfile.txt
			string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logfile.txt");
			
			// เปิดไฟล์ logfile.txt เพื่อเขียน log
			using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
			{
				writer.WriteLine($"\n{'='*60}");
				writer.WriteLine($"🚀 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MasterTrackCodeController - START");
				writer.WriteLine($"{'='*60}");
				
				try
				{
					// ========================================
					// 📋 STEP 1: Log Request Headers
					// ========================================
					writer.WriteLine($"📋 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Step 1: Analyzing request headers...");
					
					var allHeaders = new List<string>();
					foreach (var header in Request.Headers)
					{
						writer.WriteLine($"📌 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Header: {header.Key}: {header.Value}");
						allHeaders.Add($"{header.Key}: {header.Value}");
					}
					writer.WriteLine($"📊 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Total headers found: {allHeaders.Count}");
					
					// ========================================
					// 🔐 STEP 2: Check Authorization Header
					// ========================================
					writer.WriteLine($"🔐 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Step 2: Checking authorization header...");
					
					// ตรวจสอบ Authorization header แบบปลอดภัย
					string accessToken = null;
					
					// ตรวจสอบว่า Request.Headers เป็น null หรือไม่
					if (Request.Headers != null)
					{
						// ตรวจสอบ Authorization header ในรูปแบบต่างๆ
						if (Request.Headers.ContainsKey("Authorization"))
						{
							accessToken = Request.Headers["Authorization"].FirstOrDefault();
							writer.WriteLine($"🔑 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Authorization Header: {(string.IsNullOrEmpty(accessToken) ? "❌ EMPTY" : "✅ FOUND")}");
						}
						else
						{
							writer.WriteLine($"🔑 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Authorization Header: ❌ NOT FOUND");
						}

						// Check for Authorization header in different formats
						if (string.IsNullOrEmpty(accessToken))
						{
							writer.WriteLine($"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Trying alternative header formats...");
							
							// Try different header names
							if (Request.Headers.ContainsKey("authorization"))
							{
								accessToken = Request.Headers["authorization"].FirstOrDefault();
								writer.WriteLine($"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Trying lowercase authorization: {(string.IsNullOrEmpty(accessToken) ? "❌ NOT FOUND" : "✅ FOUND")}");
							}
						}

						if (string.IsNullOrEmpty(accessToken))
						{
							// Try X-Authorization header
							if (Request.Headers.ContainsKey("X-Authorization"))
							{
								accessToken = Request.Headers["X-Authorization"].FirstOrDefault();
								writer.WriteLine($"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Trying X-Authorization: {(string.IsNullOrEmpty(accessToken) ? "❌ NOT FOUND" : "✅ FOUND")}");
							}
						}

						if (string.IsNullOrEmpty(accessToken))
						{
							// Try X-API-Key header
							if (Request.Headers.ContainsKey("X-API-Key"))
							{
								accessToken = Request.Headers["X-API-Key"].FirstOrDefault();
								writer.WriteLine($"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Trying X-API-Key: {(string.IsNullOrEmpty(accessToken) ? "❌ NOT FOUND" : "✅ FOUND")}");
							}
						}
					}
					else
					{
						writer.WriteLine($"🔑 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Request.Headers is NULL");
					}

					// TEMPORARY: Bypass authorization for testing
					// Remove this after Kong configuration is fixed
					if (string.IsNullOrEmpty(accessToken))
					{
						writer.WriteLine($"⚠️ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Authorization header is missing");
						writer.WriteLine($"   🔑 Status: MISSING AUTHORIZATION HEADER");
						writer.WriteLine($"   📋 Available headers: {string.Join(", ", Request.Headers?.Keys ?? new string[0])}");
						
						// Return error response when Authorization header is missing
						Error error = new Error();
						error.StatusCode = "4010";
						error.Message = "Authorization header is missing. Please include Authorization header in your request.";
						string json = JsonConvert.SerializeObject(error);
						var jsonParsed = JObject.Parse(json);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						json = jsonParsed.ToString();
						
						writer.WriteLine($"❌ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Returning error response: {json}");
						return json;
					}
					else
					{
						writer.WriteLine($"✅ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Authorization header found, proceeding with validation...");
					}

					// ========================================
					// 🔍 STEP 3: Token Validation
					// ========================================
					writer.WriteLine($"🔍 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Step 3: Validating token...");
					
					IAuthService expirationChecker = new JWTService();
					bool isExpired = expirationChecker.IsTokenExpired(accessToken);
					writer.WriteLine($"⏰ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Token expired check: {(isExpired ? "❌ EXPIRED" : "✅ VALID")}");

					if (isExpired)
					{
						writer.WriteLine($"❌ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Token has been revoked");
						
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
						writer.WriteLine($"✅ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Token validation passed, connecting to database...");
						
						// ========================================
						// 🗄️ STEP 4: Database Operations
						// ========================================
						writer.WriteLine($"🗄️ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Step 4: Database operations...");
						
						strConnString = myConnectionString2;
						
						try
						{
							writer.WriteLine($"🔌 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Connecting to database...");
							connection.ConnectionString = strConnString;
							connection.Open();
							writer.WriteLine($"✅ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Database connection opened successfully");

							strSQL = "ARM_GET_TrackResult";
							writer.WriteLine($"📊 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Executing stored procedure: {strSQL}");
							
							sqlCommand = new SqlCommand(strSQL, connection);
							sqlCommand.CommandType = CommandType.StoredProcedure;

							//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
							dtAdapter.SelectCommand = sqlCommand;

							dtAdapter.Fill(dt);
							writer.WriteLine($"📈 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Query executed successfully, rows returned: {dt.Rows.Count}");
							
							connection.Close();
							writer.WriteLine($"🔌 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Database connection closed");
							
							// ========================================
							// 📋 STEP 5: Process Data
							// ========================================
							writer.WriteLine($"📋 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Step 5: Processing data...");
							
							if (dt.Rows.Count > 0)
							{
								writer.WriteLine($"🔄 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Processing data rows...");
								MasterTrackCodeModel MasterTrackCodeModel = new MasterTrackCodeModel();
								MasterTrackCodeModel.StatusCode = "200";
								MasterTrackCodeModel.Message = "Success.";

								List<TrackResult> TrackResultMaster = new List<TrackResult>();
								foreach (DataRow row in dt.Rows)
								{
									TrackResult TrackResult = new TrackResult();
									TrackResult.track_code = row["ARM_TRACK_CODE"].ToString();
									TrackResult.description = row["ARM_TRACK_NAME"].ToString();

									TrackResultMaster.Add(TrackResult);
								}
								MasterTrackCodeModel.Payload = TrackResultMaster;
								string json = JsonConvert.SerializeObject(MasterTrackCodeModel);
								writer.WriteLine($"✅ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Successfully processed {TrackResultMaster.Count} records");

								return json;
							}
							else
							{
								writer.WriteLine($"⚠️ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] No data returned from database");
								
								Error error = new Error();
								error.StatusCode = "4001";
								error.Message = "Invalid Data Format.";
								string json = JsonConvert.SerializeObject(error);
								var jsonParsed = JObject.Parse(json);
								jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
								json = jsonParsed.ToString();
								return json;
							}
						}
						catch (Exception ex)
						{
							writer.WriteLine($"❌ [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Database error: {ex.Message}");
							writer.WriteLine($"📚 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stack trace: {ex.StackTrace}");
							
							Error error = new Error();
							error.StatusCode = "500";
							error.Message = $"Internal Server Error: {ex.Message}";
							string json = JsonConvert.SerializeObject(error);
							var jsonParsed = JObject.Parse(json);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							json = jsonParsed.ToString();
							return json;
						}
					}
				}
				catch (Exception ex)
				{
					writer.WriteLine($"💥 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] General error in MasterTrackCodeController: {ex.Message}");
					writer.WriteLine($"📚 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stack trace: {ex.StackTrace}");
					
					Error error = new Error();
					error.StatusCode = "500";
					error.Message = $"Internal Server Error: {ex.Message}";
					string json = JsonConvert.SerializeObject(error);
					var jsonParsed = JObject.Parse(json);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					json = jsonParsed.ToString();
					return json;
				}
				
				// ========================================
				// 🏁 END: Success
				// ========================================
				writer.WriteLine($"\n{'='*60}");
				writer.WriteLine($"🏁 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MasterTrackCodeController - END (SUCCESS)");
				writer.WriteLine($"{'='*60}\n");
			}
		}
	}
}
