using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class RtvConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("vote_duration")]
        public int VoteDuration { get; set; } = 30;

        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; } = 60;

        [JsonPropertyName("success_command")]
        public string SuccessCommand { get; set; } = "mp_halftime false; mp_maxrounds 1";
    }
}
