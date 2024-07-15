namespace KoC.states;

public abstract class BaseState(KoC koC)
{
    public KoC KoC { get; } = koC;

    public abstract void Enter();
    public abstract void Exit();
}