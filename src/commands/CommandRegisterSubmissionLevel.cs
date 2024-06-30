using System;
using ZeepSDK.ChatCommands;

namespace KoC.commands;

public class CommandRegisterSubmissionLevel : ILocalChatCommand
{
    public static Action OnHandle;
    public string Prefix => "/";
    public string Command => "koc register";
    public string Description => "Use this to manually register a submission level.";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }
}