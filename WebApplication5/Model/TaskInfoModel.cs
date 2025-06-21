namespace WebApplication5.Model
{
	public class TaskInfoModel
	{
		public string? contract { get; set; }
		public string? record_call_id { get; set; }
	}

	public class TaskInfo
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<Payload> Payload { get; set; }
		
	}

	public class Payload
	{
		public string? contract { get; set; }
		public string? track_address { get; set; }
		public string? lat { get; set; }
		public string? lng { get; set; }
		public string? plandate { get; set; }
		public string? tel { get; set; }
		public string? track_code { get; set; }
		public string? detail { get; set; }
		public string? custappointDate { get; set; }
		public string? appointmentAmt { get; set; }
		public string? datecollect { get; set; }
		public List<attchments> attchments { get; set; }
	}
	public class attchments
	{
		public string? attach_ref_id { get; set; }
		public string? file_name { get; set; }
		public string? file_name2 { get; set; }
		public string? file_desc { get; set; }
		public string? content_type { get; set;}
		public string? url { get; set; }
		public string? file_size { get; set; }
	
	}
}