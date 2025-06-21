using System.Diagnostics.Contracts;

namespace WebApplication5.Model
{
	public class SyncTaskPlan
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<M_Contract_Message> payload { get; set; }
	}

	public class SyncTaskPlanMaster
	{
		public string? employee_id { get; set; }
		public string? application_name { get; set; }
		public string? track_type { get; set; }
		public List<SyncTaskPlanRequest> payload { get; set; }
	}

	public class SyncTaskPlanRequest
	{
		public string? contract {get;set;}
		public string? taskplan_date {get;set;}
		public string? track_code {get;set;}
		public string? workplan_code {get;set;}
		public string? created_date { get; set; }
	}


}
