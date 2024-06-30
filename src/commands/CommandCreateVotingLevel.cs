using System;
using ZeepSDK.ChatCommands;

namespace KoC.commands;

public class CommandCreateVotingLevel : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc save";
    public string Description => "Creates a Voting-Level. Just load the level in an online lobby and type this command. It will be saved in AppData\\Roaming\\Zeepkist\\Mods\\KoC";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}