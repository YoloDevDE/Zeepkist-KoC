using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KoC.utils;

namespace KoC.models;

public class SubmissionLevel(
    ulong workshopId,
    string levelUid,
    string levelName,
    string authorName)
{
    public ulong WorkshopId { get; set; } = workshopId;
    public string Name { get; set; } = levelName;
    public string LevelUid { get; set; } = levelUid;
    public List<Vote> Votes { get; } = new List<Vote>();
    public string Author { get; set; } = authorName;

    public List<string> AllAuthors { get; set; } = [];
    public ulong AuthorSteamId { get; set; }

    public int VotesClutch => Votes.Count(v => !v.IsKick);
    public int VotesKick => Votes.Count(v => v.IsKick);

    public async Task InitializeAsync()
    {
        if (WorkshopId != 0)
        {
            SteamUGCHelper steamUGCHelper = new SteamUGCHelper();
            AuthorSteamId = await steamUGCHelper.GetSteamIdFromWorkshopItemAsync(WorkshopId);
            // List<string> contributorsSteamIdsAsync = await SteamWorkshopContributors.GetContributorsAsync(AuthorSteamId);
            // foreach (string contributerSteamId in contributorsSteamIdsAsync)
            // {
            //     ChatApi.SendMessage($"[{contributerSteamId}]");
            //     // ZeepkistNetwork.TryGetPlayer(contributerSteamId, out ZeepkistNetworkPlayer player);
            //     AllAuthors.Add(contributerSteamId);
            // }
        }
    }

    public void ResetVotes()
    {
        Votes.Clear();
    }

    public void AddVoteKick(ulong steamId)
    {
        Vote existingVote = Votes.FirstOrDefault(v => v.SteamId == steamId);

        if (existingVote != null)
        {
            existingVote.IsKick = true;
        }
        else
        {
            Votes.Add(new Vote { SteamId = steamId, IsKick = true });
        }
    }

    public void AddVoteClutch(ulong steamId)
    {
        Vote existingVote = Votes.FirstOrDefault(v => v.SteamId == steamId);

        if (existingVote != null)
        {
            existingVote.IsKick = false;
        }
        else
        {
            Votes.Add(new Vote { SteamId = steamId, IsKick = false });
        }
    }

    public void RemoveVote(ulong steamId)
    {
        Vote existingVote = Votes.FirstOrDefault(v => v.SteamId == steamId);

        if (existingVote != null)
        {
            Votes.Remove(existingVote);
        }
    }
}