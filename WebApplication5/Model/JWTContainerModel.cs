using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace WebApplication5.Model
{
	public class JWTContainerModel : IAuthContainerModel
	{
		public int ExpireMinutes { get; set; } = 1440;
		public string? SecretKey { get; set; } = "kNN4PkaHN5ajtCjxeASoENrUBVVkorlfSXWUTJbvGlA="; //https://www.digitalsanctuary.com/aes-key-generator-free
		public string? SecurityAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256Signature;
		public Claim[] Claims { get; set; }
	}
}
