using ArgonautCore.Network.Health.Models;
using Microsoft.AspNetCore.Mvc;

namespace YtDownloader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public ActionResult<HealthStatus> GetHealthStatus()
        {
            return Ok(new HealthStatus("Youtube Downloader Service", Status.Healthy)
            {
                Description = "Easily convert and download youtube videos to mp3 or mp4, without viruses, ads and the highest bitrate."
            });
        }
    }
}