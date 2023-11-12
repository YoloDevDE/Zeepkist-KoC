using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class DisablePlugin : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc stop";
    public string Description => "";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}