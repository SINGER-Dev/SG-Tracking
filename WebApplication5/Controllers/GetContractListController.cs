using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
using System.Text.Json;
using System.Xml.Linq;
using WebApplication5.Managers;
using WebApplication5.Model;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class GetContractListController : ControllerBase
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2;
		
		DataTable dt3 = new DataTable();
		public string myConnectionString1;
		public string myConnectionString2;


		private readonly ILogger<LoginController> _logger;

		public GetContractListController(ILogger<LoginController> logger)
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

		[HttpPost]
		public string Main([FromBody] string[] requestBodyArray)
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

				strConnString = myConnectionString2;
				try
				{

					connection.ConnectionString = strConnString;
					connection.Open();
					string json = string.Empty;
					string value = string.Join(",", requestBodyArray);

					string value2 = value.Replace(",", "','");

					List<string> requestBodyArray2 = new List<string> { };

					requestBodyArray2.AddRange(requestBodyArray);
					List<string> uniqueArray = requestBodyArray2.Distinct().ToList();
					ValidateCenter ValidateCenter = new ValidateCenter();
					bool chk_contract = true;
					List<string> data_val = new List<string>();
					List<string> data2_val = new List<string>();
					for (int i = 0; i < uniqueArray.Count; i++)
					{

						bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC(uniqueArray[i].ToString());
						if (!chkCenterContract)
						{
							chk_contract = false;
							data2_val.Add(uniqueArray[i].ToString());
						}
						else
						{
							data_val.Add(uniqueArray[i].ToString());
						}

					}

					strSQL = "ARM_GET_CONTRACT_LIST";
					sqlCommand = new SqlCommand(strSQL, connection);
					sqlCommand.CommandType = CommandType.StoredProcedure;


					ContractListModel ContractListModel = new ContractListModel();
					ContractListModel.StatusCode = "200";
					ContractListModel.Message = "Success.";
					List<Contract> ContractsMaster = new List<Contract>();
					List<M_Contract_Message> ContractNotFoundMaster = new List<M_Contract_Message>();
					ContractListPayload ContractListPayload = new ContractListPayload();
					List<ContractListPayload> ContractListPayloadMaster = new List<ContractListPayload>();
					//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
					for (int i = 0; i < uniqueArray.Count; i++)
					{
						DataTable dt = new DataTable();
						sqlCommand.Parameters.AddWithValue("ARM_ACC_NO", uniqueArray[i].ToString());
						dtAdapter.SelectCommand = sqlCommand;
						dtAdapter.Fill(dt);
						sqlCommand.Parameters.Clear();
						if (dt.Rows.Count > 0)
						{

							foreach (DataRow row in dt.Rows)
							{


								Contract Contracts = new Contract();
								Contracts.contract = row["ARM_ACC_NO"].ToString();

								string inputString = row["ARM_ITEM_DESC"].ToString();
								string[] items = inputString.Split(',');

								for (int i2 = 0; i2 < items.Length; i2++)
								{
									Contracts.productName.Add(items[i2]);

								}

								Contracts.customerName = row["ARM_CUST_NAME"].ToString();


								CurrentAddress currentAddress = new CurrentAddress();
								currentAddress.address = row["AddressNo"].ToString();
								currentAddress.moo = row["MooNo"].ToString();
								currentAddress.village = row["ViilageName"].ToString();
								currentAddress.soi = row["SoiName"].ToString();
								currentAddress.road = row["RoadName"].ToString();
								currentAddress.subdistrict = row["SubdistrictDesc"].ToString();
								currentAddress.district = row["DistrictDesc"].ToString();
								currentAddress.province = row["ProvinceDesc"].ToString();
								currentAddress.zip_code = row["PostCode"].ToString();
								currentAddress.LocationID = row["LocationID"].ToString();

								List<CurrentAddress> currentAddressList = new List<CurrentAddress>();
								currentAddressList.Add(currentAddress);

								Contracts.currentAddress = currentAddressList;
								ContractsMaster.Add(Contracts);
							}

						}
					}
					ContractListPayload.ContractError = data2_val;
					ContractListPayload.Contract = ContractsMaster;
					ContractListPayloadMaster.Add(ContractListPayload);
					ContractListModel.Payload = ContractListPayloadMaster;
					json = JsonConvert.SerializeObject(ContractListModel);
					connection.Close();
					return json;



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
