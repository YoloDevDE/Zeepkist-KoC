using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KoC.commands;
using KoC.models;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StateRegisterSubmission(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        OnLevelLoaded();
        InitializeEligibleVoters();
        RacingApi.LevelLoaded += OnLevelLoaded;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
        CommandRegisterSubmissionLevel.OnHandle += OverrideSubmissionLevel;
        ChatApi.SendMessage($"/joinmessage orange {Plugin.Instance.JoinMessageNormal}");
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        StateMachine.EligibleVoters.Add(player);
    }

    private void InitializeEligibleVoters()
    {
        StateMachine.EligibleVoters = new List<ZeepkistNetworkPlayer>();
        StateMachine.EligibleVoters.AddRange(ZeepkistNetwork.Players.Values);
    }


    private void OverrideSubmissionLevel()
    {
        StateMachine.OverrideSubmission = true;
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
    }

    private void RegisterSubmissionLevel(SubmissionLevel submissionLevel)
    {
        if (!StateMachine.OverrideSubmission)
        {
            if (IsAdventureLevel())
            {
                Plugin.Instance.Messenger.LogWarning(
                    "Submission-Level expected but got Adventure-Level. You may want to skip to a Submission-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
                return;
            }

            if (LevelUtils.IsVotingLevel(ZeepkistNetwork.CurrentLobby.LevelUID, StateMachine.VotingLevels))
            {
                Plugin.Instance.Messenger.LogWarning(
                    "Submission-Level expected but got Voting-Level. You may want to skip to a Submission-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
                return;
            }
        }

        StateMachine.SubmissionLevel = submissionLevel;
        Plugin.Instance.Messenger.LogSuccess($"Submission-Level registered for Voting: '{StateMachine.SubmissionLevel.Name}'", 5F);
        StateMachine.OverrideSubmission = false;
        StateMachine.TransitionTo(new StatePreVoting(StateMachine));
    }

    private bool IsAdventureLevel()
    {
        return PlayerManager.Instance.currentMaster.GlobalLevel.UseAvonturenLevel;
    }
}