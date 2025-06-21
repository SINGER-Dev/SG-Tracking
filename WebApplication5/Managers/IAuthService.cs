using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebApplication5.Model;
namespace WebApplication5.Managers
{
	public interface IAuthService
	{
		string SecretKey { get; set; }
		bool IsTokenValid(string token);
		string GenerateToken(IAuthContainerModel model);
		bool IsTokenExpired(string token);
		DateTime ConvertTimestampToDate(DateTime timestamp);
		
		IEnumerable<Claim> GetTokenClaims(string token);
	}
}
