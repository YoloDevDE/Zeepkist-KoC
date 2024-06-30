namespace KoC.states;

public abstract class BaseState(StateMachine stateMachine)
{
    public StateMachine StateMachine { get; } = stateMachine;

    public abstract void Enter();
    public abstract void Exit();
}

