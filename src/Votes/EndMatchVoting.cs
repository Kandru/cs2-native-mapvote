using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace NativeMapVote
{
    public partial class NativeMapVote
    {
        private double GetMapRating(string mapName)
        {
            if (!Config.Maps.TryGetValue(mapName, out var mapConfig))
            {
                return 0;
            }

            int totalVotes = mapConfig.VotesPositive + mapConfig.VotesNegative;
            if (totalVotes == 0)
            {
                return 0;
            }

            return (double)mapConfig.VotesPositive / totalVotes;
        }

        private List<string> GetFilteredMapsByRating(IEnumerable<string> maps, double minRating, bool excludeThreshold = false)
        {
            return maps
                .Where(map =>
                {
                    if (excludeThreshold && GetMapRating(map) < minRating)
                    {
                        return false;
                    }
                    return true;
                })
                .ToList();
        }

        private List<string> GetMostPlayedMaps(int total = 5)
        {
            return [.. Config.Maps
                .OrderByDescending(static x => x.Value.TimesPlayed)
                .Take(total)
                .Select(static x => x.Key)];
        }

        private List<string> GetLeastPlayedMaps(int total = 5)
        {
            return [.. Config.Maps
                .OrderBy(static x => x.Value.TimesPlayed)
                .Take(total)
                .Select(static x => x.Key)];
        }

        private List<string> GetBestVotedMaps(int total = 5)
        {
            return [.. Config.Maps
                .Where(x => x.Value.VotesPositive + x.Value.VotesNegative > 0)
                .OrderByDescending(x => GetMapRating(x.Key))
                .ThenByDescending(x => x.Value.VotesPositive)
                .Take(total)
                .Select(static x => x.Key)];
        }

        private List<string> GetWorstVotedMaps(int total = 5)
        {
            return [.. Config.Maps
                .Where(x => x.Value.VotesPositive + x.Value.VotesNegative > 0)
                .OrderBy(x => GetMapRating(x.Key))
                .ThenBy(x => x.Value.VotesPositive)
                .Take(total)
                .Select(static x => x.Key)];
        }

        private List<string> GetMapsForEndMatchVoting()
        {
            if (_workshopMaps.Count == 0 || Config.Maps.Count == 0)
            {
                return [];
            }

            var selectedMaps = new List<string>();
            var seenMaps = new HashSet<string>();

            var displayOrder = Config.Endmatch.DisplayOrder ?? new List<string>
            {
                "nominations",
                "most_played",
                "least_played",
                "best_voted",
                "worst_voted"
            };

            foreach (var displayType in displayOrder)
            {
                if (selectedMaps.Count >= Config.Endmatch.MaxMaps)
                {
                    break;
                }

                List<string> candidateMaps = displayType switch
                {
                    "nominations" => Config.Endmatch.DisplayNominations
                        ? [.. _nominations.Values.Distinct()]
                        : [],
                    "most_played" => Config.Endmatch.DisplayMostPlayed.Enabled
                        ? GetMostPlayedMaps(Config.Endmatch.DisplayMostPlayed.Amount)
                        : [],
                    "least_played" => Config.Endmatch.DisplayLeastPlayed.Enabled
                        ? GetLeastPlayedMaps(Config.Endmatch.DisplayLeastPlayed.Amount)
                        : [],
                    "best_voted" => Config.Endmatch.DisplayBestVoted.Enabled
                        ? GetBestVotedMaps(Config.Endmatch.DisplayBestVoted.Amount)
                        : [],
                    "worst_voted" => Config.Endmatch.DisplayWorstVoted.Enabled
                        ? GetWorstVotedMaps(Config.Endmatch.DisplayWorstVoted.Amount)
                        : [],
                    _ => []
                };

                if (displayType != "nominations")
                {
                    candidateMaps = GetFilteredMapsByRating(candidateMaps, Config.Endmatch.HideWorstRatingThreshold, excludeThreshold: true);
                }

                foreach (var map in candidateMaps)
                {
                    if (selectedMaps.Count >= Config.Endmatch.MaxMaps)
                    {
                        break;
                    }

                    if (seenMaps.Add(map))
                    {
                        selectedMaps.Add(map);
                    }
                }
            }

            return selectedMaps.Where(_workshopMaps.Contains).ToList();
        }

        private void UpdateEndMatchVoting()
        {
            if (_workshopMaps.Count == 0 || Config.Maps.Count == 0)
            {
                return;
            }

            List<string> maps = GetMapsForEndMatchVoting();
            IEnumerable<CCSGameRulesProxy> proxies = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            CCSGameRulesProxy? proxy = proxies.FirstOrDefault();
            if (proxy == null || !proxy.IsValid || proxy.GameRules == null)
            {
                return;
            }

            var mapEnumerator = maps.GetEnumerator();

            foreach (ref int option in proxy.GameRules.EndMatchMapGroupVoteOptions)
            {
                if (mapEnumerator.MoveNext())
                {
                    option = _workshopMaps.IndexOf(mapEnumerator.Current);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
