using ZeepSDK.Chat;

namespace KoC.States;

public class StateDisabled : BaseState
{
    public StateDisabled(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        ChatApi.SendMessage($"/joinmessage off");
        ChatApi.SendMessage($"/servermessage remove");
        StateMachine.Enabled = false;
    }

    public override void Exit()
    {
        StateMachine.Enabled = true;
        ChatApi.SendMessage($"/joinmessage orange {StateMachine.Plugin.JoinMessageNormal.Value}");
    }
}