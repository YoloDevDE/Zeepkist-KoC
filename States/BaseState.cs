using ZeepSDK.Messaging;

namespace KoC.States;

public abstract class BaseState
{
    public BaseState(StateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }
    

    public StateMachine StateMachine { get; }

    public abstract void Enter();
    public abstract void Exit();

    public virtual void Kill()
    {
        MessengerApi.Log("KoC stopped");
        StateMachine.KillSwitch();
    }

    public virtual void KillOnError()
    {
        MessengerApi.Log("KoC stopped");
        StateMachine.KillSwitch();
    }

    public virtual void Start()
    {
        MessengerApi.LogWarning("KoC is already running");
    }
}