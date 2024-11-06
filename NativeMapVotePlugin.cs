using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using RconSharp;

namespace NativeMapVotePlugin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("fetch_mapgroup_over_rcon")]
    public bool FetchMapGroupOverRcon { get; set; } = false;
    [JsonPropertyName("rcon_port")]
    public int RconPort { get; set; } = 27015;
    [JsonPropertyName("maps")]
    public ImmutableList<string> Maps { get; set; } = ImmutableList<string>.Empty;
    
    [JsonPropertyName("callvote_enabled")]
    public bool CallVoteEnabled { get; set; } = true;
    [JsonPropertyName("callvote_allow_spectators")]
    public bool CallVoteAllowSpectators { get; set; } = true;
    [JsonPropertyName("callvote_duration")]
    public int CallVoteDuration { get; set; } = 60;
    [JsonPropertyName("callvote_cooldown")]
    public int CallVoteCooldown { get; set; } = 120;
    [JsonPropertyName("callvote_percentage")]
    public double CallVotePercentage { get; set; } = 0.6;
    [JsonPropertyName("callvote_message_interval")]
    public int CallVoteMessageInterval { get; set; } = 15;
    [JsonPropertyName("callvote_changemap_command")]
    public string CallVoteChangeMapCommand { get; set; } = "tv_stoprecord; map {map}";
    [JsonPropertyName("callvote_changemap_command_workshop")]
    public string CallVoteChangeMapCommandWorkshop { get; set; } = "tv_stoprecord; ds_workshop_changelevel {map}";
    
    [JsonPropertyName("rtv_allow_spectators")]
    public bool RtvAllowSpectators { get; set; } = true;
    [JsonPropertyName("rtv_cooldown")]
    public int RtvCooldown { get; set; } = 240;
    [JsonPropertyName("rtv_percentage")]
    public double RtvPercentage { get; set; } = 0.6;
    [JsonPropertyName("rtv_message_interval")]
    public int RtvMessageInterval { get; set; } = 15;
    [JsonPropertyName("rtv_duration")]
    public int RtvDuration { get; set; } = 60;
    [JsonPropertyName("rtv_end_match_command")]
    public string RtvEndMatchCommand { get; set; } = "mp_halftime false; mp_maxrounds 1";
}

public partial class NativeMapVotePlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Native Map Vote Plugin";
    public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it>";
    public override string ModuleVersion => "1.0.2";
    
    public PluginConfig Config { get; set; } = null!;

    private ChatMenu? _nominationMenuAllMaps;
    private ChatMenu? _callVoteMenuAllMaps;

    public NativeMapVotePlugin()
    {
        _callVoteChatVote = new(this);
        _rtvChatVote = new(this);
    }
    
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        if (Config.FetchMapGroupOverRcon) FetchMapGroupOverRcon();
        else OnMapGroupChange();

        _callVoteChatVote.Duration = config.CallVoteDuration;
        _callVoteChatVote.Cooldown = config.CallVoteCooldown;
        _callVoteChatVote.Percentage = config.CallVotePercentage;
        _callVoteChatVote.NotificationInterval = config.CallVoteMessageInterval;
        _callVoteChatVote.AllowSpectators = config.CallVoteAllowSpectators;
        _callVoteChatVote.Localizer.AnotherVoteRunning = Localizer["callVotes.anotherVoteRunning"];
        _callVoteChatVote.Localizer.ActiveCooldown = Localizer["callVotes.activeCooldown"];
        _callVoteChatVote.Localizer.SpectatorsNotAllowed = Localizer["callVotes.spectatorsNotAllowed"];
        
        _rtvChatVote.Duration = config.RtvDuration;
        _rtvChatVote.Cooldown = config.RtvCooldown;
        _rtvChatVote.Percentage = config.RtvPercentage;
        _rtvChatVote.NotificationInterval = config.RtvMessageInterval;
        _rtvChatVote.AllowSpectators = config.RtvAllowSpectators;
        _rtvChatVote.Localizer.AlreadyVoted = Localizer["rtv.alreadyVoted"];
        _rtvChatVote.Localizer.VotedSuccessfully = Localizer["rtv.votedSuccessfully"];
        _rtvChatVote.Localizer.VoteSucceeded = Localizer["rtv.voteSucceeded"];
        _rtvChatVote.Localizer.VoteFailed = Localizer["rtv.voteFailed"];
        _rtvChatVote.Localizer.VoteAlreadySucceeded = Localizer["rtv.alreadySucceeded"];
        _rtvChatVote.Localizer.AnotherVoteRunning = Localizer["rtv.anotherVoteRunning"];
        _rtvChatVote.Localizer.ActiveCooldown = Localizer["rtv.activeCooldown"];
        _rtvChatVote.Localizer.VoteStartedByPlayer = Localizer["rtv.voteStartedByPlayer"];
        _rtvChatVote.Localizer.VoteStartedByUnknownEntity = Localizer["rtv.voteStartedByUnknownEntity"];
        _rtvChatVote.Localizer.Notification = Localizer["rtv.status"];
        _rtvChatVote.Localizer.NotificationHint = Localizer["rtv.statusHint"];
        _rtvChatVote.Localizer.SpectatorsNotAllowed = Localizer["rtv.spectatorsNotAllowed"];
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventCsIntermission>(OnIntermission, HookMode.Pre);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        AddCommand("css_nom", "Nominates a map so that it appears in the map vote after the match ends", OnNominateCommand);
        AddCommand("css_nominate", "Nominates a map so that it appears in the map vote after the match ends", OnNominateCommand);
        AddCommand("css_noms", "Shows the current nominated maps that will appear in the map vote after the match ends", OnNominationsCommand);
        AddCommand("css_nominations", "Shows the current nominated maps that will appear in the map vote after the match ends", OnNominationsCommand);
        AddCommand("css_vote", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_callvote", "Starts a callvote to change the map to a specific map ater round end", OnCallVoteCommand);
        AddCommand("css_cv", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_changelevel", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_cl", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_map", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_level", "Starts a callvote to change the map to a specific map after round end", OnCallVoteCommand);
        AddCommand("css_rtv", "Starts a vote to end the match after round end, consequently starting a map vote", OnRtvCommand);
        AddCommand("css_skip", "Starts a vote to end the match after round end, consequently starting a map vote", OnRtvCommand);

        _callVoteChatVote.OnVoteSucceeded += OnCallVoteVoteSucceeded;
        _rtvChatVote.OnVoteSucceeded += OnRtvVoteSucceeded;
        
        Reset();
    }

    private void OnMapStart(string mapName)
    {
        if (Config.FetchMapGroupOverRcon) FetchMapGroupOverRcon();
        Reset();
    }
    
    private HookResult OnIntermission(EventCsIntermission @eventCsIntermission, GameEventInfo info)
    {
        UpdateEndMatchGroupVoteOptions();
        Reset();
        return HookResult.Continue;
    }

    private void OnMapGroupChange()
    {
        var nominateMenu = new ChatMenu(Localizer["nominations.chatMenuTitle"]);
        var callVoteMenu = new ChatMenu(Localizer["callVotes.chatMenuTitle"]);
        foreach (var mapName in Config.Maps)
        {
            nominateMenu.AddMenuOption(mapName, (player, _) =>
            {
                Nominate(mapName, player, null);
            });
            callVoteMenu.AddMenuOption(mapName, (player, _) =>
            {
                StartCallVote(mapName, player, null);
            });
        }
        _nominationMenuAllMaps = nominateMenu;
        _callVoteMenuAllMaps = callVoteMenu;
    }
    
    private void FetchMapGroupOverRcon()
    {
        var rconPasswordCvar = ConVar.Find("rcon_password");
        if (rconPasswordCvar == null || rconPasswordCvar.StringValue == null || rconPasswordCvar.StringValue.Length == 0)
        {
            Console.WriteLine("[NativeMapVotePlugin][WARNING] Fetching map list over RCON disabled due to disabled RCON (cvar rcon_password not set)!");
            return;
        }
        
        Task.Run(async () => {
            var client = RconClient.Create("127.0.0.1", Config.RconPort);
            await client.ConnectAsync();
            await client.AuthenticateAsync(rconPasswordCvar.StringValue);
            var output = await client.ExecuteCommandAsync("print_mapgroup_sv");
            client.Disconnect();
            
            if (string.IsNullOrEmpty(output))
            {
                Console.WriteLine("[NativeMapVotePlugin][WARNING] Fetching mapgroup over RCON failed!");
                return;
            }

            var lines = output.Split("\n");
            var mapNames = new List<string>();
            foreach (var line in lines)
            {
                if (line.Length == 0 || line.StartsWith("Map group:")) continue;
                if (line.StartsWith("No maps"))
                {
                    Console.WriteLine("[NativeMapVotePlugin][WARNING] No maps in map group found - plugin will not work this map!");
                    return;
                }

                var mapName = line.Replace("\t", "").Replace(" ", "");
                mapNames.Add(mapName);
            }

            if (mapNames.Count == 0)
            {
                Console.WriteLine("[NativeMapVotePlugin][ERROR] Could not parse map group over RCON!");
                return;
            }
            
            Config.Maps = ImmutableList<string>.Empty;
            Config.Maps = Config.Maps.AddRange(mapNames);
            OnMapGroupChange();
            
            Console.WriteLine("[NativeMapVotePlugin][INFO] Found " + Config.Maps.Count + " maps in map group, now used for voting!");
        });
    }

    private void Reset()
    {
        ResetNominations();
        ResetCallVotes();
        ResetRtv();
    }
}
