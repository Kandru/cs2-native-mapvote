using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private readonly Dictionary<CCSPlayerController, string> _nominations = [];

        private void NominateReset()
        {
            _nominations.Clear();
        }

        private void NominateMap(string mapName, CCSPlayerController player, CommandInfo command, bool sentFromMenu = false)
        {
            if (player == null
            || !player.IsValid
            || command == null)
            {
                return;
            }
            // check if max nominations is reached
            if (_nominations.Count >= Config.MaxNominations && !_nominations.ContainsKey(player))
            {
                command.ReplyToCommand(Localizer["nomination.max_reached"].Value
                    .Replace("{amount}", Config.MaxNominations.ToString()));
                return;
            }
            // check if map does not already exists in nominations
            if (_nominations.ContainsValue(mapName))
            {
                string playerName = _nominations.FirstOrDefault(x => x.Value == mapName).Key?.PlayerName ?? "Unknown";
                command.ReplyToCommand(Localizer["nomination.already_exists"].Value
                    .Replace("{player}", playerName));
                return;
            }
            // add map to nominations
            if (!_nominations.TryAdd(player, mapName))
            {
                _nominations[player] = mapName;
                // announce nomination
                command.ReplyToCommand(Localizer["nomination.updated"].Value
                    .Replace("{map}", mapName));
            }
            else
            {
                // announce nomination
                command.ReplyToCommand(Localizer["nomination.success"].Value
                    .Replace("{map}", mapName));
            }
            // close menu if not sent from menu
            if (sentFromMenu)
            {
                MenuManager.CloseActiveMenu(player);
            }
        }
    }
}
