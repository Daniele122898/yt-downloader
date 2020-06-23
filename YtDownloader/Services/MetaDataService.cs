using System.IO;
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
        public void WriteMetadata(VideoInfo videoInfo, MetaDataInfo info, ConversionTarget target)
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

            var imageBytes = File.ReadAllBytes(Path.Combine(PathHelper.OutputPath, "maxresdefault.jpg"));
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
        }

        public string GetFileTitleFromMetadata(string path)
        {
            if (!File.Exists(path))
                return null;

            using var tFile = TagLib.File.Create(path);
            return tFile.Tag.Title;
        }

        public string ConstructFilenameFromMetadata(string path)
        {
            string name = this.GetFileTitleFromMetadata(path);
            if (string.IsNullOrWhiteSpace(name))
                return null;
            
            using var tFile = TagLib.File.Create(path);
            if (tFile.MimeType.Contains("video"))
                return name + ".mp4";
            else
                return name + ".mp3";
        }
    }
}