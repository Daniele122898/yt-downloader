using Microsoft.AspNetCore.Mvc;

namespace YtDownloader.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// Test Endpoint
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Test()
        {
            return Ok();
        }
    }
}