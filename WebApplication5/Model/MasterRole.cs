namespace WebApplication5.Model
{
	public class MasterRole
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<MasterRoleList> Payload { get; set; }
	}

	public class MasterRoleList
	{
		public string? role { get; set; }
		public string? description { get; set; }

	}

	public class MasterRoleParameter
	{
		public string? company { get; set; }
		public string? team { get; set; }

	}

}
