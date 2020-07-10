using System.IO;
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
            string extension = Path.GetExtension(fileNameAndExtension);
            string fileName = _metaDataService.ConstructFilenameFromMetadata(path, extension) ?? videoInfo.FileName;

            var cd = new System.Net.Mime.ContentDisposition
            {
                // FileName = WebUtility.UrlEncode(fileName),
                FileName = ToValidASCIIString(fileName),
                Inline = false, // Have it as attachment to force the browser to download it
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            return PhysicalFile(PathHelper.GenerateFilePath(videoInfo.FileName), "audio/mpeg");
        }

        private static string ToValidASCIIString(string fileName)
        {
            string asciiString = "";
            foreach (var c in fileName)
            {
                // ReSharper disable once RedundantCast
                if ((int) c < 128)
                    asciiString += c;
                else
                    asciiString += '_';
            }

            return asciiString;
        }
    }
}