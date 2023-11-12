namespace KoC.Data;

public class SubmissionLevel
{
    public SubmissionLevel(LevelScriptableObject globalLevel)
    {
        LevelUid = globalLevel.UID;
        Name = globalLevel.Name;
        Author = globalLevel.Author;
        WorkshopId = globalLevel.WorkshopID;
        AuthorSteamId = globalLevel.WorkshopID == 0 ? 0 : WorkshopManager.Instance.WorkshopInfoDictionary[globalLevel.WorkshopID].authorSteamID;
    }

    public ulong WorkshopId { get; set; }

    public string Name { get; set; }

    public string LevelUid { get; set; }

    public string Author { get; set; }
    public ulong AuthorSteamId { get; set; }
}