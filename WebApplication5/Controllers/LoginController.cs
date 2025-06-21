using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using WebApplication5.Model;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using WebApplication5.Model;
using WebApplication5.Managers;
using static System.Net.Mime.MediaTypeNames;

namespace WebApplication5.Controllers
{
	
	[ApiController]
	[Route("[controller]")]
	public class LoginController : ControllerBase
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL , strConnString2, api_key , secretKeyString, strConnString3, SecretKey;
		int ApplicationID;
		DataTable dt = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1;
		public string myConnectionString2, myConnectionString3;


		private readonly ILogger<LoginController> _logger;

		public LoginController(ILogger<LoginController> logger)
		{
			this.secretKey = secretKey;
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			myConnectionString3 = _configuration.GetConnectionString("strConnString3");
			api_key = _configuration.GetConnectionString("api_key");
			secretKeyString = _configuration.GetConnectionString("secretKeyString");
			SecretKey = _configuration.GetConnectionString("SecretKey");
			ApplicationID = Int32.Parse(_configuration.GetConnectionString("ApplicationID"));
			_logger = logger;
		}


		[HttpPost]
		public string Main([FromBody] User User)
		{
			strConnString3 = myConnectionString3;

			connection.ConnectionString = strConnString3;
			connection.Open();

			strSQL = "LoginAuth";
			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.CommandType = CommandType.StoredProcedure;

			//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
			sqlCommand.Parameters.AddWithValue("EMP_CODE", User.user_id);
			sqlCommand.Parameters.AddWithValue("Password", User.password);

			if(!string.IsNullOrWhiteSpace(User.applicationID))
			{
                sqlCommand.Parameters.AddWithValue("ApplicationID", User.applicationID);
            }
			else
			{
                sqlCommand.Parameters.AddWithValue("ApplicationID", ApplicationID);
            }
				
			dtAdapter3.SelectCommand = sqlCommand;
			
			dtAdapter3.Fill(dt3);
			sqlCommand.Parameters.Clear();
			connection.Close();

			try
			{
				 if (dt3.Rows.Count > 0 && User.api_key.ToString().Trim() == api_key.Trim())
				 {


					var itemToAdd = new JObject();
					itemToAdd["UserID"] = dt3.Rows[0]["UserID"].ToString();
					itemToAdd["UserName"] = dt3.Rows[0]["UserName"].ToString();
					itemToAdd["Email"] = dt3.Rows[0]["Email"].ToString();
					itemToAdd["EMP_CODE"] = dt3.Rows[0]["EMP_CODE"].ToString();
					itemToAdd["ApplicationID"] = dt3.Rows[0]["ApplicationID"].ToString();
					itemToAdd["ApplicationName"] = dt3.Rows[0]["ApplicationName"].ToString();
					itemToAdd["ApplicationDescription"] = dt3.Rows[0]["ApplicationDescription"].ToString();
					
					//Gen Token
					IAuthService authService = new JWTService();
					string token = GetJWTTokenl(dt3.Rows[0]["EMP_CODE"].ToString());



					TokenReturn tokenReturn = new TokenReturn();

					
					string[] jsonData21 = dt3.Rows[0]["RoleDescription"].ToString().Split(',');
					List<string> list = new List<string>();
					foreach (var x in jsonData21)
					{
						string[] arrayTXT = x.ToString().Split('-');
						if(arrayTXT.Length > 1)
						{
							string firstTXT = arrayTXT[0].ToString();
							string secondTXT = arrayTXT[1].ToString().ToUpper();
							list.Add(firstTXT.Trim() + "-" + secondTXT.Trim());
						}
						else
						{
							string firstTXT = arrayTXT[0].ToString();
							list.Add(firstTXT.Trim());
						}

					}


					tokenReturn.Role = list.ToArray();

					var encryptTextServices = new EncryptTextService();
					var tokenHandler = new JwtSecurityTokenHandler();

					tokenReturn.StatusCode = "200";
					tokenReturn.Message = "Success.";
					tokenReturn.access_token = token;

					var tokenRead = tokenHandler.ReadJwtToken(token);
					var expirationTime = authService.ConvertTimestampToDate(tokenRead.ValidTo);
					tokenReturn.expired_date = expirationTime;

					tokenReturn.user_id = dt3.Rows[0]["EMP_CODE"].ToString();
					tokenReturn.name = dt3.Rows[0]["FullName"].ToString();
					tokenReturn.position = dt3.Rows[0]["POSITION"].ToString();

					tokenReturn.id_card = encryptTextServices.EncryptText(dt3.Rows[0]["EMP_CARD_ID"].ToString(), secretKeyString);
					tokenReturn.mobile_no = encryptTextServices.EncryptText(dt3.Rows[0]["EMP_MOBILE_NO"].ToString(), secretKeyString);

					tokenReturn.company = dt3.Rows[0]["COMPANY"].ToString();

					tokenReturn.first_name_en = dt3.Rows[0]["EMP_NAME_ENG"].ToString();
					tokenReturn.last_name_en = dt3.Rows[0]["EMP_SUR_ENG"].ToString();

					string json = JsonConvert.SerializeObject(tokenReturn);
					return json;

				}
				else
				{
					Error error = new Error();
					error.StatusCode = "4044";
					error.Message = "UserName or Password is incorrect";
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
				error.StatusCode = "4044";
				error.Message = "Data not found.";
				string json = JsonConvert.SerializeObject(error);

				var jsonParsed = JObject.Parse(json);
				jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
				json = jsonParsed.ToString();

				return json;
			}

		}

	




		

		#region Private Methods
		private static JWTContainerModel GetJWTContainerModel(string name, string email)
		{
			return new JWTContainerModel()
			{
				Claims = new Claim[]
				{
					new Claim(ClaimTypes.Name, name),
					new Claim(ClaimTypes.Email, email)
				}
			};
		}

		private string GetJWTTokenl(string EMP_CODE)
		{
			// Secret key used for signing the JWT
			string secretKey = SecretKey;

			// Create claims for the JWT payload
			var claims = new[]
			{
            new Claim(ClaimTypes.NameIdentifier, EMP_CODE), // User's email
			};

			// Create a JWT token with specific claims and options
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddHours(24), // Token expiration time
				SigningCredentials = new SigningCredentials(
					new SymmetricSecurityKey(Convert.FromBase64String(secretKey)),
					SecurityAlgorithms.HmacSha256Signature
				)
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);

			// Serialize the token to a JWT string
			var jwt = tokenHandler.WriteToken(token);
			return jwt;
		}
		#endregion

	}
}