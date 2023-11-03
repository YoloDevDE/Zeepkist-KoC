using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class EndVoting : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc result";
    public string Description => "";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}