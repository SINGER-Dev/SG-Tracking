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
using Microsoft.AspNetCore.Authorization;

namespace WebApplication5.Controllers
{
	//[Authorize(Roles = "admin")]
	[ApiController]
	[Route("[controller]")]
	public class SupportingInfoController : ControllerBase
	{
		JsonResult result = null;
		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		SqlDataAdapter dtAdapter2 = new SqlDataAdapter();
		SqlDataAdapter dtAdapter3 = new SqlDataAdapter();
		String strConnString, strSQL, strConnString2, api_key , secretKeyString;
		DataSet ds = new DataSet();
		DataTable dt = new DataTable();
		DataTable dt2 = new DataTable();
		DataTable dt3 = new DataTable();
		public string myConnectionString1, myConnectionString2, strWeb;

		private readonly ILogger<LoginController> _logger;

		public SupportingInfoController(ILogger<LoginController> logger)
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
			_logger = logger;
		}

		[HttpGet]
		[Route("Debtor")]
		public string Debtor([FromBody] RequestDebtor RequestDebtor)
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
				try
				{
					Error Error = new Error();
					ValidateCenter ValidateCenter = new ValidateCenter();
					bool chkCenterContract = ValidateCenter.GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(RequestDebtor.contract);
					bool chk_contract = true;
					List<string> data2_val = new List<string>();
					if (!chkCenterContract)
					{
						chk_contract = false;
						data2_val.Add(RequestDebtor.contract);
					}

					if (!chk_contract)
					{
						
						Error.StatusCode = "4041";
						Error.Message = "Contract Not Found.";
						Error.Payload = data2_val.Distinct().ToArray();
						var jsonReturn = JsonConvert.SerializeObject(Error);
						return jsonReturn;
					}
					else if (RequestDebtor.debtor_type.ToString().Trim() != "1" && RequestDebtor.debtor_type.ToString().Trim() != "2" && RequestDebtor.debtor_type.ToString().Trim() != "")
					{
						Error.StatusCode = "4001";
						Error.Message = "Invalid Data Format.";
						var jsonReturn = JsonConvert.SerializeObject(Error);

						var jsonParsed2 = JObject.Parse(jsonReturn);
						jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
						jsonReturn = jsonParsed2.ToString();

						return jsonReturn;
					}
					else
					{
						List<M_Contract_Message> ContractNotFoundMaster = new List<M_Contract_Message>();

						strConnString2 = myConnectionString2;

						connection.ConnectionString = strConnString2;
						connection.Open();

						strSQL = "ARM_GET_ADDRESS";
						sqlCommand = new SqlCommand(strSQL, connection);
						sqlCommand.CommandType = CommandType.StoredProcedure;

						//ชื่อตัวแปรในสโตร , ค่าที่เก็บสโตร
						sqlCommand.Parameters.AddWithValue("ACC_NO", RequestDebtor.contract.Trim());
						sqlCommand.Parameters.AddWithValue("DEBTOR_TYPE", RequestDebtor.debtor_type.Trim());

						dtAdapter2.SelectCommand = sqlCommand;

						dtAdapter2.Fill(dt2);
						connection.Close();

						if (dt2.Rows.Count > 0)
						{
							var encryptTextServices = new EncryptTextService();


							List<DebtorDetail> DebtorDetailMaster = new List<DebtorDetail>();
							foreach (DataRow row in dt2.Rows)
							{
							
							
								DebtorDetail DebtorDetail = new DebtorDetail();
								DebtorDetail.debtor_type = row["DEBTOR_TYPE"].ToString();

								DebtorDetail.cus_name = row["ARM_GUAR_NAME"].ToString();
								DebtorDetail.id_card = encryptTextServices.EncryptText(row["ARM_GUAR_NIC"].ToString(), secretKeyString);
								DebtorDetail.age = row["AgeYears"].ToString();
								DebtorDetail.professional_group = row["Occupation"].ToString();
								DebtorDetail.occupation = row["CurrentPosition"].ToString();
								DebtorDetail.tel = encryptTextServices.EncryptText(row["Tel"].ToString(), secretKeyString);
								DebtorDetail.Referral1 = row["OTHER_C1"].ToString();
								DebtorDetail.Referral2 = row["OTHER_C2"].ToString();

								List<ADDRESS_CURRENT> ADDRESS_CURRENT_MASTER = new List<ADDRESS_CURRENT>();
								ADDRESS_CURRENT ADDRESS_CURRENT = new ADDRESS_CURRENT();

								ADDRESS_CURRENT.HouseNo = row["ADDRESS_NO_D"].ToString();
								ADDRESS_CURRENT.VillageNo = row["MooNo_D"].ToString();
								ADDRESS_CURRENT.Village = row["ViilageName_D"].ToString();
								ADDRESS_CURRENT.Lane = row["SoiName_D"].ToString();
								ADDRESS_CURRENT.Road = row["RoadName_D"].ToString();
								ADDRESS_CURRENT.SubDistrictCode = row["SubdisCode_D"].ToString();
								ADDRESS_CURRENT.SubDistrict = row["SubdisName_D"].ToString();
								ADDRESS_CURRENT.DistrictCode = row["DisCode_D"].ToString();
								ADDRESS_CURRENT.District = row["DisName_D"].ToString();
								ADDRESS_CURRENT.ProvinceCode = row["ProvCode_D"].ToString();
								ADDRESS_CURRENT.Province = row["ProvName_D"].ToString();
								ADDRESS_CURRENT.PostalCode = row["PostCode_D"].ToString();
								ADDRESS_CURRENT_MASTER.Add(ADDRESS_CURRENT);
								DebtorDetail.ADDRESS_CURRENT = ADDRESS_CURRENT_MASTER;

								List<ADDRESS_COMPANY> ADDRESS_COMPANY_MASTER = new List<ADDRESS_COMPANY>();
								ADDRESS_COMPANY ADDRESS_COMPANY = new ADDRESS_COMPANY();
								ADDRESS_COMPANY.HouseNo = row["ADDRESS_NO_C"].ToString();
								ADDRESS_COMPANY.VillageNo = row["MooNo_C"].ToString();
								ADDRESS_COMPANY.Village = row["ViilageName_C"].ToString();
								ADDRESS_COMPANY.Lane = row["SoiName_C"].ToString();
								ADDRESS_COMPANY.Road = row["RoadName_C"].ToString();
								ADDRESS_COMPANY.SubDistrictCode = row["SubdisCode_C"].ToString();
								ADDRESS_COMPANY.SubDistrict = row["SubdisName_C"].ToString();
								ADDRESS_COMPANY.DistrictCode = row["DisCode_C"].ToString();
								ADDRESS_COMPANY.District = row["DisName_C"].ToString();
								ADDRESS_COMPANY.ProvinceCode = row["ProvCode_C"].ToString();
								ADDRESS_COMPANY.Province = row["ProvName_C"].ToString();
								ADDRESS_COMPANY.PostalCode = row["PostCode_C"].ToString();
								ADDRESS_COMPANY_MASTER.Add(ADDRESS_COMPANY);
								DebtorDetail.ADDRESS_COMPANY = ADDRESS_COMPANY_MASTER;

								List<ADDRESS_HOUSE> ADDRESS_HOUSE_MASTER = new List<ADDRESS_HOUSE>();
								ADDRESS_HOUSE ADDRESS_HOUSE = new ADDRESS_HOUSE();

								ADDRESS_HOUSE.HouseNo = row["ADDRESS_NO_H"].ToString();
								ADDRESS_HOUSE.VillageNo = row["MooNo_H"].ToString();
								ADDRESS_HOUSE.Village = row["ViilageName_H"].ToString();
								ADDRESS_HOUSE.Lane = row["SoiName_H"].ToString();
								ADDRESS_HOUSE.Road = row["RoadName_H"].ToString();
								ADDRESS_HOUSE.SubDistrictCode = row["SubdisCode_H"].ToString();
								ADDRESS_HOUSE.SubDistrict = row["SubdisName_H"].ToString();
								ADDRESS_HOUSE.DistrictCode = row["DisCode_H"].ToString();
								ADDRESS_HOUSE.District = row["DisName_H"].ToString();
								ADDRESS_HOUSE.ProvinceCode = row["ProvCode_H"].ToString();
								ADDRESS_HOUSE.Province = row["ProvName_H"].ToString();
								ADDRESS_HOUSE.PostalCode = row["PostCode_H"].ToString();



								ADDRESS_HOUSE_MASTER.Add(ADDRESS_HOUSE);
								DebtorDetail.ADDRESS_HOUSE = ADDRESS_HOUSE_MASTER;
								DebtorDetailMaster.Add(DebtorDetail);
							}
							

							DebtorModel DebtorModel = new DebtorModel();
							DebtorModel.StatusCode = "200";
							DebtorModel.Message = "Success.";
							DebtorModel.Payload = DebtorDetailMaster;
							

							string jsonReturn = JsonConvert.SerializeObject(DebtorModel);

							return jsonReturn;

						}
						else
						{
							Error.StatusCode = "4041";
							Error.Message = "Contract Not Found.";
							Error.Payload = data2_val.Distinct().ToArray();
							var jsonReturn = JsonConvert.SerializeObject(Error);
							return jsonReturn;
						}
					}
					
				}
				catch (Exception ex)
				{
					Error Error = new Error();
					Error.StatusCode = "500";
					Error.Message = "Internal Server Error.";
					var jsonReturn = JsonConvert.SerializeObject(Error);

					var jsonParsed2 = JObject.Parse(jsonReturn);
					jsonParsed2.Properties().Where(attr => attr.Name == "Payload").First().Remove();
					jsonReturn = jsonParsed2.ToString();

					return jsonReturn;
				}
			}
		}

	}
}
