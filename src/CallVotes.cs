using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace NativeMapVote;

public partial class NativeMapVote
{
    private string _callVoteMap = "";
    private string? _mapNextRound;
    private readonly ChatVote _callVoteChatVote;

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_mapNextRound == null) return HookResult.Continue;
        Server.ExecuteCommand(IsNativeMap(_mapNextRound)
            ? Config.CallVoteChangeMapCommand.Replace("{map}", _mapNextRound)
            : Config.CallVoteChangeMapCommandWorkshop.Replace("{map}", _mapNextRound));
        _mapNextRound = null;
        return HookResult.Continue;
    }

    private void OnCallVoteCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!Config.CallVoteEnabled)
        {
            info.ReplyToCommand(Localizer["callVotes.disabledManually"]);
            return;
        }

        if (Config.WorkshopMaps.Count == 0)
        {
            info.ReplyToCommand(Localizer["callVotes.disabledEmptyMapgroup"]);
            return;
        }

        if (_callVoteChatVote.Running || _mapNextRound != null)
        {
            _callVoteChatVote.SubmitVote(player, info);
            return;
        }

        // if no argument was given, we can't possibly know what the user wants - give him all the options then
        if (info.ArgCount <= 1)
        {
            if (player == null)
            {
                info.ReplyToCommand(Localizer["callVotes.beMoreSpecific"]);
                return;
            }

            if (_callVoteMenuAllMaps == null) return;
            MenuManager.OpenChatMenu(player, _callVoteMenuAllMaps);
            return;
        }

        // if the exact map name was given, we can use a shortcut
        var query = info.GetArg(1);
        if (Config.WorkshopMaps.Contains(query))
        {
            StartCallVote(query, player, info);
            return;
        }

        var menu = new ChatMenu(Localizer["callVotes.chatMenuTitle"]);
        foreach (var mapName in Config.WorkshopMaps)
        {
            if (mapName.StartsWith(query) || mapName.Contains(query))
            {
                menu.AddMenuOption(mapName, (_, _) =>
                {
                    StartCallVote(mapName, player, info);
                });
            }
        }

        if (menu.MenuOptions.Count == 0)
        {
            info.ReplyToCommand(Localizer["callVotes.noMapFound"]);
            return;
        }

        // yay, shortcut time
        if (menu.MenuOptions.Count == 1)
        {
            StartCallVote(menu.MenuOptions[0].Text, player, info);
            return;
        }

        // if we deal with an RCON or internal command invocation, and we have multiple options, we cannot really show a menu unfortunately
        if (player == null)
        {
            info.ReplyToCommand(Localizer["callVotes.couldNotIdentify"]);
            foreach (var option in menu.MenuOptions)
            {
                info.ReplyToCommand("- " + option.Text);
            }
            return;
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void OnCallVoteVoteSucceeded()
    {
        _mapNextRound = _callVoteMap;
    }

    private void StartCallVote(string mapName, CCSPlayerController? player, CommandInfo? info)
    {
        _callVoteMap = mapName;

        _callVoteChatVote.Localizer.AlreadyVoted = Localizer["callVotes.alreadyVoted"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VotedSuccessfully = Localizer["callVotes.votedSuccessfully"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteSucceeded = Localizer["callVotes.voteSucceeded"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteFailed = Localizer["callVotes.voteFailed"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteAlreadySucceeded = Localizer["callVotes.alreadySucceeded"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteStartedByPlayer = Localizer["callVotes.voteStartedByPlayer"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.VoteStartedByUnknownEntity = Localizer["callVotes.voteStartedByUnknownEntity"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.Notification = Localizer["callVotes.status"].Value.Replace("{map}", mapName);
        _callVoteChatVote.Localizer.NotificationHint = Localizer["callVotes.statusHint"].Value.Replace("{map}", mapName);
        _callVoteChatVote.SubmitVote(player, info);
    }

    private bool IsNativeMap(string mapName)
    {
        string[] files = Directory.GetFiles(Server.GameDirectory + "/csgo/maps");
        foreach (string file in files)
        {
            if (Path.GetFileNameWithoutExtension(file) == mapName) return true;
        }
        return false;
    }

    private void ResetCallVotes()
    {
        _callVoteChatVote.Reset();
        _mapNextRound = null;
    }
}