using System.Collections.Generic;
using KoC.Data;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateDriving : BaseState
{
    public StateDriving(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += RacingApiOnLevelLoaded;
        StateMachine.SubmissionLevel = new SubmissionLevel(PlayerManager.Instance.currentMaster.GlobalLevel);
        MessengerApi.LogSuccess($"KoC State: Driving -> Registered Map '{StateMachine.SubmissionLevel.Name}'");
    }

    private void RacingApiOnLevelLoaded()
    {
        FetchVotingLevel(StateMachine.Plugin.GetVotingLevels());
    }

    private void FetchVotingLevel(List<VotingLevel> votingLevels)
    {
        foreach (var votingLevel in votingLevels)
            if (votingLevel.LevelUid == PlayerManager.Instance.currentMaster.GlobalLevel.UID)
            {
                MessengerApi.LogSuccess("KoC State: Voting");
                StateMachine.SwitchState(new StateVoting(StateMachine));
                return;
            }

        MessengerApi.LogError(
            "Voting level expected but got normal level. With '/koc save' you can register the level as a voting level.",
            10f);
        StateMachine.SwitchState(new StateDriving(StateMachine));
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= RacingApiOnLevelLoaded;
    }
}