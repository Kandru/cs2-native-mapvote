using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using RconSharp;

namespace NativeMapVotePlugin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("fetch_mapgroup_over_rcon")]
    public bool FetchMapGroupOverRcon { get; set; } = false;
    [JsonPropertyName("rcon_port")] public int RconPort { get; set; } = 27015;
    [JsonPropertyName("maps")] public ImmutableList<string> Maps { get; set; } = ImmutableList<string>.Empty;
    
    [JsonPropertyName("callvote_enabled")]
    public bool CallVoteEnabled { get; set; } = true;
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
    
    [JsonPropertyName("rtv_cooldown")]
    public int RtvCooldown { get; set; } = 240;
    [JsonPropertyName("rtv_percentage")] public double RtvPercentage { get; set; } = 0.6;
    [JsonPropertyName("rtv_message_interval")]
    public int RtvMessageInterval { get; set; } = 15;
    [JsonPropertyName("rtv_duration")] public int RtvDuration { get; set; } = 60;
    [JsonPropertyName("rtv_end_match_command")]
    public string RtvEndMatchCommand { get; set; } = "mp_halftime false; mp_maxrounds 1";
}

public class NativeMapVotePlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Native Map Vote Plugin";
    public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it>";
    public override string ModuleVersion => "1.0.2";
    
    public PluginConfig Config { get; set; } = null!;

    private readonly HashSet<string> _nominatedMapNames = new();
    private readonly Dictionary<SteamID, string> _playerNominations = new();
    private string _callVoteMap = "";
    private string? _mapNextRound;
    private readonly ChatVote _callVoteChatVote;
    private readonly ChatVote _rtvChatVote;
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
        _callVoteChatVote.AllowSpectators = false;
        _callVoteChatVote.Localizer.AnotherVoteRunning = Localizer["callVotes.anotherVoteRunning"];
        _callVoteChatVote.Localizer.ActiveCooldown = Localizer["callVotes.activeCooldown"];
        
        _rtvChatVote.Duration = config.RtvDuration;
        _rtvChatVote.Cooldown = config.RtvCooldown;
        _rtvChatVote.Percentage = config.RtvPercentage;
        _rtvChatVote.NotificationInterval = config.RtvMessageInterval;
        _rtvChatVote.AllowSpectators = false;
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
        AddCommand("css_vote", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_callvote", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_cv", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_changelevel", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_cl", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_map", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_level", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_rtv", "Starts a vote to end the match immediately, consequently starting a map vote", OnRtvCommand);
        AddCommand("css_skip", "Starts a vote to end the match immediately, consequently starting a map vote", OnRtvCommand);

        _callVoteChatVote.OnVoteSucceeded += OnCallVoteVoteSucceeded;
        _rtvChatVote.OnVoteSucceeded += OnRtvVoteSucceeded;
        
        Reset();
    }

    private void OnMapStart(string mapName)
    {
        if (Config.FetchMapGroupOverRcon) FetchMapGroupOverRcon();
    }
    
    private HookResult OnIntermission(EventCsIntermission @eventCsIntermission, GameEventInfo info)
    {
        UpdateEndMatchGroupVoteOptions();
        Reset();
        return HookResult.Continue;
    }
    
    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_mapNextRound == null) return HookResult.Continue;
        Server.ExecuteCommand(IsNativeMap(_mapNextRound)
            ? Config.CallVoteChangeMapCommand.Replace("{map}", _mapNextRound)
            : Config.CallVoteChangeMapCommandWorkshop.Replace("{map}", _mapNextRound));
        _mapNextRound = null;
        return HookResult.Continue;
    }

    private void OnNominateCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (Config.Maps.Count == 0)
        {
            info.ReplyToCommand(Localizer["nominations.disabledEmptyMapgroup"]);
            return;
        }
        
        // if no argument was given, we can't possibly know what the user wants - give him all the options then
        if (info.ArgCount <= 1)
        {
            if (player == null)
            {
                info.ReplyToCommand(Localizer["nominations.beMoreSpecific"]);
                return;
            }

            if (_nominationMenuAllMaps == null) return;
            MenuManager.OpenChatMenu(player, _nominationMenuAllMaps);
            return;
        }
        
        // if the exact map name was given, we can use a shortcut
        var query = info.GetArg(1);
        if (Config.Maps.Contains(query))
        {
            Nominate(query, player, info);
            return;
        }
        
        var menu = new ChatMenu(Localizer["nominations.chatMenuTitle"]);
        foreach (var mapName in Config.Maps)
        {
            if (mapName.StartsWith(query) || mapName.Contains(query))
            {
                menu.AddMenuOption(mapName, (_, _) =>
                {
                    Nominate(mapName, player, info);
                }, _nominatedMapNames.Contains(mapName));
            }
        }
        
        if (menu.MenuOptions.Count == 0)
        {
            info.ReplyToCommand(Localizer["nominations.noMapFound"]);
            return;
        }

        // yay, shortcut time
        if (menu.MenuOptions.Count == 1)
        {
            Nominate(menu.MenuOptions[0].Text, player, info);
            return;
        }
        
        // if we deal with an RCON or internal command invocation, and we have multiple options, we cannot really show a menu unfortunately
        if (player == null)
        {
            info.ReplyToCommand(Localizer["nominations.couldNotIdentify"]);
            foreach (var option in menu.MenuOptions)
            {
                info.ReplyToCommand("- " + option.Text);
            }
            return;
        }
        
        MenuManager.OpenChatMenu(player, menu);
    }

    private void OnNominationsCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (_nominatedMapNames.Count == 0)
        {
            Server.PrintToChatAll(Localizer["nominations.noNominationsYet"]);
            Server.PrintToConsole(Localizer["nominations.noNominationsYet"]);
            return;
        }
        
        var reply = Localizer["nominations.nominationListPrefix"] + _nominatedMapNames.First();
        if (_nominatedMapNames.Count > 1)
        {
            bool first = true;
            foreach (var mapName in _nominatedMapNames)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                reply += ", " + mapName;
            }
        }
        Server.PrintToChatAll(reply);
        Server.PrintToConsole(reply);
    }

    private void OnCallVoteCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!Config.CallVoteEnabled)
        {
            info.ReplyToCommand(Localizer["callVotes.disabledManually"]);
            return;
        }
        
        if (Config.Maps.Count == 0)
        {
            info.ReplyToCommand(Localizer["callVotes.disabledEmptyMapgroup"]);
            return;
        }

        if (_callVoteChatVote.Running || _mapNextRound != null)
        {
            _callVoteChatVote.SubmitVote(player, info);
            return;
        }
        
        // if no argument was given, we can't possibly know what the user wants - give him all the options then
        if (info.ArgCount <= 1)
        {
            if (player == null)
            {
                info.ReplyToCommand(Localizer["callVotes.beMoreSpecific"]);
                return;
            }

            if (_callVoteMenuAllMaps == null) return;
            MenuManager.OpenChatMenu(player, _callVoteMenuAllMaps);
            return;
        }
        
        // if the exact map name was given, we can use a shortcut
        var query = info.GetArg(1);
        if (Config.Maps.Contains(query))
        {
            StartCallVote(query, player, info);
            return;
        }
        
        var menu = new ChatMenu(Localizer["callVotes.chatMenuTitle"]);
        foreach (var mapName in Config.Maps)
        {
            if (mapName.StartsWith(query) || mapName.Contains(query))
            {
                menu.AddMenuOption(mapName, (_, _) =>
                {
                    StartCallVote(mapName, player, info);
                });
            }
        }
        
        if (menu.MenuOptions.Count == 0)
        {
            info.ReplyToCommand(Localizer["callVotes.noMapFound"]);
            return;
        }

        // yay, shortcut time
        if (menu.MenuOptions.Count == 1)
        {
            StartCallVote(menu.MenuOptions[0].Text, player, info);
            return;
        }
        
        // if we deal with an RCON or internal command invocation, and we have multiple options, we cannot really show a menu unfortunately
        if (player == null)
        {
            info.ReplyToCommand(Localizer["callVotes.couldNotIdentify"]);
            foreach (var option in menu.MenuOptions)
            {
                info.ReplyToCommand("- " + option.Text);
            }
            return;
        }
        
        MenuManager.OpenChatMenu(player, menu);
    }
    
    private void OnRtvCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount > 1)
        {
            OnNominateCommand(player, info);
        }
        
        _rtvChatVote.SubmitVote(player, info);
    }
    
    private void OnCallVoteVoteSucceeded()
    {
        _mapNextRound = _callVoteMap;
    }
    
    private void OnRtvVoteSucceeded()
    {
        Server.ExecuteCommand(Config.RtvEndMatchCommand);
    }

    private void Nominate(string mapName, CCSPlayerController? player, CommandInfo? info)
    {
        if (_nominatedMapNames.Add(mapName))
        {
            if (player != null)
            {
                var reply = Localizer["nominations.nominationByPlayerSuccessful"].Value.Replace("{player}", player.PlayerName)
                    .Replace("{map}", mapName);
                Server.PrintToChatAll(reply);
                Server.PrintToConsole(reply);
                
                // check if this player nominated a map already and denominate it
                if (player.AuthorizedSteamID != null)
                {
                    if (!_playerNominations.TryAdd(player.AuthorizedSteamID, mapName))
                    {
                        reply = Localizer["nominations.removedPreviousNomination"].Value
                            .Replace("map", _playerNominations[player.AuthorizedSteamID]);
                        if (info != null) info.ReplyToCommand(reply);
                        else player.PrintToChat(reply);
                        
                        _playerNominations[player.AuthorizedSteamID] = mapName;
                        _nominatedMapNames.Remove(mapName);
                    }
                }
            }
            else
            {
                var reply = Localizer["nominations.nominationByUnknownEntitySuccessful"].Value
                    .Replace("{map}", mapName);
                Server.PrintToChatAll(reply);
                Server.PrintToConsole(reply);
            }
        }
        else
        {
            var reply = Localizer["nominations.alreadyNominated"].Value.Replace("{map}", mapName);
            if (info != null) info.ReplyToCommand(reply);
            else if (player != null) player.PrintToChat(reply);
            else Server.PrintToConsole(reply);
        }
    }

    private void StartCallVote(string mapName, CCSPlayerController? player, CommandInfo? info)
    {
        _callVoteMap = mapName;
        
        _callVoteChatVote.Localizer.AlreadyVoted = Localizer["callVotes.alreadyVoted"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VotedSuccessfully = Localizer["callVotes.votedSuccessfully"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteSucceeded = Localizer["callVotes.voteSucceeded"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteFailed = Localizer["callVotes.voteFailed"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteAlreadySucceeded = Localizer["callVotes.alreadySucceeded"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteStartedByPlayer = Localizer["callVotes.voteStartedByPlayer"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteStartedByUnknownEntity = Localizer["callVotes.voteStartedByUnknownEntity"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.Notification = Localizer["callVotes.status"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.NotificationHint = Localizer["callVotes.statusHint"].Value.Replace("{map}", mapName);
        _callVoteChatVote.SubmitVote(player, info);
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

    private int PickPseudoRandomMapIndex(ref List<int> exceptThoseMaps)
    {
        int i;
        do i = Random.Shared.Next(Config.Maps.Count);
        while (exceptThoseMaps.Contains(i));
        return i;
    }
    
    private void UpdateEndMatchGroupVoteOptions()
    {
        if (Config.Maps.Count == 0)
        {
            Console.WriteLine("[NativeMapVotePlugin][WARNING] No maps in map group, skipping vote manipulation!");
            return;
        }
        
        var proxies = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var proxy = proxies.FirstOrDefault();
        if (proxy == null || proxy.GameRules == null)
        {
            Console.WriteLine("[NativeMapVotePlugin][ERROR] Could not find end match vote entity!");
            return;
        }

        List<int> alreadyAddedOptions = new List<int>();
        foreach (ref var option in proxy.GameRules.EndMatchMapGroupVoteOptions)
        {
            var mapName = _nominatedMapNames.FirstOrDefault();
            if (mapName != null)
            {
                option = Config.Maps.IndexOf(mapName);
                _nominatedMapNames.Remove(mapName);
            }
            else
            {
                option = PickPseudoRandomMapIndex(ref alreadyAddedOptions);
            }
            
            alreadyAddedOptions.Add(option);
            
            if (alreadyAddedOptions.Count >= Config.Maps.Count) break;
        }
        Console.WriteLine("[NativeMapVotePlugin][INFO] Successfully manipulated the vote (no scandal though)!");
    }

    private void Reset()
    {
        _nominatedMapNames.Clear();
        _playerNominations.Clear();
        _callVoteChatVote.Reset();
        _rtvChatVote.Reset();
        _mapNextRound = null;
    }

    private bool IsNativeMap(string mapName)
    {
        string[] files = Directory.GetFiles(Server.GameDirectory + "/csgo/maps");
        foreach (string file in files)
        {
            if (Path.GetFileNameWithoutExtension(file) == mapName) return true;
        }
        return false;
    }
}
