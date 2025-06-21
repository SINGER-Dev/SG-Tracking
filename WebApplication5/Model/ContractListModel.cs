using System.Diagnostics.Contracts;

namespace WebApplication5.Model
{
	public class ContractListModel
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<ContractListPayload> Payload { get; set; }
		

	}

	public class ContractListPayload
	{
		public List<string> ContractError { get; set; }
		public List<Contract> Contract { get; set; }
	}
	public class Contract
	{
		public string? contract { get; set; } = string.Empty;
		public List<string> productName = new List<string>();
		public string? customerName { get; set; } = string.Empty;
		public List<CurrentAddress> currentAddress { get; set; }
	}
	public class CurrentAddress
	{
		public string? address { get; set; } = string.Empty;
		public string? moo { get; set; } = string.Empty;
		public string? village { get; set; } = string.Empty;
		public string? soi { get; set; } = string.Empty;
		public string? road { get; set; } = string.Empty;
		public string? subdistrict { get; set; } = string.Empty;
		public string? district { get; set; } = string.Empty;
		public string? province { get; set; } = string.Empty;
		public string? zip_code { get; set; } = string.Empty;
		public string? LocationID { get; set; } = string.Empty;
		

	}
}
