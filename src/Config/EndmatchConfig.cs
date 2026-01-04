using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class MapDisplayOption
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class EndmatchConfig
    {
        [JsonPropertyName("max_maps")]
        public int MaxMaps { get; set; } = 10;

        [JsonPropertyName("display_nominations")]
        public bool DisplayNominations { get; set; } = true;

        [JsonPropertyName("display_most_played")]
        public MapDisplayOption DisplayMostPlayed { get; set; } = new() { Enabled = true, Amount = 3 };

        [JsonPropertyName("display_least_played")]
        public MapDisplayOption DisplayLeastPlayed { get; set; } = new() { Enabled = true, Amount = 2 };

        [JsonPropertyName("display_best_voted")]
        public MapDisplayOption DisplayBestVoted { get; set; } = new() { Enabled = true, Amount = 3 };

        [JsonPropertyName("display_worst_voted")]
        public MapDisplayOption DisplayWorstVoted { get; set; } = new() { Enabled = false, Amount = 0 };

        [JsonPropertyName("hide_worst_rating_threshold")]
        public double HideWorstRatingThreshold { get; set; } = 0.5;

        [JsonPropertyName("display_order")]
        public List<string> DisplayOrder { get; set; } = new()
        {
            "nominations",
            "most_played",
            "least_played",
            "best_voted",
            "worst_voted"
        };
    }
}
