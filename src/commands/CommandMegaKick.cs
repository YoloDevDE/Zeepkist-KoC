using System;
using ZeepSDK.ChatCommands;

namespace KoC.commands;

public class CommandMegaKick : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc megakick";
    public string Description => "Initiates a Megakick";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}