﻿using System.Security.Claims;

namespace WebApplication5.Model
{
	public interface IAuthContainerModel
	{
		string SecretKey { get; set; }
		string SecurityAlgorithm { get; set; }
		int ExpireMinutes { get; set; }
		Claim[] Claims { get; set; }

	}
}
