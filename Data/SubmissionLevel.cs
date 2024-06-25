namespace KoC.Data;

public class SubmissionLevel
{
    public SubmissionLevel(LevelScriptableObject globalLevel, ulong workshopId)
    {
        LevelUid = globalLevel.UID;
        Name = globalLevel.Name;
        Author = globalLevel.Author;
        WorkshopId = workshopId;
        AuthorSteamId = workshopId == 0 ? 0 : WorkshopManager.Instance.WorkshopInfoDictionary[workshopId].authorSteamID;
    }

    public ulong WorkshopId { get; set; }

    public string Name { get; set; }

    public string LevelUid { get; set; }

    public string Author { get; set; }
    public ulong AuthorSteamId { get; set; }
}