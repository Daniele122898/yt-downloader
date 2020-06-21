using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArgonautCore.Lw;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using YtDownloader.Configurations;

namespace YtDownloader.Services
{
    public class DownloaderService
    {
        private const string _YT_DL_PATH = @".\Binaries\youtube-dl.exe";
        private const string _FFMPEG_PATH = "./Binaries/ffmpeg.exe";
        private readonly string _outputPath;

        public DownloaderService(IOptions<DownloadConfig> downloadConfig)
        {
            _outputPath = downloadConfig?.Value?.OutputPath ?? "./OutputFiles";
        }

        public async Task<Result<string, Error>> TryDownloadAsync(string url)
            => await Task.Run(async () => await this.TryDownload(url));
        
        public async Task<Result<string, Error>> TryDownload(string url)
        {
            await Task.Yield(); // Force a new thread.
            
            url = this.CleanYtLink(url);
            var ytId = this.GetYoutubeId(url);
            if (!ytId)
                return new Result<string, Error>(new Error("Not a valid YT link"));
            
            // Check Cache first

            var jsonCheck = this.YtJsonDownload(url);
            using var ytJsonProc = Process.Start(jsonCheck);
            if (ytJsonProc == null)
                return new Result<string, Error>(new Error("Failed to fetch video JSON info"));

            string output = await ytJsonProc.StandardOutput.ReadToEndAsync();
            ytJsonProc.WaitForExit();
            if (ytJsonProc.ExitCode != 0) 
                return new Result<string, Error>(new Error("Failed to fetch video JSON info"));

            IDictionary<string, JToken> jsonDict = JObject.Parse(output);
            if (jsonDict.ContainsKey("is_live") && !string.IsNullOrWhiteSpace(jsonDict["is_live"].Value<string>()))
                return new Result<string, Error>(new Error("Livestreams are not allowed"));

            var ytdlInfo = this.YtDl(url, ~ytId);
            using var ytDlProc = Process.Start(ytdlInfo);
            if (ytDlProc == null)
                return new Result<string, Error>(new Error("Failed to download video"));
            ytDlProc.WaitForExit();

            string filePath = $"{_outputPath}/{~ytId}.mp3";
            if (!File.Exists(filePath))
            {
                foreach (var file in Directory.EnumerateFiles($"{_outputPath}/*{~ytId}*"))
                {
                    File.Delete(file);
                }
                return new Result<string, Error>(new Error("Failed to download video"));
            }

            return filePath;
        }

        private string CleanYtLink(string url)
        {
            int index = url.IndexOf("&", StringComparison.Ordinal);
            if (index != -1)
                return url.Substring(0, index + 1);
            return url;
        }

        private Option<string> GetYoutubeId(string url)
        {
            int index = url.IndexOf("&", StringComparison.Ordinal);
            if (index != -1)
                url = url.Substring(0, index);
               
            if (url.Contains("www.youtube.com/watch?v="))
                return url.Substring(url.IndexOf("=", StringComparison.Ordinal) + 1).Trim();
            else if (url.Contains("youtu.be/"))
                return url.Substring(url.LastIndexOf("/", StringComparison.Ordinal) + 1).Trim();
                
            return Option.None<string>();
        }

        private ProcessStartInfo YtDl(string url, string name)
            => new ProcessStartInfo()
            {
                FileName = _YT_DL_PATH,
                Arguments =
                    $"-i -x --no-playlist --max-filesize 100m --audio-format mp3 --audio-quality 0 " +
                    $"--output \"{_outputPath}/{name}.%(ext)s\"  {url} --ffmpeg-location {_FFMPEG_PATH}"
            };
        
        
        private ProcessStartInfo YtJsonDownload(string url)
            => new ProcessStartInfo()
            {
                FileName = _YT_DL_PATH,
                Arguments = $"-J --flat-playlist {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

    }
}