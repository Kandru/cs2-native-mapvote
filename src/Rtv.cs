using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace NativeMapVote;

public partial class NativeMapVote
{
    private readonly ChatVote _rtvChatVote;

    private void OnRtvCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount > 1)
        {
            OnNominateCommand(player, info);
        }

        _rtvChatVote.SubmitVote(player, info);
    }

    private void OnRtvVoteSucceeded()
    {
        Server.ExecuteCommand(Config.RtvEndMatchCommand);
    }

    private void ResetRtv()
    {
        _rtvChatVote.Reset();
    }
}