using KoC.Data;
using ZeepkistClient;
using ZeepSDK.Racing;

namespace KoC.States;

public class StateRegisterSubmission : BaseState
{
    public StateRegisterSubmission(StateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        RegisterSubmissionLevel(new SubmissionLevel(PlayerManager.Instance.currentMaster.GlobalLevel, ZeepkistNetwork.CurrentLobby.WorkshopID));
    }

    private void OnLevelLoaded()
    {
        RegisterSubmissionLevel(new SubmissionLevel(PlayerManager.Instance.currentMaster.GlobalLevel,ZeepkistNetwork.CurrentLobby.WorkshopID));
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    private bool IsVotingLevel(string currentLevelUid)
    {
        foreach (VotingLevel votingLevel in StateMachine.VotingLevels)
        {
            if (votingLevel.LevelUid == currentLevelUid)
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterSubmissionLevel(SubmissionLevel submissionLevel)
    {
        if (IsAdventureLevel())
        {
            StateMachine.Plugin.Messenger.LogWarning(
                "Submission-Level expected but got Adventure-Level. Please Skip to a different level that is no Adventure-Level.",
                5F);
        }
        else if (IsVotingLevel(PlayerManager.Instance.currentMaster.GlobalLevel.UID))
        {
            StateMachine.Plugin.Messenger.LogWarning(
                "Submission-Level expected but got Voting-Level. Please Skip to a different level that is no Voting-Level.",
                5F);
        }
        else
        {
            StateMachine.SubmissionLevel = submissionLevel;
            StateMachine.Plugin.Messenger.LogSuccess(
                $"Submission-Level registered for Voting: '{StateMachine.SubmissionLevel.Name}'", 5F);
            StateMachine.SwitchState(new StateBeforeVoting(StateMachine));
        }
    }

    private bool IsAdventureLevel()
    {
        return PlayerManager.Instance.currentMaster.GlobalLevel.UseAvonturenLevel;
    }
}