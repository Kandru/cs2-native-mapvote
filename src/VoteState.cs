using PanoramaVoteManagerAPI.Vote;

namespace NativeMapVote
{
    public class RtvState
    {
        public bool Success { get; set; }
        public long Cooldown { get; set; }
        public Vote? Vote { get; set; }

        public void Reset()
        {
            Success = false;
            Cooldown = 0;
            Vote = null;
        }
    }

    public class ChangelevelState
    {
        public bool Success { get; set; }
        public string MapName { get; set; } = "";
        public long Cooldown { get; set; }
        public Vote? Vote { get; set; }

        public void Reset()
        {
            Success = false;
            MapName = "";
            Cooldown = 0;
            Vote = null;
        }
    }

    public class MapFeedbackState
    {
        public Vote? Vote { get; set; }

        public void Reset()
        {
            Vote = null;
        }
    }
}
