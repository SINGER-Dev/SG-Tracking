using Microsoft.AspNetCore.Http;

namespace WebApplication5.Model
{
	public class UploadFileRespone
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }

		public List<UploadFileResponePayload> Payload { get; set; }
	}
	public class UploadFileResponePayload
	{
		public string? attach_ref_id { get; set; }

	}
	
}
