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
        private void ChangelevelReset()
        {
            if (_voteManager != null && _changelevelState.Vote != null)
            {
                _ = _voteManager.RemoveVote(_changelevelState.Vote);
            }
            _changelevelState.Reset();
        }

        private void InitiateLevelChange(string mapName, CCSPlayerController player, CommandInfo command, bool sentFromMenu = false)
        {
            if (player == null
            || !player.IsValid
            || !player.UserId.HasValue
            || command == null
            || _voteManager == null)
            {
                return;
            }
            if (_changelevelState.Success)
            {
                command.ReplyToCommand(Localizer["changelevel.already_success"].Value
                    .Replace("{map}", _changelevelState.MapName));
                return;
            }
            if (_changelevelState.Vote != null)
            {
                command.ReplyToCommand(Localizer["changelevel.in_progress"]);
                return;
            }
            if (_changelevelState.Cooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                command.ReplyToCommand(Localizer["changelevel.cooldown"].Value
                    .Replace("{seconds}", (_changelevelState.Cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString()));
                return;
            }
            _changelevelState.MapName = mapName;
            _changelevelState.Vote = new(
                sfui: Config.ChangelevelSfuiString,
                text: new Dictionary<string, string> {
                    {"en", mapName},
                    {"de", mapName},
                },
                time: Config.ChangelevelVoteDuration,
                team: -1,
                playerIDs: [],
                initiator: (int)player.UserId,
                minSuccessPercentage: 51f,
                minVotes: 1,
                flags: VoteFlags.None,
                callback: ChangelevelCallback
            );
            int seconds = _voteManager.AddVote(_changelevelState.Vote);
            if (seconds > 0)
            {
                command.ReplyToCommand(Localizer["changelevel.vote_delay"].Value
                    .Replace("{seconds}", seconds.ToString()));
            }
            if (sentFromMenu)
            {
                MenuManager.CloseActiveMenu(player);
            }
        }

        private void ChangelevelCallback(Vote vote, bool success)
        {
            if (success)
            {
                _changelevelState.Success = true;
                foreach (CCSPlayerController? entry in Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    if (Config.ChangelevelOnRoundEnd)
                    {
                        entry.PrintToChat(Localizer["changelevel.success_endround"].Value
                            .Replace("{map}", _changelevelState.MapName));
                    }
                    else
                    {
                        entry.PrintToChat(Localizer["changelevel.success_now"].Value
                            .Replace("{map}", _changelevelState.MapName));
                    }
                }
                if (!Config.ChangelevelOnRoundEnd)
                {
                    DoChangeLevel();
                }
            }
            else
            {
                foreach (CCSPlayerController? entry in Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    entry.PrintToChat(Localizer["changelevel.failed"].Value
                        .Replace("{map}", _changelevelState.MapName));
                }
            }
            _changelevelState.Vote = null;
            _changelevelState.Cooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.ChangelevelCooldown;
        }

        private void DoChangeLevel()
        {
            if (!_changelevelState.Success)
            {
                return;
            }
            if (_localMaps.Contains(_changelevelState.MapName))
            {
                _ = AddTimer(2f, () => { Server.ExecuteCommand($"changelevel {_changelevelState.MapName}"); });
            }
            else if (_workshopMaps.Contains(_changelevelState.MapName))
            {
                _ = AddTimer(2f, () => { Server.ExecuteCommand($"ds_workshop_changelevel {_changelevelState.MapName}"); });
            }
        }
    }
}