using System.ComponentModel.DataAnnotations;
using YtDownloader.Models;
using YtDownloader.Models.Enums;

namespace YtDownloader.Dtos
{
    public class DownloadRequestDto
    {
        [Required]
        public string Url { get; set; }

        public ConversionTarget ConversionTarget { get; set; } = ConversionTarget.Mp3;
        
        public MetaDataInfo MetaDataInfo { get; set; }
    }
}