using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
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

			string accessToken = Request.Headers["Authorization"];

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
					connection.ConnectionString = strConnString;
					connection.Open();



					strSQL = "ARM_GET_TrackResult";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.CommandType = CommandType.StoredProcedure;

					//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
					dtAdapter.SelectCommand = sqlCommand;

					dtAdapter.Fill(dt);
					connection.Close();
					if (dt.Rows.Count > 0)
					{
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

						return json;
					}
					else
					{
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
					Error error = new Error();
					error.StatusCode = "500";
					error.Message = "Internal Server Error.";
					string json = JsonConvert.SerializeObject(error);
					var jsonParsed = JObject.Parse(json);
					jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					json = jsonParsed.ToString();
					return json;
				}
			}
		}
	}
}
