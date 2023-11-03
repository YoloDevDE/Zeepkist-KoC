using ZeepSDK.Chat;
using ZeepSDK.Messaging;

namespace KoC.States;

public class StateOffline : BaseState
{
    public StateOffline(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
    }

    public override void Start()
    {
        MessengerApi.Log("KoC started");
        ChatApi.SendMessage($"/joinmessage orange {StateMachine.Plugin.JoinMessageNormal.Value}");
        StateMachine.SwitchState(new StateDriving(StateMachine));
    }

    public override void Kill()
    {
        MessengerApi.LogWarning("KoC is not running");
    }

    public override void KillOnError()
    {
        StateMachine.KillSwitch();
    }

    public override void Exit()
    {
    }
}