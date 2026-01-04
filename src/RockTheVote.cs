using CounterStrikeSharp.API;
using PanoramaVoteManagerAPI.Vote;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private void RtvReset()
        {
            if (_voteManager != null && _rtvState.Vote != null)
            {
                _ = _voteManager.RemoveVote(_rtvState.Vote);
            }
            _rtvState.Reset();
        }

        private void RtvCallback(Vote vote, bool success)
        {
            if (success)
            {
                foreach (CounterStrikeSharp.API.Core.CCSPlayerController? entry in Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    entry.PrintToChat(Localizer["rtv.success"]);
                }
                _rtvState.Success = true;
                Server.ExecuteCommand(Config.RtvSuccessCommand);
            }
            else
            {
                foreach (CounterStrikeSharp.API.Core.CCSPlayerController? entry in Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsBot && !p.IsHLTV))
                {
                    entry.PrintToChat(Localizer["rtv.failed"]);
                }
            }
            _rtvState.Vote = null;
            _rtvState.Cooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.RtvCooldown;
        }
    }
}
