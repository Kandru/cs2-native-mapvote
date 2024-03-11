using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RconSharp;

namespace NativeMapVotePlugin;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("fetch_mapgroup_over_rcon")]
    public bool FetchMapGroupOverRcon { get; set; } = false;
    [JsonPropertyName("maps")] public ImmutableList<string> Maps { get; set; } = ImmutableList<string>.Empty;
    [JsonPropertyName("callvote_cooldown")]
    public int CallVoteCooldown { get; set; } = 120;
    [JsonPropertyName("rtv_cooldown")]
    public int RtvCooldown { get; set; } = 240;
    [JsonPropertyName("rtv_percentage")] public double RtvPercentage { get; set; } = 0.6;
    [JsonPropertyName("rtv_message_interval")]
    public int RtvMessageInterval { get; set; } = 15;
    [JsonPropertyName("rtv_duration")] public int RtvDuration { get; set; } = 60;
}

public class NativeMapVotePlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "Native Map Vote Plugin";
    public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it>";
    public override string ModuleVersion => "1.0.0";
    public ChatMenu? NominationMenuAllMaps;
    public ChatMenu? CallVoteMenuAllMaps;
    
    public PluginConfig Config { get; set; } = null!;

    private readonly HashSet<string> _nominatedMapNames = new();
    private readonly Dictionary<SteamID, string> _playerNominations = new();
    private DateTime? _lastCallVote;
    private readonly HashSet<SteamID?> _playersVotedForRtv = new();
    private DateTime? _lastRtv;
    
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        if (Config.FetchMapGroupOverRcon) FetchMapGroupOverRcon();
        else OnMapGroupChange();
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventCsIntermission>(OnIntermission, HookMode.Pre);

        AddCommand("css_nom", "Nominates a map so that it appears in the map vote after the match ends", OnNominateCommand);
        AddCommand("css_nominate", "Nominates a map so that it appears in the map vote after the match ends", OnNominateCommand);
        AddCommand("css_noms", "Shows the current nominated maps that will appear in the map vote after the match ends", OnNominationsCommand);
        AddCommand("css_nominations", "Shows the current nominated maps that will appear in the map vote after the match ends", OnNominationsCommand);
        AddCommand("css_vote", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_callvote", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_cv", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_changelevel", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_cl", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        //AddCommand("css_map", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        //AddCommand("css_level", "Starts a callvote to change the map to a specific map", OnCallVoteCommand);
        AddCommand("css_rtv", "Starts a vote to end the match immediately, consequently starting a map vote", OnRtvCommand);
        AddCommand("css_skip", "Starts a vote to end the match immediately, consequently starting a map vote", OnRtvCommand);
    
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

    private void OnNominateCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (Config.Maps.Count == 0)
        {
            info.ReplyToCommand("Nomination disabled because there are no maps in the active map group!");
            return;
        }
        
        // if no argument was given, we can't possibly know what the user wants - give him all the options then
        if (info.ArgCount <= 1)
        {
            if (player == null)
            {
                info.ReplyToCommand("You need to specify a map name or a unique map prefix!");
                return;
            }

            if (NominationMenuAllMaps == null) return;
            MenuManager.OpenChatMenu(player, NominationMenuAllMaps);
            return;
        }
        
        // if the exact map name was given, we can use a shortcut
        var query = info.GetArg(1);
        if (Config.Maps.Contains(query))
        {
            Nominate(query, player, info);
            return;
        }
        
        var menu = new ChatMenu("Nominate a map:");
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
            info.ReplyToCommand("No map was found that starts with or contains your query. Please try again!");
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
            info.ReplyToCommand("No map with the exact name " + query + " was found. Did you mean one of the following maps?");
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
            info.ReplyToCommand("No nominations so far. Type in !nom <map-name> to nominate a map for voting after match end!");
            return;
        }
        
        var reply = "[Nominations] Nominated maps so far: " + _nominatedMapNames.First();
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
    }

    private void OnCallVoteCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (Config.Maps.Count == 0)
        {
            info.ReplyToCommand("Callvotes disabled because there are no maps in the active map group!");
            return;
        }
        
        // cooldown check
        if (_lastCallVote != null)
        {
            var secondsSinceLastCallvote = (DateTime.Now - _lastCallVote).Value.TotalSeconds;
            if (secondsSinceLastCallvote < Config.CallVoteCooldown)
            {
                info.ReplyToCommand($"The next callvote can be started in {double.Ceiling(Config.CallVoteCooldown - secondsSinceLastCallvote)} seconds!");
                return;
            }
        }
        
        // if no argument was given, we can't possibly know what the user wants - give him all the options then
        if (info.ArgCount <= 1)
        {
            if (player == null)
            {
                info.ReplyToCommand("You need to specify a map name or a unique map prefix!");
                return;
            }

            if (CallVoteMenuAllMaps == null) return;
            MenuManager.OpenChatMenu(player, CallVoteMenuAllMaps);
            return;
        }
        
        // if the exact map name was given, we can use a shortcut
        var query = info.GetArg(1);
        if (Config.Maps.Contains(query))
        {
            CallVote(query, player);
            return;
        }
        
        var menu = new ChatMenu("Choose map for callvote:");
        foreach (var mapName in Config.Maps)
        {
            if (mapName.StartsWith(query) || mapName.Contains(query))
            {
                menu.AddMenuOption(mapName, (_, _) =>
                {
                    CallVote(mapName, player);
                });
            }
        }
        
        if (menu.MenuOptions.Count == 0)
        {
            info.ReplyToCommand("No map was found that starts with or contains your query. Please try again!");
            return;
        }

        // yay, shortcut time
        if (menu.MenuOptions.Count == 1)
        {
            CallVote(menu.MenuOptions[0].Text, player);
            return;
        }
        
        // if we deal with an RCON or internal command invocation, and we have multiple options, we cannot really show a menu unfortunately
        if (player == null)
        {
            info.ReplyToCommand("No map with the exact name " + query + " was found. Did you mean one of the following maps?");
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
        // RTV is not running yet, start it
        if (!_playersVotedForRtv.Any())
        {
            // cooldown check
            if (_lastRtv != null)
            {
                var secondsSinceLastRtv = (DateTime.Now - _lastRtv).Value.TotalSeconds;
                if (secondsSinceLastRtv < Config.RtvCooldown)
                {
                    info.ReplyToCommand($"The next RTV can be started in {double.Ceiling(Config.RtvCooldown - secondsSinceLastRtv)} seconds!");
                    return;
                }
            }
            
            _lastRtv = DateTime.Now;
            AddTimer(Config.RtvMessageInterval, RunRtvLoop, TimerFlags.STOP_ON_MAPCHANGE);
            
            if (player != null)
            {
                Server.PrintToChatAll($"[RTV] Player {player.PlayerName} started an RTV vote. Type in !rtv if you want to change the map!");
            }
            else
            {
                Server.PrintToChatAll("[RTV] An RTV vote was started. Type in !rtv if you want to change the map!");
            }
        }

        if (_playersVotedForRtv.Add(player != null ? player.AuthorizedSteamID : null))
        {
            info.ReplyToCommand("Successfully voted for map change!");
        }
        else
        {
            info.ReplyToCommand("Already voted for map change!");
        }
    }

    private void Nominate(string mapName, CCSPlayerController? player, CommandInfo? info)
    {
        if (_nominatedMapNames.Add(mapName))
        {
            if (player != null)
            {
                Server.PrintToChatAll("[Nominations] Player " + player.PlayerName + " nominated map " + mapName + " for the map vote at match end!");
                
                // check if this player nominated a map already and denominate it
                if (player.AuthorizedSteamID != null)
                {
                    if (!_playerNominations.TryAdd(player.AuthorizedSteamID, mapName))
                    {
                        var reply = "Removed your previous nomination of map " +
                                _playerNominations[player.AuthorizedSteamID] + "!";
                        if (info != null) info.ReplyToCommand(reply);
                        else player.PrintToChat(reply);
                        
                        _playerNominations[player.AuthorizedSteamID] = mapName;
                        _nominatedMapNames.Remove(mapName);
                    }
                }
            }
            else
            {
                Server.PrintToChatAll("[Nominations] Map " + mapName + " was nominated for the map vote at match end!");
            }
        }
        else
        {
            var reply = "The map " + mapName + " is already nominated!";
            if (info != null) info.ReplyToCommand(reply);
            else if (player != null) player.PrintToChat(reply);
            else Server.PrintToConsole(reply);
        }
    }

    private int GetPlayingPlayerCount()
    {
        int count = 0;
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsBot && player.Team != CsTeam.Spectator && !player.IsHLTV)
                ++count;
        }

        return count;
    }
    private void RunRtvLoop()
    {
        if (!_playersVotedForRtv.Any() || _lastRtv == null) return;

        var playersNeeded = Math.Ceiling(Config.RtvPercentage * GetPlayingPlayerCount());
        
        // RTV passed successfully
        if (_playersVotedForRtv.Count >= playersNeeded)
        {
            _playersVotedForRtv.Clear();
            /*var ent = Utilities.CreateEntityByName<CGameEnd>("game_end");
            if (ent != null)
            {
                ent.DispatchSpawn();
            }
            else
            {
                Console.WriteLine("[NativeMapVotePlugin][ERROR] Creating game end entity failed!");
            }*/
            Server.ExecuteCommand("mp_maxrounds 0");
            
            Server.PrintToChatAll("[RTV] Vote succeeded. Changing map after this round!");
            return;
        }
        
        // duration check
        var secondsSinceLastRtv = (DateTime.Now - _lastRtv).Value.TotalSeconds;
        if (secondsSinceLastRtv >= Config.RtvDuration)
        {
            Server.PrintToChatAll($"[RTV] Vote failed. Not enough votes ({_playersVotedForRtv.Count}/{playersNeeded} players), not changing map!");
            
            _playersVotedForRtv.Clear();
            return;
        }

        AddTimer(Config.RtvMessageInterval, RunRtvLoop, TimerFlags.STOP_ON_MAPCHANGE);
        
        Server.PrintToChatAll($"[RTV] Vote in progress - {_playersVotedForRtv.Count} of {playersNeeded} players voted for changing the map!");
        Server.PrintToChatAll($"[RTV] Type in !rtv to vote. Remember to !nominate maps for the map vote!");
    }

    private void CallVote(string mapName, CCSPlayerController? player)
    {
        Server.ExecuteCommand($"callvote changelevel {mapName}");
        _lastCallVote = DateTime.Now;
        
        if (player != null)
        {
            Server.PrintToChatAll($"[Callvote] Player {player.PlayerName} started a callvote for map {mapName}!");
        }
        else
        {
            Server.PrintToChatAll($"[Callvote] A callvote for map {mapName} was started!");
        }
    }
    
    private void OnMapGroupChange()
    {
        var nominateMenu = new ChatMenu("Nominate a map for map vote:");
        var callVoteMenu = new ChatMenu("Choose map for callvote:");
        foreach (var mapName in Config.Maps)
        {
            nominateMenu.AddMenuOption(mapName, (player, _) =>
            {
                Nominate(mapName, player, null);
            });
            callVoteMenu.AddMenuOption(mapName, (player, _) =>
            {
                CallVote(mapName, player);
            });
        }
        NominationMenuAllMaps = nominateMenu;
        CallVoteMenuAllMaps = callVoteMenu;
    }
    
    private void FetchMapGroupOverRcon()
    {
        var port = 27015;
        var portCvar = ConVar.Find("port");
        if (portCvar != null) port = portCvar.GetPrimitiveValue<int>();

        var rconPasswordCvar = ConVar.Find("rcon_password");
        if (rconPasswordCvar == null || rconPasswordCvar.StringValue == null || rconPasswordCvar.StringValue.Length == 0)
        {
            Console.WriteLine("[NativeMapVotePlugin][WARNING] Fetching map list over RCON disabled due to disabled RCON (cvar rcon_password not set)!");
            return;
        }
        
        Task.Run(async () => {
            var client = RconClient.Create("127.0.0.1", port);
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
        do i = Random.Shared.Next(Config.Maps.Count - 1);
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
        }
        Console.WriteLine("[NativeMapVotePlugin][INFO] Successfully manipulated the vote (no scandal though)!");
    }

    private void Reset()
    {
        _nominatedMapNames.Clear();
        _playerNominations.Clear();
        _lastCallVote = null;
        _playersVotedForRtv.Clear();
        _lastRtv = null;
    }
}
