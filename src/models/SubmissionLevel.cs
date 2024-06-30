using System.Threading.Tasks;
using KoC.utils;

namespace KoC.models;

public class SubmissionLevel(
    ulong workshopId,
    string levelUid,
    string levelName,
    string authorName)
{
    // Initialisiere AuthorSteamId synchron mit einem Platzhalterwert

    public ulong WorkshopId { get; set; } = workshopId;

    public string Name { get; set; } = levelName;

    public string LevelUid { get; set; } = levelUid;
    public int VotesClutch { get; set; }
    public int VotesKick { get; set; }

    public string Author { get; set; } = authorName;
    public ulong AuthorSteamId { get; set; } = 0;

    public async Task InitializeAsync()
    {
        if (WorkshopId != 0)
        {
            SteamUGCHelper steamUGCHelper = new SteamUGCHelper();
            AuthorSteamId = await steamUGCHelper.GetSteamIdFromWorkshopItemAsync(WorkshopId);
        }
    }

    public void ResetVotes()
    {
        VotesClutch = 0;
        VotesKick = 0;
    }

    public void AddVoteKick()
    {
        VotesKick++;
    }

    public void AddVoteClutch()
    {
        VotesClutch++;
    }
}