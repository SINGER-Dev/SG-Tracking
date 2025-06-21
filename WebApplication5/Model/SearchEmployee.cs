using Microsoft.AspNetCore.Http;

namespace WebApplication5.Model
{
	public class SearchEmployee
	{
		public string? company { get; set; }
		public string? employee_id { get; set; }
		public string? filter_text { get; set; }
		public string? team { get; set; }
	}

	public class SearchEmpReturn
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<DataSearchEmpReturn> Payload { get; set; }

	}
	public class DataSearchEmpReturn
	{
		public string? employee_id { set; get; }
		public string? first_name { set; get; }
		public string? last_name { set; get; }
		public string? first_name_th { set; get; }
		public string? last_name_th { set; get; }
		public string? mobile_no { set; get; }
		public string[]? Role { get; set; }
	}
}
