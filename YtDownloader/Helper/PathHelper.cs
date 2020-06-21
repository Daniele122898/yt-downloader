using System.IO;

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
    }
}