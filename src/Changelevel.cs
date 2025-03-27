using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using PanoramaVoteManagerAPI.Vote;
using PanoramaVoteManagerAPI.Enums;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private bool _changelevelSuccess = false;
        private string _changelevelMap = "";
        private long _changelevelCooldown = 0;
        private Vote? _changelevelVote = null;

        private void ChangelevelReset()
        {
            _changelevelSuccess = false;
            _changelevelMap = "";
            _changelevelCooldown = 0;
            if (_voteManager != null && _changelevelVote != null)
                _voteManager.RemoveVote(_changelevelVote);
            _changelevelVote = null;
        }

        private void InitiateLevelChange(string mapName, CCSPlayerController player, CommandInfo command, bool sentFromMenu = false)
        {
            if (player == null
            || !player.IsValid
            || !player.UserId.HasValue
            || command == null
            || _voteManager == null) return;
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
            // set level name
            _changelevelMap = mapName;
            // create vote
            _changelevelVote = new(
                Config.ChangelevelSfuiString,
                new Dictionary<string, string> {
                    {"en", mapName}, // TODO: get from language file
                    {"de", mapName},
                },
                Config.ChangelevelVoteDuration,
                -1,
                [],
                (int)player.UserId,
                VoteFlags.None,
                ChangelevelCallback
            );
            // send vote
            int seconds = _voteManager.AddVote(_changelevelVote);
            if (seconds > 0)
                command.ReplyToCommand(Localizer["changelevel.vote_delay"].Value
                    .Replace("{seconds}", seconds.ToString()));
            // close menu if not sent from menu
            if (sentFromMenu)
                MenuManager.CloseActiveMenu(player);
        }

        private void ChangelevelCallback(Vote vote, bool success)
        {
            if (success)
            {
                // indicate success
                _changelevelSuccess = true;
                // send message to all players
                foreach (var entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    if (Config.ChangelevelOnRoundEnd)
                        entry.PrintToChat(Localizer["changelevel.success_endround"].Value
                            .Replace("{map}", _changelevelMap)); // TODO: get players language
                    else
                        entry.PrintToChat(Localizer["changelevel.success_now"].Value
                            .Replace("{map}", _changelevelMap)); // TODO: get players language
                // change level if not on round end
                if (!Config.ChangelevelOnRoundEnd) DoChangeLevel();
            }
            else
            {
                // send message to all players
                foreach (var entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    entry.PrintToChat(Localizer["changelevel.failed"].Value
                        .Replace("{map}", _changelevelMap)); // TODO: get players language
            }
            // reset vote
            _changelevelVote = null;
            // set cooldown
            _changelevelCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.ChangelevelCooldown;
        }

        private void DoChangeLevel()
        {
            if (!_changelevelSuccess) return;
            // check if _changelevelMap is in local maps
            if (_localMaps.Contains(_changelevelMap))
            {
                AddTimer(2f, () => { Server.ExecuteCommand($"changelevel {_changelevelMap}"); });
            }
            else if (_workshopMaps.Contains(_changelevelMap))
            {
                AddTimer(2f, () => { Server.ExecuteCommand($"ds_workshop_changelevel {_changelevelMap}"); });
            }
        }
    }
}