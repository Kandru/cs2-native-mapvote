using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private List<string> GetMostPlayedMaps(int total = 5)
        {
            return Config.Maps.OrderByDescending(x => x.Value.TimesPlayed)
                .Take(total)
                .Select(x => x.Key)
                .ToList();
        }

        private List<string> GetLeastPlayedMaps(int total = 5)
        {
            return Config.Maps.OrderBy(x => x.Value.TimesPlayed)
                .Take(total)
                .Select(x => x.Key)
                .ToList();
        }

        private List<string> GetRandomMaps(int total = 5)
        {
            return Config.Maps.OrderBy(x => Guid.NewGuid())
                .Take(total)
                .Select(x => x.Key)
                .ToList();
        }

        private List<string> GetMapsForEndMatchVoting(int total = 10, int random = 4)
        {
            total -= random;
            int totalMost = total / 2;
            int totalLeast = total - totalMost;

            var maps = GetMostPlayedMaps(totalMost)
                .Concat(GetLeastPlayedMaps(totalLeast))
                .Distinct()
                .ToList();

            if (random > 0)
            {
                var randomMaps = GetRandomMaps(100)
                    .Where(map => !maps.Contains(map))
                    .Take(total - maps.Count + random);
                maps.AddRange(randomMaps);
            }

            return maps;
        }

        private void UpdateEndMatchVoting()
        {
            if (_workshopMaps.Count == 0
                || Config.Maps.Count == 0) return;
            List<string> maps = GetMapsForEndMatchVoting(
                Config.EndmapVoteAmountMaps,
                Config.EndmapVoteAmountRandomMaps
            );
            var proxies = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            var proxy = proxies.FirstOrDefault();
            if (proxy == null
                || !proxy.IsValid
                || proxy.GameRules == null) return;
            // we need to merge _nominations and maps until we reach EndmapVoteAmountMaps. Priority is on _nominations, fill up with maps
            var endMaps = _nominations.Values.Concat(maps)
                .Distinct()
                .Take(Config.EndmapVoteAmountMaps);
            // only use endMaps which are present in _workshopMaps (vote only refers to mapgroup maps)
            endMaps = endMaps.Where(_workshopMaps.Contains);
            foreach (ref var option in proxy.GameRules.EndMatchMapGroupVoteOptions)
            {
                // get map from endMapsList and assign it to option
                if (!endMaps.Any()) break;
                if (!_workshopMaps.Contains(endMaps.First()))
                {
                    endMaps = endMaps.Skip(1);
                    continue;
                }
                option = _workshopMaps.IndexOf(endMaps.First());
                endMaps = endMaps.Skip(1);
            }
        }
    }
}
