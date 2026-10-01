using System.Linq;
using Robust.Shared.Random;

namespace Content.Server.CounterStrike.Systems;

public static class CounterStrikeTeamBalancer
{
    public static IReadOnlyList<T> ShuffleAndSplit<T>(IRobustRandom random, IEnumerable<T> players, out int firstTeamCount)
    {
        var shuffled = players.ToList();
        random.Shuffle(shuffled);

        firstTeamCount = (shuffled.Count + (random.Prob(0.5f) ? 1 : 0)) / 2;
        return shuffled;
    }
}
