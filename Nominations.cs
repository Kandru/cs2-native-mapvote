using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;

namespace NativeMapVotePlugin;

public partial class NativeMapVotePlugin
{
    private readonly HashSet<string> _nominatedMapNames = new();
    private readonly Dictionary<SteamID, string> _playerNominations = new();
    
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
                menu.AddMenuOption(mapName, (_, _) => { Nominate(mapName, player, info); },
                    _nominatedMapNames.Contains(mapName));
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

    private void ResetNominations()
    {
        _nominatedMapNames.Clear();
        _playerNominations.Clear();
    }
}