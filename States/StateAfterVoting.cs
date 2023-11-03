using ZeepSDK.Racing;

namespace KoC.States;

public class StateAfterVoting : BaseState
{
    public StateAfterVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += RacingApiOnLevelLoaded;
    }

    private void RacingApiOnLevelLoaded()
    {
        StateMachine.SwitchState(new StateDriving(StateMachine));
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= RacingApiOnLevelLoaded;
    }
}