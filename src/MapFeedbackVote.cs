using CounterStrikeSharp.API;
using PanoramaVoteManagerAPI.Enums;
using CounterStrikeSharp.API.Modules.Cvars;
using PanoramaVoteManagerAPI.Vote;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private Vote? _mapFeedbackVote = null;

        private void MapFeedbackVoteReset()
        {
            _mapFeedbackVote = null;
        }

        private void InitializeMapFeedbackVote()
        {
            if (_voteManager == null
                || !Config.FeedbackVoteEnabled
                || _mapFeedbackVote != null) return;
            int feedbackVoteTime = Config.FeedbackVoteDuration;
            // get map choose time if available and add to feedback vote time
            ConVar? mpEndmatchVoteNextLevelTime = ConVar.Find("mp_endmatch_votenextleveltime");
            ConVar? mpEndmatchVoteNextMap = ConVar.Find("mp_endmatch_votenextmap");
            if (mpEndmatchVoteNextMap != null
                && mpEndmatchVoteNextLevelTime != null
                && mpEndmatchVoteNextMap.GetPrimitiveValue<bool>() == true)
            {
                feedbackVoteTime += (int)mpEndmatchVoteNextLevelTime.GetPrimitiveValue<float>();
            }
            if (feedbackVoteTime <= 0) return;
            // create vote
            _mapFeedbackVote = new(
                Config.SfuiString,
                new Dictionary<string, string> {
                    {"en", $"{Config.SfuiPrefix}Did you like {Server.MapName.ToLower()}?{Config.SfuiSuffix}"}, // TODO: get from language file
                    {"de", $"{Config.SfuiPrefix}Hat dir {Server.MapName.ToLower()} gefallen?{Config.SfuiSuffix}"},
                },
                feedbackVoteTime,
                -1,
                [],
                99,
                VoteFlags.AlwaysSuccessful,
                MapFeedbackVoteCallback
            );
            // send vote
            int seconds = _voteManager.AddVote(_mapFeedbackVote);
            if (seconds > Config.FeedbackVoteMaxDelay)
                _voteManager.RemoveVote(_mapFeedbackVote);
        }

        private void MapFeedbackVoteCallback(Vote vote, bool success)
        {
            if (_mapFeedbackVote == null) return;
            // update map feedback
            SetMapVoteFeedback(Server.MapName, true, vote.GetYesVotes());
            SetMapVoteFeedback(Server.MapName, false, vote.GetNoVotes());
            // reset vote
            _mapFeedbackVote = null;
        }
    }
}
