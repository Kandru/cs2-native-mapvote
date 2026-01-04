using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class FeedbackvoteConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("duration")]
        public int Duration { get; set; } = 0;

        [JsonPropertyName("max_delay")]
        public int MaxDelay { get; set; } = 10;
    }
}
