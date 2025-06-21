using System.Security.Cryptography;
using System.Text;

namespace WebApplication5.Managers
{
	public class EncryptTextService
	{
		public string EncryptText(string plainText, string secretKey)
		{

			byte[] keyBytes = Encoding.ASCII.GetBytes(secretKey); //Basic 16 https://acte.ltd/utils/randomkeygen
			byte[] iv = new byte[16]; // Initialization Vector

			byte[] encryptedBytes;
			using (var aes = Aes.Create())
			{
				aes.Key = keyBytes;
				aes.IV = iv;

				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

				using (var memoryStream = new System.IO.MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
					{
						byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
						cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
						cryptoStream.FlushFinalBlock();
					}

					encryptedBytes = memoryStream.ToArray();
				}
			}

			return Convert.ToBase64String(encryptedBytes);
		}

		public string DecryptText(string encryptedText, string secretKey)
		{

			byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
			byte[] iv = new byte[16]; // Initialization Vector

			byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
			byte[] decryptedBytes;
			using (var aes = Aes.Create())
			{
				aes.Key = keyBytes;
				aes.IV = iv;

				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				using (var memoryStream = new System.IO.MemoryStream(encryptedBytes))
				{
					using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
					{
						using (var decryptedMemoryStream = new System.IO.MemoryStream())
						{
							cryptoStream.CopyTo(decryptedMemoryStream);
							decryptedBytes = decryptedMemoryStream.ToArray();
						}
					}
				}
			}

			return Encoding.UTF8.GetString(decryptedBytes);
		}

	}
}
