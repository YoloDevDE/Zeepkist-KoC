using ZeepSDK.Chat;

namespace KoC.states;

public class StateDisabled(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        ChatApi.SendMessage("/joinmessage off");
        ChatApi.SendMessage("/servermessage remove");
        StateMachine.Enabled = false;
    }

    public override void Exit()
    {
        StateMachine.Enabled = true;
    }
}