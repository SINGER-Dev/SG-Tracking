namespace WebApplication5.Model
{
	public class DebtorModel
	{
		
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<DebtorDetail> Payload { get; set; }
	}

	public class RequestDebtor
	{
		public string? contract { get; set; } = string.Empty;
		public string? debtor_type { get; set; } = string.Empty;
	}


	public class DebtorDetail
	{
		public string? debtor_type { get; set; } = string.Empty;
		public string? cus_name { get; set; } = string.Empty;
		public string? id_card { get; set; } = string.Empty;
		public string? age { get; set; } = string.Empty;
		public string? professional_group { get; set; } = string.Empty;
		public string? occupation { get; set; } = string.Empty;
		public string? tel { get; set; } = string.Empty;
		public string? Referral1 { get; set; } = string.Empty;
		public string? Referral2 { get; set; } = string.Empty;
		public List<ADDRESS_CURRENT> ADDRESS_CURRENT { get; set; }
		public List<ADDRESS_COMPANY> ADDRESS_COMPANY { get; set; }
		public List<ADDRESS_HOUSE> ADDRESS_HOUSE { get; set; }

	}

	public class ADDRESS_CURRENT
	{
		public string? HouseNo {get; set;} = string.Empty;
		public string? VillageNo {get; set;} = string.Empty;
		public string? Village {get; set;} = string.Empty;
		public string? Lane {get; set;} = string.Empty;
		public string? Road {get; set;} = string.Empty;
		public string? SubDistrictCode { get; set; } = string.Empty;
		public string? SubDistrict {get; set;} = string.Empty;
		public string? DistrictCode { get; set; } = string.Empty;
		public string? District {get; set;} = string.Empty;
		public string? ProvinceCode { get; set; } = string.Empty;
		public string? Province {get; set;} = string.Empty;
		public string? PostalCode { get; set; } = string.Empty;
	}

	public class ADDRESS_COMPANY
	{
		public string? HouseNo { get; set; } = string.Empty;
		public string? VillageNo { get; set; } = string.Empty;
		public string? Village { get; set; } = string.Empty;
		public string? Lane { get; set; } = string.Empty;
		public string? Road { get; set; } = string.Empty;
		public string? SubDistrictCode { get; set; } = string.Empty;
		public string? SubDistrict { get; set; } = string.Empty;
		public string? DistrictCode { get; set; } = string.Empty;
		public string? District { get; set; } = string.Empty;
		public string? ProvinceCode { get; set; } = string.Empty;
		public string? Province { get; set; } = string.Empty;
		public string? PostalCode { get; set; } = string.Empty;
	}

	public class ADDRESS_HOUSE
	{
		public string? HouseNo { get; set; } = string.Empty;
		public string? VillageNo { get; set; } = string.Empty;
		public string? Village { get; set; } = string.Empty;
		public string? Lane { get; set; } = string.Empty;
		public string? Road { get; set; } = string.Empty;
		public string? SubDistrictCode { get; set; } = string.Empty;
		public string? SubDistrict { get; set; } = string.Empty;
		public string? DistrictCode { get; set; } = string.Empty;
		public string? District { get; set; } = string.Empty;
		public string? ProvinceCode { get; set; } = string.Empty;
		public string? Province { get; set; } = string.Empty;
		public string? PostalCode { get; set; } = string.Empty;
	}

	
	
}
