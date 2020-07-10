using System;
using System.IO;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;
using YtDownloader.Dtos;
using YtDownloader.Helper;
using YtDownloader.Models;
using YtDownloader.Models.Enums;
using File = System.IO.File;

namespace YtDownloader.Services
{
    public class MetaDataService
    {
        private readonly HttpService _httpService;

        public MetaDataService(HttpService httpService)
        {
            _httpService = httpService;
        }
        
        public async Task WriteMetadata(VideoInfo videoInfo, MetaDataInfo info, ConversionTarget target)
        {
            string filePath = PathHelper.GenerateFilePath(videoInfo.FileName);
            if (!File.Exists(filePath))
                return; // just no-op

            var tFile = TagLib.File.Create(filePath);
            tFile.Tag.Title = string.IsNullOrWhiteSpace(info?.Title)
                ? (videoInfo.VideoTitle ?? PathHelper.GetFilenameWithoutExtension(videoInfo.FileName))
                : info.Title;

            if (target == ConversionTarget.Mp3 && !string.IsNullOrWhiteSpace(info?.Artists))
            {
                tFile.Tag.Performers = new string[]{info.Artists};
            }
            
            // Check if we even have to fetch and download album art :)
            if (tFile.Tag.Pictures?.Length > 0)
            {
                // Only fetch and set thumbnail once :)
                tFile.Save();
                return;
            }
            
            // Fetch Youtube thumbnail
            var ytId = PathHelper.GetFilenameWithoutExtension(videoInfo.FileName);
            string imageName = $"{ytId}.jpg";
            string imagePath = Path.Combine(PathHelper.OutputPath, imageName);
            try
            {
                await _httpService.DownloadAndSaveFile(new Uri($"https://i.ytimg.com/vi/{ytId}/maxresdefault.jpg"), imagePath);
            }
            catch (Exception)
            {
                // If we fail to download the image just return without it it's fine...
                tFile.Save();
                return;
            }
            
            // Add image as album cover :)
            var imageBytes = File.ReadAllBytes(imagePath);
            TagLib.Id3v2.AttachedPictureFrame cover = new AttachedPictureFrame()
            {
                Type = TagLib.PictureType.FrontCover,
                Description = "Cover",
                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                Data = imageBytes,
                TextEncoding = TagLib.StringType.UTF16
            };
            
            tFile.Tag.Pictures = new IPicture[]{ cover};
            
            tFile.Save();
            
            // Remove file if it exists.
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }

        public string GetFileTitleFromMetadata(string path)
        {
            if (!File.Exists(path))
                return null;

            using var tFile = TagLib.File.Create(path);
            return tFile.Tag.Title;
        }

        public string ConstructFilenameFromMetadata(string path, string extension)
        {
            string name = this.GetFileTitleFromMetadata(path);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return name + extension;
        }
    }
}