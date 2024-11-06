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
    public string SpectatorsNotAllowed = "SPECTATORS_NOT_ALLOWED";
}

public class ChatVote(BasePlugin plugin)
{
    public delegate void VoteSucceededEventHandler();
    public delegate void VoteFailedEventHandler();
    
    private static bool _anotherVoteRunning;
    
    public int Duration; // seconds
    public int Cooldown; // seconds
    public double Percentage;
    public int NotificationInterval; // seconds
    public bool AllowSpectators;
    public readonly ChatVoteLocalization Localizer = new();
    public event VoteSucceededEventHandler OnVoteSucceeded = delegate {};
    public event VoteFailedEventHandler OnVoteFailed = delegate {};
    public bool Running { get; private set; }
    
    private readonly HashSet<SteamID?> _voters = new();
    private Timer? _notificationTimer;
    private DateTime? _lastVote;

    public void SubmitVote(CCSPlayerController? player, CommandInfo? info)
    {
        if (player != null && player.Team == CsTeam.Spectator)
        {
            Reply(player, info, Localizer.SpectatorsNotAllowed);
            return;
        }
        
        // if vote not yet started, we need to start it first
        // then, if starting fails, we can't proceed
        if (!Running && !StartVote(player, info)) return;
        
        // try to add vote
        bool alreadyVoted = !_voters.Add(player != null ? player.AuthorizedSteamID : null);
        
        // if we cannot add vote, vote already submitted
        if (alreadyVoted)
        {
            Reply(player, info, Localizer.AlreadyVoted);
            return;
        }
        
        // check if vote succeeded
        var votesNeeded = Math.Ceiling(Percentage * CountPlayers());
        if (_voters.Count >= votesNeeded)
        {
            // special signal that means the vote already passed
            _lastVote = DateTime.MaxValue;
            
            StopVote();
            Reply(player, info, Localizer.VoteSucceeded);
            OnVoteSucceeded.Invoke();
            return;
        }
        
        Reply(player, info, Localizer.VotedSuccessfully);
    }

    public void Reset()
    {
        StopVote();
        
        // reset cooldown or succeeded vote
        _lastVote = null;
    }
    
    private bool StartVote(CCSPlayerController? player, CommandInfo? info)
    {
        // if vote succeeded already
        if (_lastVote == DateTime.MaxValue)
        {
            Reply(player, info, Localizer.VoteAlreadySucceeded);
            return false;
        }
        
        // if another vote is running
        if (_anotherVoteRunning)
        {
            Reply(player, info, Localizer.AnotherVoteRunning);
            return false;
        }
        
        // if cooldown is still active
        if (_lastVote != null)
        {
            var secondsSinceLastVote = (DateTime.Now - _lastVote).Value.TotalSeconds;
            if (secondsSinceLastVote < Cooldown)
            {
                Reply(player, info, Localizer.ActiveCooldown);
                return false;
            }
        }
        
        // update state
        _lastVote = DateTime.Now;
        _anotherVoteRunning = true;
        Running = true;
        
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
        _anotherVoteRunning = false;
        Running = false;
    }

    private int CountPlayers()
    {
        int count = 0;
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsBot: false, IsHLTV: false } && (AllowSpectators || player.Team != CsTeam.Spectator))
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

    private void Reply(CCSPlayerController? player, CommandInfo? info, string message)
    {
        if (info != null)
        {
            info.ReplyToCommand(message);
            return;
        }

        if (player != null)
        {
            player.PrintToChat(message);
            return;
        }
        
        Server.PrintToConsole(message);
    }
}