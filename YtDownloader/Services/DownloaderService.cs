using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArgonautCore.Lw;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YtDownloader.Configurations;
using YtDownloader.Dtos;
using YtDownloader.Helper;
using YtDownloader.Models;
using YtDownloader.Models.Enums;

namespace YtDownloader.Services
{
    public class DownloaderService
    {
        private readonly CacheService _cacheService;
        private readonly string _cpuEncodeProcUsed;
        private readonly DownloadConfig _config;

        public DownloaderService(CacheService cacheService, IOptions<DownloadConfig> config)
        {
            _config = config.Value;
            _cacheService = cacheService;
#if DEBUG
            _cpuEncodeProcUsed = Environment.ProcessorCount.ToString();
#else
            _cpuEncodeProcUsed = Math.Min(1, Environment.ProcessorCount / 2).ToString();
#endif
        }

        /// <summary>
        /// Tries to download and convert video and return Filename with extension 
        /// </summary>
        public async Task<Result<VideoInfo, Error>> TryDownloadAsync(string url, ConversionTarget target, uint? quality = null)
            => await Task.Run(async () => await this.TryDownload(url, target, quality));

        /// <summary>
        /// Tries to download and convert video and return Filename with extension 
        /// </summary>
        public async Task<Result<VideoInfo, Error>> TryDownload(string url, ConversionTarget target, uint? quality = null)
        {
            await Task.Yield(); // Force a new thread.

            if (target == ConversionTarget.Mp4 && !quality.HasValue)
                return new Result<VideoInfo, Error>(new Error("Quality cannot be null if conversion target is mp4!"));

            url = CleanYtLink(url);
            var ytId = GetYoutubeId(url);
            if (!ytId)
                return new Result<VideoInfo, Error>(new Error("Not a valid YT link"));

            string fileName = PathHelper.GenerateExtensionOnFilename(~ytId, target, quality);
            // Check Cache first
            if (_cacheService.TryGetFile(fileName, out var cachedVideoInfo))
                return new Result<VideoInfo, Error>(cachedVideoInfo);

            var jsonCheck = YtJsonDownload(url);
            using var ytJsonProc = Process.Start(jsonCheck);
            if (ytJsonProc == null)
                return new Result<VideoInfo, Error>(new Error("Failed to fetch video JSON info"));

            string output = await ytJsonProc.StandardOutput.ReadToEndAsync();
            ytJsonProc.WaitForExit();
            if (ytJsonProc.ExitCode != 0)
                return new Result<VideoInfo, Error>(new Error("Failed to fetch video JSON info"));

            IDictionary<string, JToken> jsonDict = JObject.Parse(output);

            if (jsonDict.ContainsKey("is_live") && jsonDict["is_live"].Value<bool>())
                return new Result<VideoInfo, Error>(new Error("Livestreams are not allowed"));

            var ytdlInfo = YtDl(url, ~ytId, target, quality ?? 720);
            using var ytDlProc = Process.Start(ytdlInfo);
            if (ytDlProc == null)
                return new Result<VideoInfo, Error>(new Error("Failed to download video"));
            ytDlProc.WaitForExit();

            string filePath = PathHelper.GenerateFilePath(fileName);
            if (!File.Exists(filePath))
            {
                _cacheService.CleanupFilesNotInCache();
                return new Result<VideoInfo, Error>(new Error("Failed to download video"));
            }
            string videoTitle = jsonDict["title"].Value<string>();

            var videoInfo = new VideoInfo()
            {
                VideoTitle = videoTitle,
                FileName = fileName
            };

            // Add to cache service
            _cacheService.TryAddFile(fileName, videoInfo);

            return videoInfo;
        }

        public async Task<Result<VideoJson, Error>> GetYoutubeJsonData(string url)
        {
            await Task.Yield(); // Force a new thread.

            url = CleanYtLink(url);

            var jsonCheck = YtJsonDownload(url);
            using var ytJsonProc = Process.Start(jsonCheck);
            if (ytJsonProc == null)
                return new Result<VideoJson, Error>(new Error("Failed to fetch YT Json data."));

            string rawJson = await ytJsonProc.StandardOutput.ReadToEndAsync();
            ytJsonProc.WaitForExit();
            if (ytJsonProc.ExitCode != 0)
                return new Result<VideoJson, Error>(new Error("Failed to fetch YT Json data."));

            var videoJson = JsonConvert.DeserializeObject<VideoJson>(rawJson);
            return videoJson;
        }

        private static string CleanYtLink(string url)
        {
            int index = url.IndexOf("&", StringComparison.Ordinal);
            if (index != -1)
                return url.Substring(0, index + 1);
            return url;
        }

        private static Option<string> GetYoutubeId(string url)
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

        private ProcessStartInfo YtDl(string url, string name, ConversionTarget target, uint quality)
            => new ProcessStartInfo()
            {
                FileName = _config.YtDlPath,
                Arguments = GenerateArgumentList(url, name, target, quality)
            };

        private string GenerateArgumentList(string url, string name, ConversionTarget target, uint res = 720)
            => target switch
            {
                ConversionTarget.Mp3 =>
                $"-i -x --no-playlist --max-filesize 500m --audio-format mp3 --audio-quality 0 " +
                $"--output \"{PathHelper.OutputPath}/{name}.%(ext)s\"  {url} --ffmpeg-location {_config.FfmpegPath}",

                ConversionTarget.Mp4 => $"-f \"bestvideo[height<=?{res.ToString()}][fps<=?60][vcodec!=?vp9]+bestaudio/best\" -i --no-playlist --max-filesize 1000m " +
                                        $"--audio-quality 0 --recode-video mp4 --output \"{PathHelper.OutputPath}/{name}_{res.ToString()}.%(ext)s\" {url} " +
                                        $"--ffmpeg-location \"{_config.FfmpegPath}\" --postprocessor-args \"-threads {_cpuEncodeProcUsed}\"",

                _ => throw new ArgumentException($"Enum {nameof(target)} out of range.")
            };


        private ProcessStartInfo YtJsonDownload(string url)
            => new ProcessStartInfo()
            {
                FileName = _config.YtDlPath,
                Arguments = $"-J --flat-playlist {url}",
                RedirectStandardOutput = true,
                UseShellExecute = true
            };

    }
}
