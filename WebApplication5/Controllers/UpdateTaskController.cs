using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;
using WebApplication5.Managers;
using WebApplication5.Model;
using System.Xml;
using System.Text.Json;
using System.Text;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NLog;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class UpdateTaskController : ControllerBase
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
		public string myConnectionString1, myConnectionString2, strWeb , URL;
		public string soapEndpoint;
		public UpdateTaskController()
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
			URL = _configuration.GetConnectionString("URL");
		}

		[HttpPost]
		public string Main([FromBody] UpdateTaskModel UpdateTaskModel)
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
				try
				{
					string track_type = UpdateTaskModel.track_type;

					string created_dateValue = "-";
					string taskplan_dateValue = "-";
					string contractValue = "-";
					string track_codeValue = "-";
					string employee_idValue = "-";
					string workplan_codeValue = "-";
					string detail = "-";
					string lat = "-";
					string lng = "-";
					string tel = "-";
					string custappointDate = "-";
					string datecollect = "-";
					string track_address = "-";
					string record_call_id = "-";
					string appointmentAmt = "-";

					CultureInfo culture = new CultureInfo("en-US");

					string format = "yyyy-MM-dd HH:mm:ss";
					List<string> track_type_01_list = new List<string>();

					if (track_type == null)
					{
						track_type_01_list.Add("track_type");
						Error.StatusCode = "4001";
						Error.Message = "Invalid Data Format.";
						Error.Payload = track_type_01_list.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}

					if (detail.Length >= 4000)
					{
						Error.StatusCode = "4004";
						Error.Message = "Detail Length Exceed Limit.";
						jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(jsonReturn);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed.ToString();
						return jsonReturn;
					}
					

					if (track_type.Trim() == "01")
					{
						if (UpdateTaskModel.datecollect != null && DateTime.TryParseExact(UpdateTaskModel.datecollect.ToString(), format, culture, System.Globalization.DateTimeStyles.None, out DateTime result))
						{
							datecollect = UpdateTaskModel.datecollect.ToString();
						}
						else
						{
							track_type_01_list.Add("datecollect");
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							Error.Payload = track_type_01_list.Distinct().ToArray();
							jsonReturn = JsonConvert.SerializeObject(Error);
							return jsonReturn;
						}

						if (UpdateTaskModel.custappointDate != null && DateTime.TryParseExact(UpdateTaskModel.custappointDate.ToString(), format, culture, System.Globalization.DateTimeStyles.None, out DateTime result2) )
						{
							//วัน เวลา นัดชำระ
							custappointDate = UpdateTaskModel.custappointDate.ToString();
						}
						else
						{
							track_type_01_list.Add("custappointDate");
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							Error.Payload = track_type_01_list.Distinct().ToArray();
							jsonReturn = JsonConvert.SerializeObject(Error);
							return jsonReturn;
						}

						
						//จำนวนเงินที่ชำระ
						if(UpdateTaskModel.appointmentAmt != null && UpdateTaskModel.appointmentAmt != "")
						{
							appointmentAmt = UpdateTaskModel.appointmentAmt;
						}
						else
						{
							track_type_01_list.Add("appointmentAmt");
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							Error.Payload = track_type_01_list.Distinct().ToArray();
							jsonReturn = JsonConvert.SerializeObject(Error);
							return jsonReturn;
						}
					}
					else if (track_type.Trim() == "02")
					{
						if (UpdateTaskModel.custappointDate != null && DateTime.TryParseExact(UpdateTaskModel.custappointDate.ToString(), format, culture, System.Globalization.DateTimeStyles.None, out DateTime result2))
						{
							//วัน เวลา นัดชำระ
							custappointDate = UpdateTaskModel.custappointDate.ToString();
						}
						else
						{
							track_type_01_list.Add("custappointDate");
							Error.StatusCode = "4001";
							Error.Message = "Invalid Data Format.";
							Error.Payload = track_type_01_list.Distinct().ToArray();
							jsonReturn = JsonConvert.SerializeObject(Error);
							return jsonReturn;
						}
					}
					else if (track_type.Trim() == "03")
					{

					}
					else
					{
						Error.StatusCode = "500";
						Error.Message = "Internal Server Error.";
						jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed = JObject.Parse(jsonReturn);
						jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed.ToString();
						return jsonReturn;
					}

					contractValue = UpdateTaskModel.contract;
					track_codeValue = UpdateTaskModel.track_code;
					created_dateValue = UpdateTaskModel.created_date.ToString();

					if
					(
						(track_codeValue.Trim() == "FC01" && track_type.Trim() == "01") ||
						(track_codeValue.Trim() == "FC12" && track_type.Trim() == "02") ||
						(track_codeValue.Trim() == "FC20" && track_type.Trim() == "02") ||
						(track_codeValue.Trim() == "FC21" && track_type.Trim() == "02")
					)
					{
						taskplan_dateValue = UpdateTaskModel.custappointDate.ToString();
					}
					else
					{
						taskplan_dateValue = UpdateTaskModel.plandate.ToString();
					}
					


					employee_idValue = UpdateTaskModel.employee_id;
					workplan_codeValue = UpdateTaskModel.workplan_code;
					detail = UpdateTaskModel.detail;
					lat = UpdateTaskModel.lat;
					lng = UpdateTaskModel.lng;
					tel = UpdateTaskModel.tel;
					track_address = UpdateTaskModel.track_address;
					record_call_id = string.Empty;


					
					ValidateCenter ValidateCenter = new ValidateCenter();
					bool checkContract = true;
					bool chk_contract_dupp = true;

					List<string> DupContract = new List<string>();
					string json = string.Empty;
					List<M_Contract_Message> M_Contract_Message_Master = new List<M_Contract_Message>();

					bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(contractValue.ToString());
					if (!chkCenterContract)
					{
						chk_contract_dupp = false;
						DupContract.Add(contractValue.ToString());
					}

					bool chk_employee = true;
					List<string> data_val = new List<string>();
					bool chkCenterEmployee = ValidateCenter.CHK_USER_TRACKING(employee_idValue.ToString());
					if (!chkCenterEmployee)
					{
						chk_employee = false;
						data_val.Add(employee_idValue.ToString());
					}

					bool chk_permission = true;
					List<string> data_val2 = new List<string>();
					bool chkCenterPermission = ValidateCenter.CHK_PERMISSION_SYNCFC(contractValue.ToString(),employee_idValue.ToString());
					if (!chkCenterPermission)
					{
						chk_permission = false;
						data_val2.Add(contractValue.ToString());
					}


					if (!chk_employee)
					{
						Error.StatusCode = "4042";
						Error.Message = "Employee Not Found.";
						Error.Payload = data_val.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}else if (!chk_contract_dupp)
					{
						Error.StatusCode = "4041";
						Error.Message = "Contract Not Found.";
						Error.Payload = DupContract.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}else if (!chk_permission)
					{
						Error.StatusCode = "4031";
						Error.Message = "Employee and Contract do not match.";
						Error.Payload = data_val2.Distinct().ToArray();
						jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else {

				

						//ดึงข้อมูลประเภทบัญชี และชื่อ
						string ARM_ORG_AGING_CURR_TYPE_VALUE = "";
						string ARM_CUST_NAME_VALUE = "";
						string txtQUERY_Acc_Detail = @"<?xml version=""1.0"" encoding =""utf -8"" ?>
																						<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12= ""http://www.w3.org/2003/05/soap-envelope"" >
																						  <soap12:Body>
																							<GET_Acc_Detail xmlns=""http://tempuri.org/"">
																							  <ACC_CODE>" + contractValue + @"</ACC_CODE>
																							</GET_Acc_Detail>
																						  </soap12:Body>
																						</soap12:Envelope>";

						using (var client = new HttpClient())
						{
							client.DefaultRequestHeaders.Add("SOAPAction", soapEndpoint + "WebService_Main_AR_Collection.asmx?op=GET_Acc_Detail");

							// Make the HTTP POST request
							var response2 = client.PostAsync(soapEndpoint + "WebService_Main_AR_Collection.asmx", new StringContent(txtQUERY_Acc_Detail, Encoding.UTF8, "application/soap+xml")).Result;
							if (response2.IsSuccessStatusCode)
							{
								var soapResponseXml2 = response2.Content.ReadAsStringAsync().Result;
								XmlDocument xmlDocument2 = new XmlDocument();
								xmlDocument2.LoadXml(soapResponseXml2);

								JsonDocument jsonDocument2 = JsonDocument.Parse(xmlDocument2.InnerText);
								foreach (var arrayElement3 in jsonDocument2.RootElement.EnumerateArray())
								{
									if (arrayElement3.TryGetProperty("ARM_ORG_AGING_CURR_TYPE", out var ARM_ORG_AGING_CURR_TYPE))
									{
										ARM_ORG_AGING_CURR_TYPE_VALUE = ARM_ORG_AGING_CURR_TYPE.GetString();
									}
									if (arrayElement3.TryGetProperty("ARM_CUST_NAME", out var ARM_CUST_NAME))
									{
										ARM_CUST_NAME_VALUE = ARM_CUST_NAME.GetString();
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
																						  <ARM_CUSTOMER_TEL>" + tel + @"</ARM_CUSTOMER_TEL>
																						  <ARM_CUSTOMER_NAME>" + ARM_CUST_NAME_VALUE + @"</ARM_CUSTOMER_NAME>
																						  <ARM_EMPLOYEE_CALL>" + employee_idValue + @"</ARM_EMPLOYEE_CALL>
																						  <ARM_WORKPLAN_TIME>" + taskplan_dateValue + @"</ARM_WORKPLAN_TIME>
																						  <ARM_WORKPLAN_ID>" + workplan_codeValue + @"</ARM_WORKPLAN_ID>
																						  <ARM_OPERATOR_DEPARTMENT>ADMIN</ARM_OPERATOR_DEPARTMENT>
																						  <ARM_OPERATOR_ID>ADMIN</ARM_OPERATOR_ID>
																						  <ARM_CALL_STAT>-</ARM_CALL_STAT>
																						  <ARM_PAYMENT_DATE>" + custappointDate + @"</ARM_PAYMENT_DATE>
																						  <ARM_PAYMENT_AMT>" + appointmentAmt + @"</ARM_PAYMENT_AMT>
																						  <ARM_ADDRESS_TYPE>-</ARM_ADDRESS_TYPE>
																						  <ARM_RECORD_CALL_DETAIL>" + detail + @"</ARM_RECORD_CALL_DETAIL>
																						  <ARM_AGING_TYPE>" + ARM_ORG_AGING_CURR_TYPE_VALUE + @"</ARM_AGING_TYPE>
																						  <ARM_RUNNING_FLAG>-</ARM_RUNNING_FLAG>
																						  <ARM_SIGNAL_CLOSE_DATE>-</ARM_SIGNAL_CLOSE_DATE>
																						  <ARM_SIGNAL_OPEN_DATE>-</ARM_SIGNAL_OPEN_DATE>
																						  <ARM_SIGNAL_OPEN_AMT>-</ARM_SIGNAL_OPEN_AMT>
																						  <ARM_OPEN_REASON></ARM_OPEN_REASON>
																						  <ARM_CONTACT_PLACE>-</ARM_CONTACT_PLACE>
																						  <ARM_MONEY_DISCOUNT>-</ARM_MONEY_DISCOUNT>
																						  <ARM_REFINANCE_AMT>-</ARM_REFINANCE_AMT>
																						  <ARM_FIRST_COLLECT>-</ARM_FIRST_COLLECT>
																						  <ARM_INSTALLMENT_AMT>-</ARM_INSTALLMENT_AMT>
																						  <ARM_INSTALLMENT_COUNT>-</ARM_INSTALLMENT_COUNT>
																						  <ARM_FEE_AMT>-</ARM_FEE_AMT>
																						  <ARM_IMPOUND_DATE>-</ARM_IMPOUND_DATE>
																						  <ARM_SHOP_NAME></ARM_SHOP_NAME>
																						  <ARM_RECIEVER_PRODUCT></ARM_RECIEVER_PRODUCT>
																						  <ARM_RECIEVER_POSITION></ARM_RECIEVER_POSITION>
																						  <ARM_CORRUPT_AMT>-</ARM_CORRUPT_AMT>
																						  <ARM_CORRUPT_NAME></ARM_CORRUPT_NAME>
																						  <ARM_CORRUPT_POSITION></ARM_CORRUPT_POSITION>
																						  <ARM_DATE_COLLECT>" + datecollect + @"</ARM_DATE_COLLECT>
																						  <ARM_LATITUDE>" + lat + @"</ARM_LATITUDE>
																						  <ARM_LONGITUDE>" + lng + @"</ARM_LONGITUDE>
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


								client.DefaultRequestHeaders.Add("SOAPAction", soapEndpoint + "WebService_Main_AR_Collection.asmx?op=INSERT_RECORD_CALL");

								// Make the HTTP POST request
								var response3 = client.PostAsync(soapEndpoint + "WebService_Main_AR_Collection.asmx", new StringContent(soapRequestXml2, Encoding.UTF8, "application/soap+xml")).Result;

								//เมื่อบันทึกข้อมูลสำเร็จ ให้ทำการ บันทึก ที่อยู่
								if (response3.IsSuccessStatusCode)
								{
									string INSERT_RECORD_CALLResult_VALUE = "";
									var soapResponseXml3 = response3.Content.ReadAsStringAsync().Result;
									XmlDocument xmlDocument3 = new XmlDocument();
									xmlDocument3.LoadXml(soapResponseXml3);
									INSERT_RECORD_CALLResult_VALUE = xmlDocument3.InnerText;

									if (INSERT_RECORD_CALLResult_VALUE != "ERR")
									{
										strConnString2 = myConnectionString2;

										connection.ConnectionString = strConnString2;
										connection.Open();

										strSQL = "ARM_ADD_TRACK_ADDRESS";
										sqlCommand = new SqlCommand(strSQL, connection);
										sqlCommand.CommandType = CommandType.StoredProcedure;

										//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
										sqlCommand.Parameters.AddWithValue("ARM_CONTRACT_ID", contractValue);
										sqlCommand.Parameters.AddWithValue("TRACK_ADDRESS", track_address);

										dtAdapter2.SelectCommand = sqlCommand;

										dtAdapter2.Fill(dt2);
										//รีเทิร์นไอดีที่บันทึกกลับมาแสดง
										record_call_id = dt2.Rows[0]["ARM_RECORD_CALL_ID"].ToString();
										connection.Close();

										//ถ้าบันทึกข้อมูลที่อยู่สำเร็จ
										if (record_call_id.Trim() != "")
										{
											bool responseSyncTaskPlan = true;
											string StatusCode = "200";
											if 
											(
												(track_codeValue.Trim() == "FC01" && track_type.Trim() == "01") ||
												(track_codeValue.Trim() == "FC12" && track_type.Trim() == "02") ||
												(track_codeValue.Trim() == "FC20" && track_type.Trim() == "02") ||
												(track_codeValue.Trim() == "FC21" && track_type.Trim() == "02")
											)

											{
												//ทำการ AUTO SAVE เส้น Sync_TaskPlan
												string apiUrl = URL + "/SyncTaskPlan";
												// Create data to be sent in the request body (if applicable)

												List<SyncTaskPlanRequest> SyncTaskPlanRequestMaster = new List<SyncTaskPlanRequest>();
												SyncTaskPlanRequest SyncTaskPlanRequest = new SyncTaskPlanRequest();


												SyncTaskPlanRequest.contract = contractValue;
												SyncTaskPlanRequest.taskplan_date = taskplan_dateValue;
												SyncTaskPlanRequest.track_code = "FC22";
												SyncTaskPlanRequest.workplan_code = "019";
												SyncTaskPlanRequest.created_date = created_dateValue;

												SyncTaskPlanRequestMaster.Add(SyncTaskPlanRequest);

												List<SyncTaskPlanMaster> SyncTaskPlanLeadMaster = new List<SyncTaskPlanMaster>();
												SyncTaskPlanMaster SyncTaskPlanMaster = new SyncTaskPlanMaster();
												SyncTaskPlanMaster.employee_id = employee_idValue;
												SyncTaskPlanMaster.application_name = "SG_TRACKING";
												SyncTaskPlanMaster.track_type = track_type;

												SyncTaskPlanMaster.payload = SyncTaskPlanRequestMaster;

												SyncTaskPlanLeadMaster.Add(SyncTaskPlanMaster);


												// Serialize the data to JSON format
												string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(SyncTaskPlanLeadMaster);
												var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

												// Make a POST request to the API
												client.DefaultRequestHeaders.Add("Authorization", accessToken);

												// Creating HttpContent with JSON data
												var jsonContent = JsonConvert.SerializeObject(SyncTaskPlanLeadMaster);
												HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

												HttpResponseMessage response = client.PostAsync(apiUrl, httpContent).Result;
												if (response.IsSuccessStatusCode)
												{
													responseSyncTaskPlan = true;
													
													string responseBody = response.Content.ReadAsStringAsync().Result;
													JsonDocument doc = JsonDocument.Parse(responseBody);
													if (doc.RootElement.TryGetProperty("StatusCode", out JsonElement StatusCodeElement))
													{
														StatusCode = StatusCodeElement.GetString();
														//ถ้าบิงไปเส้น  SyncTaskPlan แล้วบันทึกข้อมูลไม่สำเร็จ
														if (StatusCode != "200")
														{
															//record_call_id เปลี่ยน F_Active = 'N'
															strConnString2 = myConnectionString2;

															connection.ConnectionString = strConnString2;
															connection.Open();

															strSQL = "UPDATE [AR_COLLECTION].[dbo].[ARM_M_TRACK_ADDRESS] SET [F_ACTIVE] = 'N' " +
																" WHERE [ARM_RECORD_CALL_ID] = @ARM_RECORD_CALL_ID";

															sqlCommand = new SqlCommand(strSQL, connection);
															sqlCommand.Parameters.AddWithValue("@ARM_RECORD_CALL_ID", record_call_id);
															sqlCommand.CommandType = CommandType.Text;
															dtAdapter3.SelectCommand = sqlCommand;
															sqlCommand.ExecuteNonQuery();
															sqlCommand.Parameters.Clear();

															strSQL = "DELETE FROM [AR_COLLECTION].[dbo].[ARM_T_ARRecordCall] " +
																" WHERE [ARM_RECORD_CALL_ID] = @ARM_RECORD_CALL_ID";

															sqlCommand = new SqlCommand(strSQL, connection);
															sqlCommand.Parameters.AddWithValue("@ARM_RECORD_CALL_ID", record_call_id);
															sqlCommand.CommandType = CommandType.Text;
															dtAdapter3.SelectCommand = sqlCommand;
															sqlCommand.ExecuteNonQuery();
															sqlCommand.Parameters.Clear();
															connection.Close();
															return responseBody;
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
												}
												else
												{
													responseSyncTaskPlan = false;
												}
											}

											if (responseSyncTaskPlan)
											{
												UpdateTaskRespone UpdateTaskRespone = new UpdateTaskRespone();
												UpdateReturn UpdateReturn = new UpdateReturn();

												List<UpdateReturn> UpdateReturnMaster = new List<UpdateReturn>();


												UpdateReturn.contract = contractValue;
												UpdateReturn.record_call_id = record_call_id;
												UpdateReturnMaster.Add(UpdateReturn);

												UpdateTaskRespone.StatusCode = "200";
												UpdateTaskRespone.Message = "success";
												UpdateTaskRespone.Payload = UpdateReturnMaster;

												jsonReturn = JsonConvert.SerializeObject(UpdateTaskRespone);
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
								Error.StatusCode = "500";
								Error.Message = "Internal Server Error.";
								jsonReturn = JsonConvert.SerializeObject(Error);

								var jsonParsed = JObject.Parse(jsonReturn);
								jsonParsed.Properties().Where(attr => attr.Name == "Payload").First().Remove();
								jsonReturn = jsonParsed.ToString();
							}
						}


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
