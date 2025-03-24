using CounterStrikeSharp.API;
using PanoramaVoteManagerAPI.Vote;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private bool _rtvSuccess = false;
        private long _rtvCooldown = 0;
        private Vote? _rtvVote = null;

        private void RtvReset()
        {
            _rtvSuccess = false;
            _rtvCooldown = 0;
            if (_voteManager != null && _rtvVote != null)
                _voteManager.RemoveVote(_rtvVote);
            _rtvVote = null;
        }

        private void RtvCallback(Vote vote, bool success)
        {
            if (success)
            {
                // send message to all players
                foreach (var entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    entry.PrintToChat(Localizer["rtv.success"]); // TODO: get players language
                // indicate success
                _rtvSuccess = true;
                // execute server commands
                Server.ExecuteCommand(Config.RtvSuccessCommand);
            }
            else
            {
                // send message to all players
                foreach (var entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
                    entry.PrintToChat(Localizer["rtv.failed"]); // TODO: get players language
            }
            // reset vote
            _rtvVote = null;
            // set cooldown
            _rtvCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.RtvCooldown;
        }
    }
}
