namespace KoC.Data;

public class SubmissionLevel
{
    public SubmissionLevel(LevelScriptableObject globalLevel)
    {
        LevelUid = globalLevel.UID;
        Name = globalLevel.Name;
        Author = globalLevel.Author;
    }

    public ulong WorkshopId { get; set; }

    public string Name { get; set; }

    public string LevelUid { get; set; }

    public string Author { get; set; }
}