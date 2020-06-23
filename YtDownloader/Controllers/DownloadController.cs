using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YtDownloader.Dtos;
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
        
        [HttpPost]
        public async Task<IActionResult> DownloadVideo([FromBody] DownloadRequestDto downloadRequestDto)
        {
            if (!Uri.IsWellFormedUriString(downloadRequestDto.Url, UriKind.RelativeOrAbsolute))
                return BadRequest("Url must be well formed Uri string");
                
            var res = await _downloaderService.TryDownloadAsync(downloadRequestDto.Url, downloadRequestDto.ConversionTarget);
            if (res.HasError)
                return BadRequest(res.Err().Message.Get());

            var videoInfo = res.Some();
            
            // Setup metadata
            await _metaDataService.WriteMetadata(videoInfo, downloadRequestDto.MetaDataInfo, downloadRequestDto.ConversionTarget);

            return CreatedAtRoute("GetFile", 
                new {controller = "File", fileNameAndExtension = videoInfo.FileName}, videoInfo);
        }
    }
}