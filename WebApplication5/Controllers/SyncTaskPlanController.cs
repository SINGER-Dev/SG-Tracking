using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using WebApplication5.Model;
using System.Text;
using WebApplication5.Managers;
using System.Text.Json;
using System.Xml;
using System.Globalization;
using Azure;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class SyncTaskPlanController : ControllerBase
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

		public SyncTaskPlanController(ILogger<LoginController> logger)
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

		[HttpPost]
		public string Main([FromBody] JsonElement[] requestBodyArray)
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
			string contractValue = string.Empty;
			string track_codeValue = string.Empty;
			string created_dateValue = string.Empty;
			string employee_idValue = string.Empty;
			string taskplan_dateValue = string.Empty;
			string workplan_codeValue = string.Empty;
			string track_typeValue = "";
			
			string json = string.Empty;
			List<string> contactFail = new List<string>();
			

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
					List<M_Contract_Message> M_Contract_Message_Master = new List<M_Contract_Message>();
					List<M_Contract_Message> ContractNotFoundMaster = new List<M_Contract_Message>();
					Error Error = new Error();
					CultureInfo culture = new CultureInfo("en-US");
					ValidateCenter ValidateCenter = new ValidateCenter();

					bool chk_employee = true;
					bool chk_contract = true;
					bool chk_contract_dupp = true;
					bool chk_permission = true;

					List<string> data_val = new List<string>();
					List<string> data2_val = new List<string>();
					List<string> DuplicateEmp = new List<string>();
					List<string> DuplicateContract = new List<string>();
					List<string> DupContractList = new List<string>();
					List<string> PermissionList = new List<string>();

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
							employee_idValue = employee_id.GetString();
							DuplicateEmp.Add(employee_id.GetString());
						}


						if (requestBody.TryGetProperty("track_type", out var track_type))
						{
							track_typeValue = track_type.GetString().Trim();
						}


						if (requestBody.TryGetProperty("payload", out var arrayPayload))
						{
							if (arrayPayload.ValueKind == JsonValueKind.Array)
							{
								foreach (var arrayElement in arrayPayload.EnumerateArray())
								{
									if (arrayElement.TryGetProperty("taskplan_date", out var taskplan_date))
									{
										taskplan_dateValue = taskplan_date.GetString().Trim();

									}
									else
									{
										Error.StatusCode = "4001";
										Error.Message = "Invalid Data Format.";
										jsonReturn = JsonConvert.SerializeObject(Error);

										var jsonParsed2 = JObject.Parse(jsonReturn);
										jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
										jsonReturn = jsonParsed2.ToString();

										return jsonReturn;
									}

									


									if (arrayElement.TryGetProperty("contract", out var contract))
									{
										contractValue = contract.GetString().Trim();

										bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(contractValue.ToString());


										bool chkCenterContractDup = ValidateCenter.CHECK_CALL_DUP(contractValue.ToString(), taskplan_dateValue.ToString());

										bool chkCenterPermission = ValidateCenter.CHK_PERMISSION_SYNCFC(contractValue.ToString(), employee_idValue.ToString());

										if (!chkCenterContract)
										{
											chk_contract = false;
											data2_val.Add(contractValue.ToString());
										}

										if(!chkCenterContractDup && track_typeValue.Trim() == "")
										{
											chk_contract_dupp = false;
											DupContractList.Add(contractValue.ToString());
										}

										if(!chkCenterPermission)
										{
											chk_permission = false;
											PermissionList.Add(contractValue.ToString());
										}

										DuplicateContract.Add(contractValue.ToString());

									}
								}
							}
						}
						
					}

					int chkDuplicateEmp = ValidateCenter.DuplicateEmp(DuplicateEmp).Count;
					int chkDuplicateContract = ValidateCenter.DuplicateContract(DuplicateContract).Count;

					if (!chk_employee)
					{
						Error.StatusCode = "4042";
						Error.Message = "Employee Not Found.";
						Error.Payload = data_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else if (!chk_contract)
					{
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
						Error.StatusCode = "4092";
						Error.Message = "Duplicate Contract.";
						Error.Payload = DuplicateContractList.ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
/*					else if (!chk_contract_dupp)
					{
						Error.StatusCode = "4091";
						Error.Message = "Contract already exists.";
						Error.Payload = DupContractList.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}*/
					else if (!chk_permission)
					{
						Error.StatusCode = "4031";
						Error.Message = "Employee and Contract do not match.";
						Error.Payload = PermissionList.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else
					{
						
						foreach (var requestBody in requestBodyArray)
						{
							
							if (requestBody.TryGetProperty("employee_id", out var employee_id))
							{
								employee_idValue = employee_id.GetString();
							}

	
							if (requestBody.TryGetProperty("payload", out var arrayPayload))
							{
								if (arrayPayload.ValueKind == JsonValueKind.Array)
								{
									int count = 0;
									bool checkContract = true;
									bool Check_Acc = true;

									//เลขสัญญาที่ส่งมาไม่มีข้อมูลที่เคยเพิ่มทุกรายการ และ เลขสัญญาเช็คแล้วมีสัญยาอยู่จริง ให้ทำการเพิ่มข้อมูลต่อได้
									if (checkContract && Check_Acc)
									{
										
										foreach (var arrayElement in arrayPayload.EnumerateArray())
										{
											if (arrayElement.TryGetProperty("contract", out var contract2))
											{
												contractValue = contract2.GetString().Trim();
											}
											else
											{
												Error.StatusCode = "4001";
												Error.Message = "Invalid Data Format.";
												jsonReturn = JsonConvert.SerializeObject(Error);

												var jsonParsed2 = JObject.Parse(jsonReturn);
												jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
												jsonReturn = jsonParsed2.ToString();

												return jsonReturn;
											}

											if (arrayElement.TryGetProperty("track_code", out var track_code))
											{
												track_codeValue = track_code.GetString().Trim();
											}
											else
											{
												Error.StatusCode = "4001";
												Error.Message = "Invalid Data Format.";
												jsonReturn = JsonConvert.SerializeObject(Error);

												var jsonParsed2 = JObject.Parse(jsonReturn);
												jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
												jsonReturn = jsonParsed2.ToString();

												return jsonReturn;
											}

											if (arrayElement.TryGetProperty("created_date", out var created_date))
											{
												created_dateValue = created_date.ToString();
											}
											else
											{
												Error.StatusCode = "4001";
												Error.Message = "Invalid Data Format.";
												jsonReturn = JsonConvert.SerializeObject(Error);

												var jsonParsed2 = JObject.Parse(jsonReturn);
												jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
												jsonReturn = jsonParsed2.ToString();

												return jsonReturn;
											}

											if (arrayElement.TryGetProperty("taskplan_date", out var taskplan_date))
											{
												taskplan_dateValue = taskplan_date.ToString();

											}
											else
											{
												Error.StatusCode = "4001";
												Error.Message = "Invalid Data Format.";
												jsonReturn = JsonConvert.SerializeObject(Error);

												var jsonParsed2 = JObject.Parse(jsonReturn);
												jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
												jsonReturn = jsonParsed2.ToString();

												return jsonReturn;
											}

											if (arrayElement.TryGetProperty("workplan_code", out var workplan_code))
											{
												workplan_codeValue = workplan_code.GetString().Trim();
											}
											else
											{
												Error.StatusCode = "4001";
												Error.Message = "Invalid Data Format.";
												jsonReturn = JsonConvert.SerializeObject(Error);

												var jsonParsed2 = JObject.Parse(jsonReturn);
												jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
												jsonReturn = jsonParsed2.ToString();

												return jsonReturn;
											}

											//ดึงข้อมูลประเภทบัญชี และชื่อ
											string ARM_ORG_AGING_CURR_TYPE_VALUE = "";
											string ARM_CUST_NAME_VALUE = "";
											string txtQUERY_Acc_Detail = @"<?xml version=""1.0"" encoding =""utf -8"" ?>
																					<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchemainstance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12= ""http://www.w3.org/2003/05/soap-envelope"" >
																					  <soap12:Body>
																						<GET_Acc_Detail xmlns=""http://tempuri.org/"">
																						  <ACC_CODE>" + contractValue + @"</ACC_CODE>
																						</GET_Acc_Detail>
																					  </soap12:Body>
																					</soap12:Envelope>";
											using (var client = new HttpClient())
											{
												client.DefaultRequestHeaders.Add("SOAPAction", soapEndpoint + "WebService_Main_AR_Collection.asmxop=GET_Acc_Detail");

												// Make the HTTP POST request
												var response2 = client.PostAsync(soapEndpoint + "WebService_Main_AR_Collection.asmx", new StringContent(txtQUERY_Acc_Detail, Encoding.UTF8, "application/soap+xml")).Result;
												//ถ้ามีข้อมูลบัญชี
												if (response2.IsSuccessStatusCode)
												{
													var soapResponseXml2 = response2.Content.ReadAsStringAsync().Result;
													XmlDocument xmlDocument2 = new XmlDocument();
													xmlDocument2.LoadXml(soapResponseXml2);

													JsonDocument jsonDocument2 = JsonDocument.Parse(xmlDocument2.InnerText);
													foreach (var arrayElement4 in jsonDocument2.RootElement.EnumerateArray())
													{
														if (arrayElement4.TryGetProperty("ARM_ORG_AGING_CURR_TYPE", out var ARM_ORG_AGING_CURR_TYPE))
														{
															ARM_ORG_AGING_CURR_TYPE_VALUE = ARM_ORG_AGING_CURR_TYPE.GetString();
														}
														if (arrayElement4.TryGetProperty("ARM_CUST_NAME", out var ARM_CUST_NAME))
														{
															ARM_CUST_NAME_VALUE = ARM_CUST_NAME.GetString();
														}
													}
												}
												string datenow = DateTime.Now.ToString(new CultureInfo("en-US"));
												string soapRequestXml2 = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
																				<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
																				<soap12:Body>
																					<INSERT_RECORD_CALL xmlns=""http://tempuri.org/"" >
																					  <ARM_CONTRACT_ID>-</ARM_CONTRACT_ID>
																					  <ARM_TRACK_CODE>" + track_codeValue + @"</ARM_TRACK_CODE>
																					  <ARM_ACC_NO>" + contractValue + @"</ARM_ACC_NO>
																					  <ARM_CALL_DATE>" + datenow + @"</ARM_CALL_DATE>
																					  <ARM_CUSTOMER_TEL>-</ARM_CUSTOMER_TEL>
																					  <ARM_CUSTOMER_NAME>" + ARM_CUST_NAME_VALUE + @"</ARM_CUSTOMER_NAME>
																					  <ARM_EMPLOYEE_CALL>" + employee_idValue + @"</ARM_EMPLOYEE_CALL>
																					  <ARM_WORKPLAN_TIME>" + taskplan_dateValue + @"</ARM_WORKPLAN_TIME>
																					  <ARM_WORKPLAN_ID>" + workplan_codeValue + @"</ARM_WORKPLAN_ID>
																					  <ARM_OPERATOR_DEPARTMENT>ADMIN</ARM_OPERATOR_DEPARTMENT>
																					  <ARM_OPERATOR_ID>ADMIN</ARM_OPERATOR_ID>
																					  <ARM_CALL_STAT>-</ARM_CALL_STAT>
																					  <ARM_PAYMENT_DATE>-</ARM_PAYMENT_DATE>
																					  <ARM_PAYMENT_AMT>-</ARM_PAYMENT_AMT>
																					  <ARM_ADDRESS_TYPE>-</ARM_ADDRESS_TYPE>
																					  <ARM_RECORD_CALL_DETAIL>-</ARM_RECORD_CALL_DETAIL>
																					  <ARM_AGING_TYPE>" + ARM_ORG_AGING_CURR_TYPE_VALUE + @"</ARM_AGING_TYPE>
																					  <ARM_RUNNING_FLAG>-</ARM_RUNNING_FLAG>
																					  <ARM_SIGNAL_CLOSE_DATE>-</ARM_SIGNAL_CLOSE_DATE>
																					  <ARM_SIGNAL_OPEN_DATE>-</ARM_SIGNAL_OPEN_DATE>
																					  <ARM_SIGNAL_OPEN_AMT>-</ARM_SIGNAL_OPEN_AMT>
																					  <ARM_OPEN_REASON>-</ARM_OPEN_REASON>
																					  <ARM_CONTACT_PLACE>-</ARM_CONTACT_PLACE>
																					  <ARM_MONEY_DISCOUNT>-</ARM_MONEY_DISCOUNT>
																					  <ARM_REFINANCE_AMT>-</ARM_REFINANCE_AMT>
																					  <ARM_FIRST_COLLECT>-</ARM_FIRST_COLLECT>
																					  <ARM_INSTALLMENT_AMT>-</ARM_INSTALLMENT_AMT>
																					  <ARM_INSTALLMENT_COUNT>-</ARM_INSTALLMENT_COUNT>
																					  <ARM_FEE_AMT>-</ARM_FEE_AMT>
																					  <ARM_IMPOUND_DATE>-</ARM_IMPOUND_DATE>
																					  <ARM_SHOP_NAME>-</ARM_SHOP_NAME>
																					  <ARM_RECIEVER_PRODUCT>-</ARM_RECIEVER_PRODUCT>
																					  <ARM_RECIEVER_POSITION>-</ARM_RECIEVER_POSITION>
																					  <ARM_CORRUPT_AMT>-</ARM_CORRUPT_AMT>
																					  <ARM_CORRUPT_NAME>-</ARM_CORRUPT_NAME>
																					  <ARM_CORRUPT_POSITION>-</ARM_CORRUPT_POSITION>
																					  <ARM_DATE_COLLECT>-</ARM_DATE_COLLECT>
																					  <ARM_LATITUDE>-</ARM_LATITUDE>
																					  <ARM_LONGITUDE>-</ARM_LONGITUDE>
																					  <ARM_FIX_FLAG>-</ARM_FIX_FLAG>
																					  <ARM_RQ_NUMBER>-</ARM_RQ_NUMBER>
																					  <ARM_RQ_DATE>-</ARM_RQ_DATE>
																					  <ARM_RQ_TEL>-</ARM_RQ_TEL>
																					  <ARM_RQ_STATUS>-</ARM_RQ_STATUS>
																					  <ARM_RQ_REMARK>-</ARM_RQ_REMARK>
																					  <USER_TYPE>-</USER_TYPE>
																					  <CLAIM>-</CLAIM>
																					  <LATE>-</LATE>
																					  <INSTALLMENT>-</INSTALLMENT>
																					</INSERT_RECORD_CALL>
																				 </soap12:Body>
																			</soap12:Envelope>";


												client.DefaultRequestHeaders.Add("SOAPAction", soapEndpoint + "WebService_Main_AR_Collection.asmxop=INSERT_RECORD_CALL");

												// Make the HTTP POST request
												var response3 = client.PostAsync(soapEndpoint + "WebService_Main_AR_Collection.asmx", new StringContent(soapRequestXml2, Encoding.UTF8, "application/soap+xml")).Result;
												if (response3.IsSuccessStatusCode)
												{
													string INSERT_RECORD_CALLResult_VALUE = "";
													var soapResponseXml3 = response3.Content.ReadAsStringAsync().Result;
													XmlDocument xmlDocument3 = new XmlDocument();
													xmlDocument3.LoadXml(soapResponseXml3);
													INSERT_RECORD_CALLResult_VALUE = xmlDocument3.InnerText;
													if (INSERT_RECORD_CALLResult_VALUE != "ERR")
													{
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
												else
												{
													Error.StatusCode = "4001";
													Error.Message = "Invalid Data Format.";
													jsonReturn = JsonConvert.SerializeObject(Error);

													var jsonParsed = JObject.Parse(jsonReturn);
													jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
													jsonReturn = jsonParsed.ToString();
												}
											}

										}
									}
								}
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

				}

				return jsonReturn;
			}

			

		}
	}
}
