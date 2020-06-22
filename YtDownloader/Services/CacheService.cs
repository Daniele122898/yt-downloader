using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YtDownloader.Configurations;
using YtDownloader.Dtos;
using YtDownloader.Helper;

namespace YtDownloader.Services
{
    public class CacheService
    {
        private readonly ILogger<CacheService> _log;
        private readonly ConcurrentDictionary<string, VideoInfo> _fileMap = new ConcurrentDictionary<string, VideoInfo>();
        
        
        // ReSharper disable once NotAccessedField.Local
        private readonly Timer _timer;

        public CacheService(
            IOptions<DownloadConfig> downloadConfig,
            ILogger<CacheService> log)
        {
            _log = log;
            var cleanupCooldownHours = downloadConfig?.Value?.CleanupCooldownHours ?? 24;

            if (!Directory.Exists(PathHelper.OutputPath))
                throw new DirectoryNotFoundException($"Couldn't find Output directory at: {PathHelper.OutputPath}!");

            // Clean directory on restart
            _log.LogInformation("Cleaning Output directory...");
            foreach (var file in Directory.EnumerateFiles(PathHelper.OutputPath))
            {
                File.Delete(file);
            }
            
            _log.LogInformation("Initializing cleanup timer");
            _timer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(cleanupCooldownHours), 
                TimeSpan.FromMinutes(cleanupCooldownHours));
        }
        
        private void CleanupCache(object _)
        {
            _log.LogInformation("Starting file cleanup...");
            var values = _fileMap.Values;
            _fileMap.Clear();
            foreach (var file in values)
            {
                string path = PathHelper.GenerateFilePath(file.FileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            _log.LogInformation("Finished file cleanup...");
        }

        public bool TryGetFile(string ytId, out VideoInfo info)
            => _fileMap.TryGetValue(ytId, out info);

        public bool TryAddFile(string ytId, VideoInfo info)
            => _fileMap.TryAdd(ytId, info);
    }
}