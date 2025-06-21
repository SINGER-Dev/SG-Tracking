using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using WebApplication5.Model;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;
using Aes = System.Security.Cryptography.Aes;
using WebApplication5.Managers;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace WebApplication5.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class validateContractController : ControllerBase
	{
		JsonResult result = null;
		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter2 = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2, api_key;
		DataSet ds = new DataSet();
		DataTable dt = new DataTable();
		DataTable dt2 = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1, myConnectionString2, strWeb;

		private readonly ILogger<validateContractController> _logger;

		public validateContractController(ILogger<validateContractController> logger)
		{
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			strWeb = _configuration.GetConnectionString("strWeb");
			api_key = _configuration.GetConnectionString("api_key");
			_logger = logger;
		}


		[HttpPost]
		public string Main([FromBody] GetDetail getDetail)
		{

			string accessToken = Request.Headers["Authorization"];
			
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
				try
				{
					strConnString2 = myConnectionString2;

					connection.ConnectionString = strConnString2;
					connection.Open();

					strSQL = "GetDetail";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.CommandType = CommandType.StoredProcedure;

					//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
					sqlCommand.Parameters.AddWithValue("ARM_ACC_NO", getDetail.contract);

					dtAdapter2.SelectCommand = sqlCommand;

					dtAdapter2.Fill(dt2);
					connection.Close();

					if (dt2.Rows.Count > 0)
					{
						ShowDetail showDetail = new ShowDetail();
						showDetail.StatusCode = "200";
						showDetail.Message = "Success.";
						showDetail.contractStatus = true;
						showDetail.contract = dt2.Rows[0]["ARM_ACC_NO"].ToString();
						showDetail.productName = dt2.Rows[0]["ARM_SALES_PART_GROUPDESC"].ToString();
						showDetail.customerName = dt2.Rows[0]["ARM_SALESMAN_NAME"].ToString();
						showDetail.customerAddress = dt2.Rows[0]["ADDRESS"].ToString();



						/*string text = "pageslug=null&readonly=false&accno=MDc0LUg3MDE1";*/

						string originalString = getDetail.contract; // original string
						byte[] data = Encoding.UTF8.GetBytes(originalString); // convert the string to a byte array
						string base64EncodedString = Convert.ToBase64String(data); //เข้ารหัสด้วย base64 

						/*string text = "null,false,"+ base64EncodedString + ","+ getDetail.user_id;*/
						string text = "null,false," + base64EncodedString;
						SHA256 mySHA256 = SHA256Managed.Create();
						byte[] keyBytes = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(api_key.ToString())); //เข้ารหัส Hash256bit ใช้ token ในการผสมเข้าไปด้วย ถ้าไม่มี token จะไม่สามารถถอดรหัสได้ 
						byte[] ivBytes = new byte[16]; // initialization vector
						byte[] encryptedBytes;

						using (var aes = Aes.Create())
						{
							aes.Key = keyBytes;
							aes.IV = ivBytes;
							aes.Mode = CipherMode.CBC;

							var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
							byte[] textBytes = Encoding.UTF8.GetBytes(text);

							using (var ms = new MemoryStream())
							{
								using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
								{
									cs.Write(textBytes, 0, textBytes.Length);
									cs.FlushFinalBlock();
									encryptedBytes = ms.ToArray();
								}
							}
						}

						string encodedText = Convert.ToBase64String(encryptedBytes);

						//Login โดยไม่ต้องใส่รหัส
						/*showDetail.ContractURL = "https:/intranet.singerthai.netar_collection_devarcollection_detial_contact.php?"+encodedText;*/

						string text_2 = base64EncodedString;
						using (var aes = Aes.Create())
						{
							aes.Key = keyBytes;
							aes.IV = ivBytes;
							aes.Mode = CipherMode.CBC;

							var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
							byte[] textBytes = Encoding.UTF8.GetBytes(text_2);

							using (var ms = new MemoryStream())
							{
								using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
								{
									cs.Write(textBytes, 0, textBytes.Length);
									cs.FlushFinalBlock();
									encryptedBytes = ms.ToArray();
								}
							}
						}

						string encodedText2 = Convert.ToBase64String(encryptedBytes);

						//Redirect ไปยังหน้า Contract Auto
						showDetail.ContractURL = strWeb+"redirect_ar_collection_detail_contact.php?" + encodedText2;

						string json = JsonConvert.SerializeObject(showDetail);
						return json;

					}
					else
					{
						List<string> data2_val = new List<string>();
						data2_val.Add(getDetail.contract);
						Error.StatusCode = "4041";
						Error.Message = "Contract Not Found.";
						Error.Payload = data2_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
				}catch (Exception ex)
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