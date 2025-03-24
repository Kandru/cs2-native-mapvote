using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;

namespace NativeMapVote
{
    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // rtv vote duration
        [JsonPropertyName("rtv_vote_duration")] public int RtvVoteDuration { get; set; } = 30;
        // rtv cooldown
        [JsonPropertyName("rtv_cooldown")] public int RtvCooldown { get; set; } = 60;
        // rtv success server commands
        [JsonPropertyName("rtv_success_command")] public string RtvSuccessCommand { get; set; } = "mp_halftime false; mp_maxrounds 1";
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
    }
}
