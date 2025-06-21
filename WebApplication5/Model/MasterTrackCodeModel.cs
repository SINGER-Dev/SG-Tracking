namespace WebApplication5.Model
{
	public class MasterTrackCodeModel
	{
		public string? StatusCode { get; set; }
		public string? Message { get; set; }
		public List<TrackResult> Payload { get; set; }
	}
	public class TrackResult
	{
		public string? track_code { get; set; } = string.Empty;
		public string? description { get; set; } = string.Empty;
	}

}
