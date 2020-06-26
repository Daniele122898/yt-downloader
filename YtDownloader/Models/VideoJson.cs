using Newtonsoft.Json;

namespace YtDownloader.Models
{
    public class VideoJson
    {
        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("is_live")]
        public bool? IsLive { get; set; }
        
        [JsonProperty("thumbnail")] 
        public string ThumbnailUrl { get; set; }
    }
}