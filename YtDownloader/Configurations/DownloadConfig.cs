namespace YtDownloader.Configurations
{
    public class DownloadConfig
    {
        public string OutputPath { get; set; }
        public int CleanupCooldownHours { get; set; }
        public string YtDlPath { get; set; }
        public string FfmpegPath { get; set; }
    }
}