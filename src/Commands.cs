using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        [ConsoleCommand("rtv", "Rock The Vote")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!rtv")]
        public void CommandRtv(CCSPlayerController player, CommandInfo command)
        {
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
                "#SFUI_vote_custom_default",
                Localizer["rtv.text"].Value, // TODO: support player languages
                Config.RtvVoteDuration,
                -1,
                [],
                (int)player.UserId,
                RtvCallback
            );
            // send vote
            int seconds = _voteManager.AddVote(_rtvVote);
            if (seconds > 0)
                command.ReplyToCommand(Localizer["rtv.vote_delay"].Value
                    .Replace("{seconds}", seconds.ToString()));
        }
    }
}
