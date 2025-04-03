using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Menu;
using PanoramaVoteManagerAPI.Enums;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        [ConsoleCommand("skip", "Rock The Vote")]
        [ConsoleCommand("rtv", "Rock The Vote")]
        [ConsoleCommand("rockthevote", "Rock The Vote")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "")]
        public void CommandRtv(CCSPlayerController player, CommandInfo command)
        {
            if (!Config.RtvEnabled) return;
            if (_voteManager == null || !player.UserId.HasValue) return;
            // check if rtv was already successful
            if (_rtvSuccess)
            {
                command.ReplyToCommand(Localizer["rtv.already_success"]);
                return;
            }
            // check if rtv is in progress
            if (_rtvVote != null)
            {
                command.ReplyToCommand(Localizer["rtv.in_progress"]);
                return;
            }
            // check if rtv cooldown is active
            if (_rtvCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                command.ReplyToCommand(Localizer["rtv.cooldown"].Value
                    .Replace("{seconds}", (_rtvCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString()));
                return;
            }
            // create vote
            _rtvVote = new(
                Config.SfuiString,
                new Dictionary<string, string> {
                    {"en", $"{Config.SfuiPrefix}RTV: want to change the map after this round?{Config.SfuiSuffix}"}, // TODO: get from language file
                    {"de", $"{Config.SfuiPrefix}RTV: Möchtest du die Karte nach dieser Runde ändern?{Config.SfuiSuffix}"},
                },
                Config.RtvVoteDuration,
                -1,
                [],
                (int)player.UserId,
                VoteFlags.None,
                RtvCallback
            );
            // send vote
            int seconds = _voteManager.AddVote(_rtvVote);
            if (seconds > 0)
                command.ReplyToCommand(Localizer["rtv.vote_delay"].Value
                    .Replace("{seconds}", seconds.ToString()));
        }

        [ConsoleCommand("nom", "Nomination of a map")]
        [ConsoleCommand("nominate", "Nomination of a map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 1, usage: "<mapname>")]
        public void CommandNom(CCSPlayerController player, CommandInfo command)
        {
            if (!Config.NominationsEnabled) return;
            string mapName = command.GetArg(1);
            // check if max nominations is reached
            if (_nominations.Count >= Config.MaxNominations && !_nominations.ContainsKey(player))
            {
                command.ReplyToCommand(Localizer["nomination.max_reached"].Value
                    .Replace("{amount}", Config.MaxNominations.ToString()));
                return;
            }
            // check if maps do exist
            if (_workshopMaps.Count == 0)
            {
                command.ReplyToCommand(Localizer["nomination.no_maps"]);
                return;
            }
            // check if one or multiple map(s) are found by the given name
            List<string> maps = _workshopMaps.FindAll(map => map.Contains(mapName, StringComparison.OrdinalIgnoreCase));
            // if no map is found
            if (maps.Count == 0)
            {
                command.ReplyToCommand(Localizer["nomination.map_not_found"]);
                return;
            }
            else if (maps.Count > 1)
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["nomination.menu.title"]);
                // add menu options
                foreach (var map in maps)
                    menu.AddMenuOption(map, (_, _) => { NominateMap(map, player, command, true); });
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                NominateMap(maps.First(), player, command);
            }
        }

        [ConsoleCommand("noms", "list of all current nominations")]
        [ConsoleCommand("nominations", "list of all current nominations")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "")]
        public void CommandNoms(CCSPlayerController player, CommandInfo command)
        {
            if (!Config.NominationsEnabled) return;
            if (_nominations.Count == 0)
                command.ReplyToCommand(Localizer["nominations.empty"]);
            else
                command.ReplyToCommand(Localizer["nominations.list"].Value
                    .Replace("{nominations}", string.Join(", ", _nominations.Select(x => $"{x.Key.PlayerName} -> {x.Value}"))));
        }

        [ConsoleCommand("cl", "Vote to change the level")]
        [ConsoleCommand("cv", "Vote to change the level")]
        [ConsoleCommand("map", "Vote to change the level")]
        [ConsoleCommand("level", "Vote to change the level")]
        [ConsoleCommand("changelevel", "Vote to change the level")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 1, usage: "<mapname>")]
        public void CommandChangeLevel(CCSPlayerController player, CommandInfo command)
        {
            if (!Config.ChangelevelEnabled
                || _voteManager == null) return;
            string mapName = command.GetArg(1);
            // check if changelevel was already successful
            if (_changelevelSuccess)
            {
                command.ReplyToCommand(Localizer["changelevel.already_success"].Value
                    .Replace("{map}", _changelevelMap)); // TODO: get players language
                return;
            }
            // check if changelevel is in progress
            if (_changelevelVote != null)
            {
                command.ReplyToCommand(Localizer["changelevel.in_progress"]);
                return;
            }
            // check if changelevel cooldown is active
            if (_changelevelCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                command.ReplyToCommand(Localizer["changelevel.cooldown"].Value
                    .Replace("{seconds}", (_changelevelCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString()));
                return;
            }
            // check if maps do exist
            if (_localMaps.Count == 0 && _workshopMaps.Count == 0)
            {
                command.ReplyToCommand(Localizer["changelevel.no_maps"]);
                return;
            }
            // check if one or multiple map(s) are found by the given name
            List<string> maps = _localMaps.FindAll(map => map.Contains(mapName, StringComparison.OrdinalIgnoreCase));
            maps.AddRange(_workshopMaps.FindAll(map => map.Contains(mapName, StringComparison.OrdinalIgnoreCase)));
            maps = [.. maps.Distinct()];
            // if no map is found
            if (maps.Count == 0)
            {
                command.ReplyToCommand(Localizer["changelevel.map_not_found"]);
                return;
            }
            else if (maps.Count > 1)
            {
                // create menu to choose map
                var menu = new ChatMenu(Localizer["changelevel.menu.title"]);
                // add menu options
                foreach (var map in maps)
                    menu.AddMenuOption(map, (_, _) => { InitiateLevelChange(map, player, command, true); });
                MenuManager.OpenChatMenu(player, menu);
            }
            else
            {
                InitiateLevelChange(maps.First(), player, command);
            }
        }

        [ConsoleCommand("nativemapvote", "NativeMapVote admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandMapVote(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower())
            {
                case "reload":
                    Config.Reload();
                    command.ReplyToCommand(Localizer["admin.reload"]);
                    break;
                case "best_maps":
                    command.ReplyToCommand(Localizer["admin.best_maps"]);
                    // get best voted maps from Config.Maps
                    var bestMaps = Config.Maps
                        .Where(x => x.Value.VotesPositive > 0)
                        .OrderByDescending(x => (double)x.Value.VotesPositive / (x.Value.VotesPositive + x.Value.VotesNegative))
                        .Take(25)
                        .Select(x => x.Key)
                        .ToList();
                    // iterate through all maps and reply
                    foreach (var map in bestMaps)
                        command.ReplyToCommand($"{map}: {Config.Maps[map].VotesPositive} / {Config.Maps[map].VotesNegative}");
                    break;
                case "worst_maps":
                    command.ReplyToCommand(Localizer["admin.worst_maps"]);
                    // get worst maps from Config.Maps
                    var worstMaps = Config.Maps
                        .Where(x => x.Value.VotesNegative > 0)
                        .OrderByDescending(x => (double)x.Value.VotesNegative / (x.Value.VotesPositive + x.Value.VotesNegative))
                        .Take(25)
                        .Select(x => x.Key)
                        .ToList();
                    foreach (var map in worstMaps)
                        command.ReplyToCommand($"{map}: {Config.Maps[map].VotesPositive} / {Config.Maps[map].VotesNegative}");
                    break;
                case "cleanup":
                    // remove all maps from Config.Maps that are not in _localMaps or _workshopMaps
                    if (_workshopMaps.Count > 0 || _localMaps.Count > 0)
                    {
                        var mapsToRemove = Config.Maps
                            .Where(x => !_localMaps.Contains(x.Key) && !_workshopMaps.Contains(x.Key))
                            .Select(x => x.Key)
                            .ToList();
                        command.ReplyToCommand(Localizer["admin.cleanup"].Value
                            .Replace("{amount}", mapsToRemove.Count.ToString()));
                        foreach (var map in mapsToRemove)
                            Config.Maps.Remove(map);
                        Config.Update();
                    }
                    else
                    {
                        command.ReplyToCommand(Localizer["admin.cleanup"].Value
                            .Replace("{amount}", "0"));
                    }
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }
    }
}
