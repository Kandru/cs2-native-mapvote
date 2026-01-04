using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class ChangelevelConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("sfui_string")]
        public string SfuiString { get; set; } = "#SFUI_vote_changelevel";

        [JsonPropertyName("vote_duration")]
        public int VoteDuration { get; set; } = 30;

        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; } = 60;

        [JsonPropertyName("on_round_end")]
        public bool OnRoundEnd { get; set; } = false;
    }
}
