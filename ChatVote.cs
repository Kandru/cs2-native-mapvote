using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace NativeMapVotePlugin;

public class ChatVoteLocalization
{
    public string AlreadyVoted = "ALREADY_VOTED";
    public string VotedSuccessfully = "VOTED_SUCCESSFULLY";
    public string VoteSucceeded = "VOTE_SUCCEEDED";
    public string VoteFailed = "VOTE_FAILED";
    public string VoteAlreadySucceeded = "VOTE_ALREADY_SUCCEEDED";
    public string AnotherVoteRunning = "ANOTHER_VOTE_RUNNING";
    public string ActiveCooldown = "ACTIVE_COOLDOWN";
    public string VoteStartedByPlayer = "VOTE_STARTED_BY_PLAYER";
    public string VoteStartedByUnknownEntity = "VOTE_STARTED_BY_UNKNOWN_ENTITY";
    public string Notification = "NOTIFICATION";
    public string? NotificationHint = "NOTIFICATION_HINT"; // optional second chat message after notification
}

public class ChatVote(BasePlugin plugin)
{
    public delegate void VoteSucceededEventHandler();
    public delegate void VoteFailedEventHandler();
    
    private static bool _voteRunning;
    
    public int Duration; // seconds
    public int Cooldown; // seconds
    public double Percentage;
    public int NotificationInterval; // seconds
    public bool AllowSpectators;
    public readonly ChatVoteLocalization Localizer = new();
    public event VoteSucceededEventHandler OnVoteSucceeded = delegate {};
    public event VoteFailedEventHandler OnVoteFailed = delegate {};
    
    private readonly HashSet<SteamID?> _voters = new();
    private Timer? _notificationTimer;
    private DateTime? _lastVote;

    public void SubmitVote(CCSPlayerController? player, CommandInfo info)
    {
        // if vote not yet started, we need to start it first
        // then, if starting fails, we can't proceed
        if (!_voters.Any() && !StartVote(player, info)) return;
        
        // try to add vote
        bool alreadyVoted = !_voters.Add(player != null ? player.AuthorizedSteamID : null);
        
        // if cannot add vote, vote already submitted
        if (alreadyVoted)
        {
            info.ReplyToCommand(Localizer.AlreadyVoted);
            return;
        }
        
        // check if vote succeeded
        var votesNeeded = Math.Ceiling(Percentage * CountPlayers());
        if (_voters.Count >= votesNeeded)
        {
            // special signal that means the vote already passed
            _lastVote = DateTime.MaxValue;
            
            StopVote();
            info.ReplyToCommand(Localizer.VoteSucceeded);
            OnVoteSucceeded.Invoke();
            return;
        }
        
        info.ReplyToCommand(Localizer.VotedSuccessfully);
    }

    public void Reset()
    {
        StopVote();
        
        // reset cooldown or succeeded vote
        _lastVote = null;
    }
    
    private bool StartVote(CCSPlayerController? player, CommandInfo info)
    {
        // if vote succeeded already
        if (_lastVote == DateTime.MaxValue)
        {
            info.ReplyToCommand(Localizer.VoteAlreadySucceeded);
            return false;
        }
        
        // if another vote is running
        if (_voteRunning)
        {
            info.ReplyToCommand(Localizer.AnotherVoteRunning);
            return false;
        }
        
        // if cooldown is still active
        if (_lastVote != null)
        {
            var secondsSinceLastVote = (DateTime.Now - _lastVote).Value.TotalSeconds;
            if (secondsSinceLastVote < Cooldown)
            {
                info.ReplyToCommand(Localizer.ActiveCooldown);
                return false;
            }
        }
        
        // update state
        _lastVote = DateTime.Now;
        _voteRunning = true;
        
        // start notification loop that will also check if the vote ends
        _notificationTimer = plugin.AddTimer(NotificationInterval, SendNotification,
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        
        if (player != null)
        {
            var reply = Localizer.VoteStartedByPlayer.Replace("{player}", player.PlayerName);
            Server.PrintToChatAll(reply);
            Server.PrintToConsole(reply);
        }
        else
        {
            Server.PrintToChatAll(Localizer.VoteStartedByUnknownEntity);
            Server.PrintToConsole(Localizer.VoteStartedByUnknownEntity);
        }
        
        return true;
    }

    private void StopVote()
    {
        _notificationTimer?.Kill();
        _voters.Clear();
    }

    private int CountPlayers()
    {
        int count = 0;
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsBot && !player.IsHLTV && (AllowSpectators || player.Team != CsTeam.Spectator))
                ++count;
        }

        return count;
    }

    private void SendNotification()
    {
        if (_lastVote == null) return;
        
        var votesNeeded = Math.Ceiling(Percentage * CountPlayers());
        
        // duration check
        var secondsSinceLastVote = (DateTime.Now - _lastVote).Value.TotalSeconds;
        if (secondsSinceLastVote >= Duration)
        {
            StopVote();
            
            var reply = Localizer.VoteFailed
                .Replace("{playersVoted}", _voters.Count.ToString())
                .Replace("{playersNeeded}", votesNeeded.ToString(CultureInfo.CurrentCulture));
            Server.PrintToChatAll(reply);
            Server.PrintToConsole(reply);

            OnVoteFailed.Invoke();
            return;
        }
        
        var reply2 = Localizer.Notification.Replace("{playersVoted}",
            _voters.Count.ToString()).Replace("{playersNeeded}", votesNeeded.ToString(CultureInfo.CurrentCulture));
        Server.PrintToChatAll(reply2);
        Server.PrintToConsole(reply2);

        if (Localizer.NotificationHint != null)
        {
            Server.PrintToChatAll(Localizer.NotificationHint);
            Server.PrintToConsole(Localizer.NotificationHint);            
        }
    }
}