using Microsoft.AspNetCore.Mvc;

namespace WebApplication5.Model
{
	public class GetHeader
	{
		[FromHeader]
		public string? Token { get; set; }
	}
}
