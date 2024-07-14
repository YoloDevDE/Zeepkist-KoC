using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public static async Task<SubmissionLevel> RegisterSubmissionLevel(string levelUid, ulong workshopId, string authorName, string levelName)
    {
        const int maxRetries = 5;
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            SubmissionLevel submissionLevel = new SubmissionLevel(levelUid: levelUid, workshopId: workshopId, levelName: levelName, authorName: authorName);
            await submissionLevel.InitializeAsync();
            return submissionLevel;
        }, maxRetries);
    }
}