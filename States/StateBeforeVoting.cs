using KoC.Commands;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateBeforeVoting : BaseState
{
    public StateBeforeVoting(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RegisterMapForVotingManually.OnHandle += RegisterSubmissionLevel;
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded()
    {  
        if (IsAdventureLevel(PlayerManager.Instance.currentMaster.GlobalLevel.WorkshopID))
        {
            StateMachine.Plugin.Messenger.LogWarning(
                "Voting-Level expected but got Adventure-Level. Please Skip to a different level that is no Adventure-Level.",
                5F);
        }
        else if (IsVotingLevel(PlayerManager.Instance.currentMaster.GlobalLevel.UID))
        {            StateMachine.Plugin.Messenger.LogSuccess("Voting started");
            StateMachine.SwitchState(new StateVoting(StateMachine));
        }
        else
        {
            StateMachine.Plugin.Messenger.LogWarning(
                "Voting-Level expected but got different Submission-Level. If that was a mistake you can just skip to a Voting-Level. If you want the current level as a submission type '/koc register' in the chat",
                10F);
        }
    }

    private bool IsAdventureLevel(ulong currentWorkshopID)
    {
        return currentWorkshopID == 0;
    }

    private bool IsVotingLevel(string currentLevelUid)
    {
        foreach (var votingLevel in StateMachine.VotingLevels)
            if (votingLevel.LevelUid == currentLevelUid)
                return true;

        return false;
    }

    private void RegisterSubmissionLevel()
    {
        StateMachine.SwitchState(new StateRegisterSubmission(StateMachine));
    }

    public override void Exit()
    {
        RegisterMapForVotingManually.OnHandle -= RegisterSubmissionLevel;
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }
}