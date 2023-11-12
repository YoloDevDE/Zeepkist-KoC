using System;
using ZeepSDK.ChatCommands;
using ZeepSDK.Messaging;

namespace KoC.Commands;

public class RegisterMapForVotingManually : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc register";
    public string Description => "";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}