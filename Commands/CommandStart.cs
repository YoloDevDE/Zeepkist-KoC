using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class CommandStart : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc start";
    public string Description => "Starts the Kick or Clutch Mod - It will immediately register the current level as a submission level automatically";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}