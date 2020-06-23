using Microsoft.AspNetCore.Mvc;
using YtDownloader.Helper;
using YtDownloader.Services;

namespace YtDownloader.Controllers
{
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly CacheService _cacheService;
        private readonly MetaDataService _metaDataService;

        public FileController(CacheService cacheService, MetaDataService metaDataService)
        {
            _cacheService = cacheService;
            _metaDataService = metaDataService;
        }
        
        [HttpGet("[controller]/{fileNameAndExtension}", Name = "GetFile")]
        public IActionResult GetFile(string fileNameAndExtension)
        {
            if (!_cacheService.TryGetFile(fileNameAndExtension, out var videoInfo) || !System.IO.File.Exists(PathHelper.GenerateFilePath(videoInfo.FileName)))
                return NotFound();

            string path = PathHelper.GenerateFilePath(videoInfo.FileName);
            string fileName = _metaDataService.ConstructFilenameFromMetadata(path) ?? videoInfo.FileName;

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = false, // Have it as attachment to force the browser to download it
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            return PhysicalFile(PathHelper.GenerateFilePath(videoInfo.FileName), "audio/mpeg");
        }
    }
}