using CounterStrikeSharp.API;
using PanoramaVoteManagerAPI.Enums;
using CounterStrikeSharp.API.Modules.Cvars;
using PanoramaVoteManagerAPI.Vote;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private void MapFeedbackVoteReset()
        {
            _mapFeedbackState.Reset();
        }

        private void InitializeMapFeedbackVote()
        {
            if (_voteManager == null
                || !Config.FeedbackVoteEnabled
                || _mapFeedbackState.Vote != null)
            {
                return;
            }

            int feedbackVoteTime = Config.FeedbackVoteDuration;
            ConVar? mpEndmatchVoteNextLevelTime = ConVar.Find("mp_endmatch_votenextleveltime");
            ConVar? mpEndmatchVoteNextMap = ConVar.Find("mp_endmatch_votenextmap");
            if (mpEndmatchVoteNextMap != null
                && mpEndmatchVoteNextLevelTime != null
                && mpEndmatchVoteNextMap.GetPrimitiveValue<bool>())
            {
                feedbackVoteTime += (int)mpEndmatchVoteNextLevelTime.GetPrimitiveValue<float>();
            }
            if (feedbackVoteTime <= 0)
            {
                return;
            }
            _mapFeedbackState.Vote = new(
                sfui: Config.SfuiString,
                text: new Dictionary<string, string> {
                    {"en", $"{Config.SfuiPrefix}Did you like {Server.MapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)}?{Config.SfuiSuffix}"},
                    {"de", $"{Config.SfuiPrefix}Hat dir {Server.MapName.ToLower(System.Globalization.CultureInfo.CurrentCulture)} gefallen?{Config.SfuiSuffix}"},
                },
                time: feedbackVoteTime,
                team: -1,
                playerIDs: [],
                initiator: 99,
                minSuccessPercentage: 0f,
                minVotes: 0,
                flags: VoteFlags.AlwaysSuccessful | VoteFlags.DoNotEndUntilAllVoted,
                callback: MapFeedbackVoteCallback
            );
            int seconds = _voteManager.AddVote(_mapFeedbackState.Vote);
            if (seconds > Config.FeedbackVoteMaxDelay)
            {
                _ = _voteManager.RemoveVote(_mapFeedbackState.Vote);
            }
        }

        private void MapFeedbackVoteCallback(Vote vote, bool success)
        {
            if (_mapFeedbackState.Vote == null)
            {
                return;
            }
            SetMapVoteFeedback(Server.MapName, true, vote.GetYesVotes());
            SetMapVoteFeedback(Server.MapName, false, vote.GetNoVotes());
            _mapFeedbackState.Reset();
        }
    }
}
