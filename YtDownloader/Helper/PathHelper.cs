using System;
using System.IO;
using YtDownloader.Models.Enums;

namespace YtDownloader.Helper
{
    public static class PathHelper
    {
        public static string OutputPath { get; private set; }

        public static string GenerateFilePath(string filename)
            => Path.Combine(OutputPath, filename);

        public static void SetOutputPath(string path)
        {
            OutputPath = path;
        }
        
        public static string GetFilenameWithoutExtension(string fileNameWithExt)
        {
            int ind = fileNameWithExt.LastIndexOf('.');
            if (ind < 0)
                return null;

            return fileNameWithExt.Remove(ind);
        }

        public static string GenerateExtensionOnFilename(string filename, ConversionTarget target)
            => target switch
            {
                ConversionTarget.Mp3 => $"{filename}.mp3",
                ConversionTarget.Mp4 => $"{filename}.mp4",
                _                    => throw new ArgumentException($"Not handled {nameof(ConversionTarget)} enum type.")
            };
    }
}