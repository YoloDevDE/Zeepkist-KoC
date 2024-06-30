using System.Collections.Generic;
using System.Linq;
using KoC.models;

namespace KoC.utils;

public static class LevelUtils
{
    public static bool IsVotingLevel(string currentLevelUid, IEnumerable<VotingLevel> votingLevels)
    {
        return votingLevels.Any(vl => vl.LevelUid == currentLevelUid);
    }

    public static bool IsAdventureLevel()
    {
        return PlayerManager.Instance.currentMaster.GlobalLevel.UseAvonturenLevel;
    }
}