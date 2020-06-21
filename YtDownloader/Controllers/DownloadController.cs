using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YtDownloader.Services;

namespace YtDownloader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly DownloaderService _downloaderService;

        public DownloadController(DownloaderService downloaderService)
        {
            _downloaderService = downloaderService;
        }
        
        [HttpGet]
        public async Task<IActionResult> DownloadVideo(string url)
        {
            var res = await _downloaderService.TryDownloadAsync(url);
            if (res.HasError)
                return BadRequest(res.Err().Message.Get());
            
            return Ok(res.Some());
        }
    }
}