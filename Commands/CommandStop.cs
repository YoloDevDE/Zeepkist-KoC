using System;
using ZeepSDK.ChatCommands;

namespace KoC.Commands;

public class CommandStop : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc stop";
    public string Description => "Stops the Kick or Clutch Mod.";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}