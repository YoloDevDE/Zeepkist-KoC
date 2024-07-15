using KoC.commands;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StatePreVoting(KoC koC) : BaseState(koC)
{
    public override void Enter()
    {
        KoC.InitializeEligibleVoters();
        CommandRegisterSubmissionLevel.OnHandle += RegisterSubmissionLevel;
        RacingApi.LevelLoaded += OnLevelLoaded;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
    }


    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        KoC.EligibleVoters.Add(player);
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

        if (!LevelUtils.IsVotingLevel(ZeepkistNetwork.CurrentLobby.LevelUID, KoC.VotingLevels))
        {
            Plugin.Instance.Messenger
                .LogWarning(
                    "Voting-Level expected but got Adventure-Level. You may want to skip to a Voting-Level. If you want to vote this level type '/koc register' and continue as usual.",
                    10F);
            return;
        }

        Plugin.Instance.Messenger.LogSuccess("Voting started");
        KoC.TransitionTo(new StateVoting(KoC));
    }


    private void RegisterSubmissionLevel()
    {
        KoC.OverrideSubmission = true;
        KoC.TransitionTo(new StateRegisterSubmission(KoC));
    }

    public override void Exit()
    {
        CommandRegisterSubmissionLevel.OnHandle -= RegisterSubmissionLevel;
        RacingApi.LevelLoaded -= OnLevelLoaded;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
    }
}