using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using WebApplication5.Model;

namespace WebApplication5.Managers
{
	public class ValidateCenter
	{
		private string secretKey;

		SqlConnection connection = new SqlConnection();
		SqlCommand sqlCommand;
		SqlDataAdapter dtAdapter = new SqlDataAdapter();
		String strConnString, strSQL;
		
		public string myConnectionString1, myConnectionString2, myConnectionString3, ApplicationID;


		public ValidateCenter()
		{
			this.secretKey = secretKey;
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			myConnectionString1 = _configuration.GetConnectionString("strConnString1");
			myConnectionString2 = _configuration.GetConnectionString("strConnString2");
			myConnectionString3 = _configuration.GetConnectionString("strConnString3");
			
			ApplicationID = _configuration.GetConnectionString("ApplicationID");
		}

		public bool CHK_USER_TRACKING(string EMP_CODE)
		{
			DataTable dt = new DataTable();
			strConnString = myConnectionString3;
			connection.ConnectionString = strConnString;
			connection.Open();

			strSQL = "SELECT [Auth_Users].[UserID] FROM [SG-AUTHORIZE].[dbo].[Auth_UserRoles] LEFT JOIN [SG-AUTHORIZE].[dbo].[Auth_Users] ON [Auth_Users].UserID = [Auth_UserRoles].UserID where [Auth_Users].[EMP_CODE] = @EMP_CODE AND [Auth_UserRoles].ApplicationID = @ApplicationID AND [Auth_UserRoles].Status = 1 AND [Auth_Users].Status = 1";


			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.Parameters.Add("@EMP_CODE", SqlDbType.VarChar);
			sqlCommand.Parameters["@EMP_CODE"].Value = EMP_CODE;

			sqlCommand.Parameters.Add("@ApplicationID", SqlDbType.Int);
			sqlCommand.Parameters["@ApplicationID"].Value = ApplicationID;

			sqlCommand.CommandType = CommandType.Text;
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool GET_ARM_CHECK_ACC(string ARM_ACC_NO)
		{
			//เช็คเลขสัญญา
			DataTable dt = new DataTable();

			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();

			strSQL = "ARM_CHECK_ACC";
			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.CommandType = CommandType.StoredProcedure;
			sqlCommand.Parameters.AddWithValue("@ACC", ARM_ACC_NO);
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();
			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool GET_ARM_CHECK_ACC_ACTIVE_AND_CLOSED(string ARM_ACC_NO)
		{
			//เช็คเลขสัญญา
			DataTable dt = new DataTable();

			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();

			strSQL = "ARM_CHECK_ACC_CLOSED";
			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.CommandType = CommandType.StoredProcedure;
			sqlCommand.Parameters.AddWithValue("@ACC", ARM_ACC_NO);
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();
			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public List<string> DuplicateEmp(List<string> Emp)
		{
			List<string> DuplicateEmp = new List<string>();

			var duplicates = Emp.GroupBy(x => x)
							  .Where(g => g.Count() > 1)
							  .Select(y => y.Key)
							  .ToList();
			//ถ้ามีข้อมูลซ้ำ
			foreach (var duplicate in duplicates)
			{
				DuplicateEmp.Add(duplicate.ToString());
			}
			DuplicateEmp.Distinct();
			return DuplicateEmp;
		}

		public List<string> DuplicateContract(List<string> Contract)
		{
			List<string> DuplicateContract = new List<string>();

			var duplicates = Contract.GroupBy(x => x)
							  .Where(g => g.Count() > 1)
							  .Select(y => y.Key)
							  .ToList();
			//ถ้ามีข้อมูลซ้ำ
			foreach (var duplicate in duplicates)
			{
				DuplicateContract.Add(duplicate.ToString());
			}
			DuplicateContract.Distinct();
			return DuplicateContract;
		}

		

		public bool CHECK_CALL_DUP(string CUS_ID , string CREATED_DATE)
		{
			DataTable dt = new DataTable();

			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();

			strSQL = "CHECK_CALL_DUPPLICATE2";
			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.CommandType = CommandType.StoredProcedure;
			sqlCommand.Parameters.AddWithValue("@CUS_NIC", CUS_ID);
			sqlCommand.Parameters.AddWithValue("@CREATED_DATE", CREATED_DATE);
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);
			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count <= 0)
			{
				return true;
			}
			else
			{
				return false;
			}

		}

		public bool CHK_PERMISSION_SYNCFC( string ARM_ACC_NO , string EMP_CODE)
		{
			DataTable dt = new DataTable();
			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();
			strSQL = "SELECT ARM_ACC_NO FROM Work_Plan WHERE User_ID_TO = @EMP_CODE and ARM_ACC_NO = @ARM_ACC_NO";


			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.Parameters.Add("@EMP_CODE", SqlDbType.VarChar);
			sqlCommand.Parameters["@EMP_CODE"].Value = EMP_CODE;

			sqlCommand.Parameters.Add("@ARM_ACC_NO", SqlDbType.VarChar);
			sqlCommand.Parameters["@ARM_ACC_NO"].Value = ARM_ACC_NO;

			sqlCommand.CommandType = CommandType.Text;
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool CHK_RECORD_CONTRACT(string ARM_ACC_NO, string ARM_RECORD_CALL_ID)
		{
			DataTable dt = new DataTable();
			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();
			strSQL = "SELECT [ARM_RECORD_CALL_ID] FROM [AR_COLLECTION].[dbo].[ARM_T_ARRecordCall] WHERE [ARM_RECORD_CALL_ID] = @ARM_RECORD_CALL_ID and [ARM_ACC_NO] = @ARM_ACC_NO";


			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.Parameters.Add("@ARM_RECORD_CALL_ID", SqlDbType.Int);
			sqlCommand.Parameters["@ARM_RECORD_CALL_ID"].Value = ARM_RECORD_CALL_ID;

			sqlCommand.Parameters.Add("@ARM_ACC_NO", SqlDbType.VarChar);
			sqlCommand.Parameters["@ARM_ACC_NO"].Value = ARM_ACC_NO;

			sqlCommand.CommandType = CommandType.Text;
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool CHK_RECORD_CONTRACT_FILE(string ARM_ACC_NO, string ARM_RECORD_CALL_ID, string ARM_TRACK_FILE_ID)
		{
			DataTable dt = new DataTable();
			strConnString = myConnectionString2;
			connection.ConnectionString = strConnString;
			connection.Open();
			strSQL = "SELECT [ARM_M_TRACK_FILE].ARM_TRACK_FILE_ID FROM [AR_COLLECTION].[dbo].[ARM_M_TRACK_FILE] LEFT JOIN [AR_COLLECTION].[dbo].[ARM_T_ARRecordCall] ON [ARM_T_ARRecordCall].[ARM_RECORD_CALL_ID] = [ARM_M_TRACK_FILE].ARM_RECORD_CALL_ID WHERE [ARM_T_ARRecordCall].[ARM_RECORD_CALL_ID] = @ARM_RECORD_CALL_ID AND [ARM_T_ARRecordCall].[ARM_ACC_NO] = @ARM_ACC_NO AND [ARM_M_TRACK_FILE].ARM_TRACK_FILE_ID = @ARM_TRACK_FILE_ID";


			sqlCommand = new SqlCommand(strSQL, connection);
			sqlCommand.Parameters.Add("@ARM_RECORD_CALL_ID", SqlDbType.Int);
			sqlCommand.Parameters["@ARM_RECORD_CALL_ID"].Value = ARM_RECORD_CALL_ID;

			sqlCommand.Parameters.Add("@ARM_ACC_NO", SqlDbType.VarChar);
			sqlCommand.Parameters["@ARM_ACC_NO"].Value = ARM_ACC_NO;

			sqlCommand.Parameters.Add("@ARM_TRACK_FILE_ID", SqlDbType.Int);
			sqlCommand.Parameters["@ARM_TRACK_FILE_ID"].Value = ARM_TRACK_FILE_ID;
			
			sqlCommand.CommandType = CommandType.Text;
			dtAdapter.SelectCommand = sqlCommand;
			dtAdapter.Fill(dt);

			sqlCommand.Parameters.Clear();
			connection.Close();

			if (dt.Rows.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
