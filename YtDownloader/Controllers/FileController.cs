using Microsoft.AspNetCore.Mvc;
using YtDownloader.Helper;
using YtDownloader.Services;

namespace YtDownloader.Controllers
{
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly CacheService _cacheService;

        public FileController(CacheService cacheService)
        {
            _cacheService = cacheService;
        }
        
        [HttpGet("[controller]/{fileNameAndExtension}", Name = "GetFile")]
        public IActionResult GetFile(string fileNameAndExtension)
        {
            string ytId = fileNameAndExtension.Remove(fileNameAndExtension.IndexOf('.'));
            if (!_cacheService.TryGetFile(ytId, out string filename) || !System.IO.File.Exists(PathHelper.GenerateFilePath(filename)))
                return NotFound();
            
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = false, // Have it as attachment to force the browser to download it
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            return PhysicalFile(PathHelper.GenerateFilePath(filename), "audio/mpeg");
        }
    }
}