using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;
using NativeMapVote.Config;

namespace NativeMapVote
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("debug")]
        public bool Debug { get; set; } = false;

        [JsonPropertyName("sfui")]
        public SfuiConfig Sfui { get; set; } = new();

        [JsonPropertyName("rtv")]
        public RtvConfig Rtv { get; set; } = new();

        [JsonPropertyName("nominations")]
        public NominationsConfig Nominations { get; set; } = new();

        [JsonPropertyName("endmatch")]
        public EndmatchConfig Endmatch { get; set; } = new();

        [JsonPropertyName("changelevel")]
        public ChangelevelConfig Changelevel { get; set; } = new();

        [JsonPropertyName("feedbackvote")]
        public FeedbackvoteConfig Feedbackvote { get; set; } = new();

        [JsonPropertyName("maps")]
        public Dictionary<string, MapConfig> Maps { get; set; } = [];
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

        private void AddMapConfig(string mapName, int type)
        {
            if (!Config.Maps.ContainsKey(mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)))
            {
                Config.Maps.Add(mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture), new MapConfig());
            }
            Config.Maps[mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)].Type = type;
        }

        private void UpdateMapPlayTime(string mapName)
        {
            if (!Config.Maps.ContainsKey(mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)))
            {
                return;
            }

            Config.Maps[mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)].TimesPlayed++;
        }

        private void SetMapVoteFeedback(string mapName, bool positive, int amount = 1)
        {
            if (!Config.Maps.TryGetValue(mapName.ToLower(System.Globalization.CultureInfo.CurrentCulture), out MapConfig? value))
            {
                return;
            }

            if (positive)
            {
                value.VotesPositive += amount;
            }
            else
            {
                value.VotesNegative += amount;
            }
        }
    }
}
