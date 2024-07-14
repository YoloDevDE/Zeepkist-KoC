using KoC.commands;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StatePreVoting(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        StateMachine.InitializeEligibleVoters();
        CommandRegisterSubmissionLevel.OnHandle += RegisterSubmissionLevel;
        RacingApi.LevelLoaded += OnLevelLoaded;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
    }


    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        StateMachine.EligibleVoters.Add(player);
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
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
    }
}