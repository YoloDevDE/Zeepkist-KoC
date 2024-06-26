using System;
using System.Threading.Tasks;
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
        OnLevelLoaded();
        ChatApi.SendMessage($"/joinmessage orange {Plugin.Instance.JoinMessageNormal.Value}");
    }

    private async void OnLevelLoaded()
    {
        const int maxRetries = 5;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                string levelUid = ZeepkistNetwork.CurrentLobby.LevelUID;
                ulong workshopId = ZeepkistNetwork.CurrentLobby.WorkshopID;
                string authorName = PlayerManager.Instance.currentMaster.GlobalLevel.Author;
                string levelName = PlayerManager.Instance.currentMaster.GlobalLevel.Name;

                SubmissionLevel submissionLevel = new SubmissionLevel(
                    levelUid: levelUid,
                    workshopId: workshopId,
                    levelName: levelName,
                    authorName: authorName
                );
                await submissionLevel.InitializeAsync(); // Asynchrone Initialisierung ohne Blockierung

                RegisterSubmissionLevel(submissionLevel);
                break; // Erfolgreich, Schleife beenden
            }
            catch (Exception e)
            {
                attempt++;
                Plugin.Instance.Messenger.LogError($"Attempt {attempt} to register submission level failed: {e.Message}", 5F);

                if (attempt >= maxRetries)
                {
                    Plugin.Instance.Messenger.LogError("Max retries reached. Unable to register submission level.", 5F);
                }
                else
                {
                    // Kurze Verzögerung zwischen den Versuchen, um das Problem zu beheben
                    await Task.Delay(1000);
                }
            }
        }
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