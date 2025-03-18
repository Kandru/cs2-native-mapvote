using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace NativeMapVote;

public partial class NativeMapVote
{
    private void CountMap(string mapName)
    {
        // count the map
        if (Config.Maps.ContainsKey(mapName.ToLower()))
            Config.Maps[mapName.ToLower()]++;
    }

    private List<string> GetTopMaps(int total = 8)
    {
        if (total > Config.AmountTopMapsToShow)
            total = Config.AmountTopMapsToShow;
        // get the top maps
        return Config.Maps.OrderByDescending(x => x.Value).Take(total).Select(x => x.Key).ToList();
    }

    private List<string> GetNewestMaps(int total = 8)
    {
        if (total > Config.AmountNewestMapsToShow)
            total = Config.AmountNewestMapsToShow;
        // get the new maps
        return Config.Maps.OrderByDescending(x => x.Key).Take(total).Select(x => x.Key).ToList();
    }

    private List<string> GetAllMaps(int total = 8)
    {
        int totalTop = total / 2;
        int totalNew = total - totalTop;

        // combine the top and new maps
        return GetTopMaps(totalTop).Concat(GetNewestMaps(totalNew)).Distinct().ToList();
    }

    private void OnNextMapsByCountCommand(CCSPlayerController? player, CommandInfo info)
    {
        // get all maps
        var allMaps = GetAllMaps(Config.AmountTopMapsToShow + Config.AmountNewestMapsToShow);
        if (allMaps.Count == 0)
        {
            Server.PrintToChatAll(Localizer["nominations.noNominationsYet"]);
            Server.PrintToConsole(Localizer["nominations.noNominationsYet"]);
            return;
        }

        var reply = Localizer["nominations.nominationListPrefix"] + allMaps.First();
        if (allMaps.Count > 1)
        {
            bool first = true;
            foreach (var mapName in allMaps)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                reply += ", " + mapName;
            }
        }
        Server.PrintToChatAll(reply);
        Server.PrintToConsole(reply);
    }
}