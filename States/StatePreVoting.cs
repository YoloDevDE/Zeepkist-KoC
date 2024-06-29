using KoC.Commands;
using KoC.Utils;
using ZeepkistClient;
using ZeepSDK.Racing;

namespace KoC.States;

public class StatePreVoting : BaseState
{
    public StatePreVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        CommandRegisterSubmissionLevel.OnHandle += RegisterSubmissionLevel;
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded()
    {
        if (LevelUtils.IsAdventureLevel())
        {
            Plugin.Instance.Messenger
                .LogWarning(
                    "Voting-Level expected but got Adventure-Level. You may want to skip to a Voting-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
            return;
        }

        if (!LevelUtils.IsVotingLevel(ZeepkistNetwork.CurrentLobby.LevelUID, StateMachine.VotingLevels))
        {
            Plugin.Instance.Messenger
                .LogWarning(
                    "Voting-Level expected but got Adventure-Level. You may want to skip to a Voting-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
            return;
        }

        Plugin.Instance.Messenger.LogSuccess("Voting started");
        StateMachine.TransitionTo(new StateVoting(StateMachine));
    }


    private void RegisterSubmissionLevel()
    {
        StateMachine.OverrideSubmission = true;
        StateMachine.TransitionTo(new StateRegisterSubmission(StateMachine));
    }

    public override void Exit()
    {
        CommandRegisterSubmissionLevel.OnHandle -= RegisterSubmissionLevel;
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }
}