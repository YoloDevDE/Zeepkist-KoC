using System.Threading.Tasks;
using KoC.Utils;

namespace KoC.Data;

public class SubmissionLevel
{
    public SubmissionLevel(
        ulong workshopId,
        string levelUid,
        string levelName,
        string authorName
    )
    {
        WorkshopId = workshopId;
        LevelUid = levelUid;
        Name = levelName;
        Author = authorName;

        // Initialisiere AuthorSteamId synchron mit einem Platzhalterwert
        AuthorSteamId = 0;
    }

    public ulong WorkshopId { get; set; }

    public string Name { get; set; }

    public string LevelUid { get; set; }
    public int VotesClutch { get; set; }
    public int VotesKick { get; set; }

    public string Author { get; set; }
    public ulong AuthorSteamId { get; set; }

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