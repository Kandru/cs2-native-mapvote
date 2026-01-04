using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class EndmapConfig
    {
        [JsonPropertyName("vote_amount_total_maps")]
        public int VoteAmountMaps { get; set; } = 10;

        [JsonPropertyName("vote_amount_random_maps")]
        public int VoteAmountRandomMaps { get; set; } = 4;
    }
}
