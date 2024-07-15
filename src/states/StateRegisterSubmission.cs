using System;
using System.Threading.Tasks;
using KoC.commands;
using KoC.models;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StateRegisterSubmission(KoC koC) : BaseState(koC)
{
    public override void Enter()
    {
        OnLevelLoaded();
        RacingApi.LevelLoaded += OnLevelLoaded;
        CommandRegisterSubmissionLevel.OnHandle += OverrideSubmissionLevel;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
        ChatApi.SendMessage($"/joinmessage orange {Plugin.Instance.JoinMessageNormal}");
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        KoC.EligibleVoters.Add(player);
    }


    private void OverrideSubmissionLevel()
    {
        KoC.OverrideSubmission = true;
        OnLevelLoaded();
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
        CommandRegisterSubmissionLevel.OnHandle -= OverrideSubmissionLevel;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
    }

    private void RegisterSubmissionLevel(SubmissionLevel submissionLevel)
    {
        if (!KoC.OverrideSubmission)
        {
            if (IsAdventureLevel())
            {
                Plugin.Instance.Messenger.LogWarning(
                    "Submission-Level expected but got Adventure-Level. You may want to skip to a Submission-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
                return;
            }

            if (LevelUtils.IsVotingLevel(ZeepkistNetwork.CurrentLobby.LevelUID, KoC.VotingLevels))
            {
                Plugin.Instance.Messenger.LogWarning(
                    "Submission-Level expected but got Voting-Level. You may want to skip to a Submission-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
                return;
            }
        }

        KoC.SubmissionLevel = submissionLevel;
        KoC.CachedSubmissionLevel = submissionLevel;
        Plugin.Instance.Messenger.LogSuccess($"Submission-Level registered for Voting: '{KoC.SubmissionLevel.Name}'", 5F);
        KoC.OverrideSubmission = false;
        KoC.TransitionTo(new StatePreVoting(KoC));
    }

    private bool IsAdventureLevel()
    {
        return PlayerManager.Instance.currentMaster.GlobalLevel.UseAvonturenLevel;
    }
}