namespace WebApplication5.Model
{
	public class MasterSubdistrict
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<Subdistrict> Payload { get; set; }
	}

	public class Subdistrict
	{
		public string? LocationID { get; set; }
		public string? subdistrict_code { get; set; }
		public string? subdistrict_name { get; set; }
		public string? district_code { get; set; }
		public string? district_name { get; set; }
		public string? province_code { get; set; }
		public string? province_name { get; set; }
		public string? PostCode { get; set;}
	}
}
