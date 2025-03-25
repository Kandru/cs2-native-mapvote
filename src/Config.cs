using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;

namespace NativeMapVote
{
    public class MapConfig
    {
        // map type (0 = local, 1 = workshop)
        [JsonPropertyName("type")] public int Type { get; set; }
        // amount of plays
        [JsonPropertyName("times_played")] public int TimesPlayed { get; set; }
        // amount of positive votes
        [JsonPropertyName("votes_positive")] public int VotesPositive { get; set; }
        // amount of negative votes
        [JsonPropertyName("votes_negative")] public int VotesNegative { get; set; }
    }

    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // rtv enabled
        [JsonPropertyName("rtv_enabled")] public bool RtvEnabled { get; set; } = true;
        // rtv vote duration
        [JsonPropertyName("rtv_vote_duration")] public int RtvVoteDuration { get; set; } = 30;
        // rtv cooldown
        [JsonPropertyName("rtv_cooldown")] public int RtvCooldown { get; set; } = 60;
        // rtv success server commands
        [JsonPropertyName("rtv_success_command")] public string RtvSuccessCommand { get; set; } = "mp_halftime false; mp_maxrounds 1";
        // nominations enabled
        [JsonPropertyName("nominations_enabled")] public bool NominationsEnabled { get; set; } = true;
        // maximum number of nominations
        [JsonPropertyName("nominations_max")] public int MaxNominations { get; set; } = 10;
        // amount of top maps to show
        [JsonPropertyName("endmap_vote_amount_maps")] public int EndmapVoteAmountMaps { get; set; } = 10;
        // changelevel enabled
        [JsonPropertyName("changelevel_enabled")] public bool ChangelevelEnabled { get; set; } = true;
        // changelevel vote duration
        [JsonPropertyName("changelevel_vote_duration")] public int ChangelevelVoteDuration { get; set; } = 30;
        // changelevel cooldown
        [JsonPropertyName("changelevel_cooldown")] public int ChangelevelCooldown { get; set; } = 60;
        // changelevel on round end (else instantly)
        [JsonPropertyName("changelevel_on_round_end")] public bool ChangelevelOnRoundEnd { get; set; } = false;
        // feedbackvote enable
        [JsonPropertyName("feedbackvote_enabled")] public bool FeedbackVoteEnabled { get; set; } = true;
        // feedbackvote duration
        [JsonPropertyName("feedbackvote_duration")] public int FeedbackVoteDuration { get; set; } = 30;
        // feedbackvote maximum delay
        [JsonPropertyName("feedbackvote_max_delay")] public int FeedbackVoteMaxDelay { get; set; } = 10;
        // map list
        [JsonPropertyName("maps")] public Dictionary<string, MapConfig> Maps { get; set; } = [];
    }

    public partial class NativeMapVote : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }

        private void AddOrUpdateMapConfig(string mapName, int type)
        {
            if (!Config.Maps.ContainsKey(mapName.ToLower()))
            {
                Config.Maps.Add(mapName.ToLower(), new MapConfig());
            }
            Config.Maps[mapName.ToLower()].Type = type;
            Config.Maps[mapName.ToLower()].TimesPlayed++;
        }

        private void SetMapVoteFeedback(string mapName, bool positive, int amount = 1)
        {
            if (!Config.Maps.TryGetValue(mapName.ToLower(), out MapConfig? value)) return;
            if (positive)
                value.VotesPositive += amount;
            else
                value.VotesNegative += amount;
        }
    }
}
