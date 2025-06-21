namespace WebApplication5.Model
{
	public class TokenReturn
	{
		
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public string? access_token { get; set; }
		public DateTime expired_date { get; set; }
		public string? user_id { get; set; }

		public string? id_card { get; set; }

		public string? mobile_no { get; set; }
		public string? name { get; set; }
		public string? position { get; set; }
		public string? company { get; set; }

		public string? first_name_en { get; set; }
		public string? last_name_en { get; set; }
		public string[]? Role { get; set; }
	}
}
