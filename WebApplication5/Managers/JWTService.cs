using System;
using System.Security.Claims;
using System.Collections.Generic;
using WebApplication5.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using static System.Net.Mime.MediaTypeNames;
using WebApplication5.Controllers;

namespace WebApplication5.Managers
{
	public class JWTService : IAuthService
	{

		public JWTService()
		{
			var builder = new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory())
						.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
			IConfiguration _configuration = builder.Build();
			SecretKey = _configuration.GetConnectionString("SecretKey");
		}

		public string SecretKey { get; set; } = "kNN4PkaHN5ajtCjxeASoENrUBVVkorlfSXWUTJbvGlA="; //https://www.digitalsanctuary.com/aes-key-generator-free

		public bool IsTokenValid(string token)
		{
			if (string.IsNullOrEmpty(token))
				throw new ArgumentException("Given token is null or empty.");

			TokenValidationParameters tokenValidationParameters = GetTokenValidationParameters();

			JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
			try
			{
				ClaimsPrincipal tokenValid = jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}


		public IEnumerable<Claim> GetTokenClaims(string token)
		{
			if (string.IsNullOrEmpty(token))
				throw new ArgumentException("Given token is null or empty.");

			TokenValidationParameters tokenValidationParameters = GetTokenValidationParameters();

			JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
			try
			{
				ClaimsPrincipal tokenValid = jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
				return tokenValid.Claims;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public string GenerateToken(IAuthContainerModel model)
		{
			if (model == null || model.Claims == null || model.Claims.Length == 0)
				throw new ArgumentException("Arguments to create token are not valid.");

			SecurityTokenDescriptor securityTokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(model.Claims),
				Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(model.ExpireMinutes)),
				SigningCredentials = new SigningCredentials(GetSymmetricSecurityKey(), model.SecurityAlgorithm)
			};

			JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
			SecurityToken securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);
			string token = jwtSecurityTokenHandler.WriteToken(securityToken);

			return token;
		}

		private SecurityKey GetSymmetricSecurityKey()
		{
			byte[] symmetricKey = Convert.FromBase64String(SecretKey);
			return new SymmetricSecurityKey(symmetricKey);
		}

		private TokenValidationParameters GetTokenValidationParameters()
		{
			return new TokenValidationParameters()
			{
				ValidateIssuer = false,
				ValidateAudience = false,
				IssuerSigningKey = GetSymmetricSecurityKey()
			};
		}

		public bool IsTokenExpired(string accessToken)
		{
			try
			{
				int spaceIndex = accessToken.IndexOf(' ');
				accessToken = accessToken.Substring(spaceIndex + 1);

				// Create a token validation parameters object
				var tokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = false,               // Validate the token's issuer (iss)
					ValidateAudience = false,             // Validate the token's audience (aud)
					ValidateLifetime = true,             // Check token expiration
					ValidateIssuerSigningKey = true,     // Validate the signature
					IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(SecretKey))
				};

				// Try to validate and decode the token
				var tokenHandler = new JwtSecurityTokenHandler();
				var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken validatedToken);

				return false;
				// You can access the claims in the token like this:
				// var claims = principal.Claims;
			}
			catch (SecurityTokenExpiredException)
			{
				return true;
			}
			catch (SecurityTokenInvalidSignatureException)
			{
				return true;
			}
			catch (SecurityTokenException)
			{
				return true;
			}
		}

		public DateTime ConvertTimestampToDate(DateTime expirationTime)
		{
			TimeZoneInfo thailandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Thailand's time zone ID

			return TimeZoneInfo.ConvertTime(expirationTime, thailandTimeZone);
		}
	

	}
}
