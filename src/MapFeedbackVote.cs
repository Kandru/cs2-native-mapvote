using CounterStrikeSharp.API;
using PanoramaVoteManagerAPI.Enums;
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
            // create vote
            _mapFeedbackVote = new(
                Config.SfuiString,
                new Dictionary<string, string> {
                    {"en", $"{Config.SfuiPrefix}Did you like {Server.MapName.ToLower()}?{Config.SfuiSuffix}"}, // TODO: get from language file
                    {"de", $"{Config.SfuiPrefix}Hat dir {Server.MapName.ToLower()} gefallen?{Config.SfuiSuffix}"},
                },
                Config.FeedbackVoteDuration,
                -1,
                [],
                99,
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
            // count feedback from vote._voters
            int count_positive = 0;
            int count_negative = 0;
            foreach (var kvp in vote._voters)
            {
                if (kvp.Value == (int)VoteOptions.YES)
                    count_positive++;
                else if (kvp.Value == (int)VoteOptions.NO)
                    count_negative++;
            }
            // update map feedback
            SetMapVoteFeedback(Server.MapName, true, count_positive);
            SetMapVoteFeedback(Server.MapName, false, count_negative);
            // reset vote
            _mapFeedbackVote = null;
        }
    }
}
