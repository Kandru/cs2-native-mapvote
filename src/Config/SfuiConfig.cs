using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class SfuiConfig
    {
        [JsonPropertyName("string")]
        public string SfuiString { get; set; } = "#SFUI_vote_passed_changelevel";

        [JsonPropertyName("prefix")]
        public string SfuiPrefix { get; set; } = "= = = =>";

        [JsonPropertyName("suffix")]
        public string SfuiSuffix { get; set; } = "";
    }
}
