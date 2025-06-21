namespace WebApplication5.Model
{
	public class UpdateTaskRespone
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<UpdateReturn> Payload { get; set; }


	}
	public class UpdateReturn
	{
		public string? contract { get; set; }
		public string? record_call_id { get; set; }
	}

}
