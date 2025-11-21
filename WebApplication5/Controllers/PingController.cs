using Microsoft.AspNetCore.Mvc;

namespace WebApplication5.Controllers
{
    public class PingController : ControllerBase
    {
        [HttpGet("/Ping")]
        public IActionResult Ping()
        {
            return Ok();
        }


        [HttpGet("/Version")]
        public string GetVersion()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            IConfiguration _configuration = builder.Build();
            return "ECOMMERCE SERVICE / Ver. 0.01.2025.11.11.001";
        }
    }
}
