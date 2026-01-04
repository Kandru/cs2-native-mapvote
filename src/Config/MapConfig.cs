using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class MapConfig
    {
        [JsonPropertyName("type")] 
        public int Type { get; set; }
        
        [JsonPropertyName("times_played")] 
        public int TimesPlayed { get; set; }
        
        [JsonPropertyName("votes_positive")] 
        public int VotesPositive { get; set; }
        
        [JsonPropertyName("votes_negative")] 
        public int VotesNegative { get; set; }
    }
}
