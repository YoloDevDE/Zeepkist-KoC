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
}