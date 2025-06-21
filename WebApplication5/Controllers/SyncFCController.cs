using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using WebApplication5.Model;
using System.Text;
using Newtonsoft.Json.Linq;
using WebApplication5.Managers;
using System.Text.Json;
using System.Xml;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class SyncFCController : ControllerBase
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
		public string soapEndpoint;
		
		private readonly ILogger<LoginController> _logger;

		public SyncFCController(ILogger<LoginController> logger)
		{
			this.secretKey = secretKey;
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			soapEndpoint = _configuration.GetConnectionString("soapEndpoint");
			_logger = logger;
		}

		class SyncFCList
		{
			public string employee_id { get; set; }
			public string[] contracts { get; set; }
		}

		[HttpPost]
		public string Main([FromBody] JsonElement[]? requestBodyArray)
		{
			string accessToken = Request.Headers["Authorization"];
			IAuthService expirationChecker = new JWTService();
			bool isExpired = expirationChecker.IsTokenExpired(accessToken);
			var jsonReturn = "";
			if (isExpired)
			{
				Error Error = new Error();
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
					string jsonString = System.Text.Json.JsonSerializer.Serialize(requestBodyArray);
					ValidateCenter ValidateCenter = new ValidateCenter();
					List<M_Contract_Message> ContractNotFoundMaster = new List<M_Contract_Message>();

					bool chk_employee = true;
					bool chk_contract = true;
					List<string> data_val = new List<string>();
					List<string> data2_val = new List<string>();
					List<string> DuplicateEmp = new List<string>();
					List<string> DuplicateContract = new List<string>();


					foreach (var requestBody in requestBodyArray)
					{
						if (requestBody.TryGetProperty("employee_id", out var employee_id))
						{
							bool chkCenterEmployee = ValidateCenter.CHK_USER_TRACKING(employee_id.GetString());
							if (!chkCenterEmployee)
							{
								chk_employee = false;
								data_val.Add(employee_id.GetString());
							}
							DuplicateEmp.Add(employee_id.GetString());
						}
						else
						{
							Error Error = new Error();
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							jsonReturn = JsonConvert.SerializeObject(Error);

							var jsonParsed = JObject.Parse(jsonReturn);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							jsonReturn = jsonParsed.ToString();

							return jsonReturn;
						}

						if (requestBody.TryGetProperty("contracts", out var contracts))
						{

							foreach (var value in contracts.EnumerateArray())
							{
								bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(value.ToString());
								if (!chkCenterContract)
								{
									chk_contract = false;
									data2_val.Add(value.ToString());
								}
								DuplicateContract.Add(value.ToString());
							}

						}
						else
						{
							Error Error = new Error();
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							jsonReturn = JsonConvert.SerializeObject(Error);

							var jsonParsed = JObject.Parse(jsonReturn);
							jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
							jsonReturn = jsonParsed.ToString();

							return jsonReturn;
						}
					}

					int chkDuplicateEmp = ValidateCenter.DuplicateEmp(DuplicateEmp).Count;
					int chkDuplicateContract = ValidateCenter.DuplicateContract(DuplicateContract).Count;

					if (!chk_employee)
					{
						Error Error = new Error();
						Error.StatusCode = "4042";
						Error.Message = "Employee Not Found.";
						Error.Payload = data_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else if (!chk_contract)
					{
						Error Error = new Error();
						Error.StatusCode = "4041";
						Error.Message = "Contract Not Found.";
						Error.Payload = data2_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else if (chkDuplicateEmp > 0)
					{
						List<string> DuplicateEmpList = new List<string>();
						DuplicateEmpList = ValidateCenter.DuplicateEmp(DuplicateEmp);

						Error Error = new Error();
						Error.StatusCode = "4093";
						Error.Message = "Duplicate Employee.";
						Error.Payload = DuplicateEmpList.ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else if (chkDuplicateContract > 0)
					{
						List<string> DuplicateContractList = new List<string>();
						DuplicateContractList = ValidateCenter.DuplicateEmp(DuplicateContract);

						Error Error = new Error();
						Error.StatusCode = "4092";
						Error.Message = "Duplicate Contract.";
						Error.Payload = DuplicateContractList.ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else
					{
						string soapRequestXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
											<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
											  <soap12:Body>
												<Import_File_Excel2 xmlns=""http://tempuri.org/"">
														<jsonData> " + jsonString + @"</jsonData>
													</Import_File_Excel2>
												</soap12:Body>
											</soap12:Envelope>";


						using (var client = new HttpClient())
						{
							client.DefaultRequestHeaders.Add("SOAPAction", soapEndpoint + "WebService_Main_AR_Collection.asmx?op=Import_File_Excel2");
							// Make the HTTP POST request
							var response = client.PostAsync(soapEndpoint + "WebService_Main_AR_Collection.asmx", new StringContent(soapRequestXml, Encoding.UTF8, "application/soap+xml")).Result;

							if (response.IsSuccessStatusCode)
							{
								Error Error = new Error();
								Error.StatusCode = "200";
								Error.Message = "Success.";
								jsonReturn = JsonConvert.SerializeObject(Error);

								var jsonParsed = JObject.Parse(jsonReturn);
								jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
								jsonReturn = jsonParsed.ToString();

								return jsonReturn;

							}
							else
							{
								Error Error = new Error();
								Error.StatusCode = "4001";
								Error.Message = "Invalid Data Format.";
								jsonReturn = JsonConvert.SerializeObject(Error);

								var jsonParsed = JObject.Parse(jsonReturn);
								jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
								jsonReturn = jsonParsed.ToString();

								return jsonReturn;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Error Error = new Error();
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
