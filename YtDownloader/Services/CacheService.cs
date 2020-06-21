using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YtDownloader.Configurations;

namespace YtDownloader.Services
{
    public class CacheService
    {
        private readonly ILogger<CacheService> _log;
        private readonly ConcurrentDictionary<string, string> _fileMap = new ConcurrentDictionary<string, string>();
        
        
        // ReSharper disable once NotAccessedField.Local
        private readonly Timer _timer;

        public CacheService(
            IOptions<DownloadConfig> downloadConfig,
            ILogger<CacheService> log)
        {
            _log = log;
            var outputPath = downloadConfig?.Value?.OutputPath ?? "./OutputFiles";
            var cleanupCooldownHours = downloadConfig?.Value?.CleanupCooldownHours ?? 24;

            if (!Directory.Exists(outputPath))
                throw new DirectoryNotFoundException($"Couldn't find Output directory at: {outputPath}!");

            // Clean directory on restart
            _log.LogInformation("Cleaning Output directory...");
            foreach (var file in Directory.EnumerateFiles(outputPath))
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
                if (File.Exists(file))
                    File.Delete(file);
            }
            _log.LogInformation("Finished file cleanup...");
        }

        public bool TryGetFile(string ytId, out string path)
            => _fileMap.TryGetValue(ytId, out path);

        public bool TryAddFile(string ytId, string path)
            => _fileMap.TryAdd(ytId, path);
    }
}