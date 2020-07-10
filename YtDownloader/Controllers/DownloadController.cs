using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YtDownloader.Dtos;
using YtDownloader.Models;
using YtDownloader.Services;

namespace YtDownloader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly DownloaderService _downloaderService;
        private readonly MetaDataService _metaDataService;

        public DownloadController(DownloaderService downloaderService, MetaDataService metaDataService)
        {
            _downloaderService = downloaderService;
            _metaDataService = metaDataService;
        }

        [HttpGet("json")]
        public async Task<ActionResult<VideoJson>> GetVideoJson(string ytUrl)
        {
            if (!Uri.IsWellFormedUriString(ytUrl, UriKind.RelativeOrAbsolute))
                return BadRequest("Url must be well formed Uri string");

            var data = await _downloaderService.GetYoutubeJsonData(ytUrl);

            if (data.HasError)
                return BadRequest(data.Err().Message.Get());

            return Ok(data.Some());
        }
        
        [HttpPost]
        public async Task<IActionResult> DownloadVideo([FromBody] DownloadRequestDto downloadRequestDto)
        {
            if (!Uri.IsWellFormedUriString(downloadRequestDto.Url, UriKind.RelativeOrAbsolute))
                return BadRequest("Url must be well formed Uri string");

            uint quality = downloadRequestDto.MetaDataInfo?.Quality ?? 720;

            if (!IsValidQuality(quality))
                return BadRequest("Invalid Video Quality setting");
                
            var res = await _downloaderService.TryDownloadAsync(downloadRequestDto.Url, downloadRequestDto.ConversionTarget, quality);
            if (res.HasError)
                return BadRequest(res.Err().Message.Get());

            var videoInfo = res.Some();
            
            // Setup metadata
            await _metaDataService.WriteMetadata(videoInfo, downloadRequestDto.MetaDataInfo, downloadRequestDto.ConversionTarget);

            return CreatedAtRoute("GetFile", 
                new {controller = "File", fileNameAndExtension = videoInfo.FileName}, videoInfo);
        }

        private static bool IsValidQuality(uint quality)
        {
            switch (quality)
            {
                case 1080:
                case 720:
                case 480:
                case 360:
                case 144:
                    return true;
                default:
                    return false;
            }
        }
    }
}