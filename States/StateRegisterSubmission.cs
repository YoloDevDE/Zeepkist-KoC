using KoC.Data;
using KoC.Utils;
using ZeepkistClient;
using ZeepSDK.Chat;
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
        ChatApi.SendMessage($"/joinmessage orange {Plugin.Instance.JoinMessageNormal.Value}");
    }

    private void OnLevelLoaded()
    {
        RegisterSubmissionLevel(new SubmissionLevel(PlayerManager.Instance.currentMaster.GlobalLevel, ZeepkistNetwork.CurrentLobby.WorkshopID));
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }


    private void RegisterSubmissionLevel(SubmissionLevel submissionLevel)
    {
        if (IsAdventureLevel())
        {
            Plugin.Instance.Messenger.LogWarning(
                "Submission-Level expected but got Adventure-Level. Please Skip to a different level that is no Adventure-Level.",
                5F);
            return;
        }

        if (LevelUtils.IsVotingLevel(ZeepkistNetwork.CurrentLobby.LevelUID, StateMachine.VotingLevels))
        {
            Plugin.Instance.Messenger.LogWarning(
                "Submission-Level expected but got Voting-Level. Please Skip to a different level that is no Voting-Level.",
                5F);
            return;
        }

        StateMachine.CurrentSubmissionLevel = submissionLevel;
        Plugin.Instance.Messenger.LogSuccess($"Submission-Level registered for Voting: '{StateMachine.CurrentSubmissionLevel.Name}'", 5F);
        StateMachine.TransitionTo(new StatePreVoting(StateMachine));
    }

    private bool IsAdventureLevel()
    {
        return PlayerManager.Instance.currentMaster.GlobalLevel.UseAvonturenLevel;
    }
}