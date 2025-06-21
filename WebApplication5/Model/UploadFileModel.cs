namespace WebApplication5.Model
{
	public class UploadFileModel
	{
		public IFormFile? file { get; set; }
		public string? contract { get; set; }
		public string? record_call_id { get; set; }
		public string? file_name { get; set; }
		public string? file_desc { get; set; }

	}
}
