using System.Text.Json.Serialization;

namespace NativeMapVote.Config
{
    public class NominationsConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("max")]
        public int MaxNominations { get; set; } = 10;
    }
}
