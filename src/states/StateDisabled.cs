using System.Collections.Generic;
using KoC.utils;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace KoC.states;

public class StateDisabled(StateMachine stateMachine) : BaseState(stateMachine)
{
    public override void Enter()
    {
        ChatApi.SendMessage("/joinmessage off");
        ChatApi.SendMessage("/servermessage remove");
        StateMachine.Enabled = false;
        StateMachine.SubmissionLevel = null;
        MultiplayerApi.ConnectedToGame += OnConnectedToOnlineLobby;
    }

    public void OnConnectedToOnlineLobby()
    {
        MultiplayerApi.ConnectedToGame -= OnConnectedToOnlineLobby;
        MultiplayerApi.DisconnectedFromGame += OnDisconnectedToOnlineLobby;
        RacingApi.LevelLoaded += OnLevelLoaded;
        MultiplayerApi.PlayerJoined += OnPlayerJoined;
    }

    public void OnDisconnectedToOnlineLobby()
    {
        MultiplayerApi.ConnectedToGame += OnConnectedToOnlineLobby;
        MultiplayerApi.DisconnectedFromGame -= OnDisconnectedToOnlineLobby;
        RacingApi.LevelLoaded -= OnLevelLoaded;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
    }

    public async void OnLevelLoaded()
    {
        string levelUid = ZeepkistNetwork.CurrentLobby.LevelUID;
        ulong workshopId = ZeepkistNetwork.CurrentLobby.WorkshopID;
        string authorName = PlayerManager.Instance.currentMaster.GlobalLevel.Author;
        string levelName = PlayerManager.Instance.currentMaster.GlobalLevel.Name;
        if (!LevelUtils.IsVotingLevel(levelUid, StateMachine.VotingLevels))
        {
            StateMachine.CachedSubmissionLevel = await LevelUtils.RegisterSubmissionLevel(levelUid, workshopId, authorName, levelName);
            StateMachine.InitializeEligibleVoters();
            Plugin.Instance.GetLogger().LogInfo($"Level Cached: {levelName} by {authorName} -- UID: {levelUid} -- WorkshopId: {workshopId}");
            Plugin.Instance.GetLogger().LogInfo($"Eligible Voters: {BlameNic(StateMachine.EligibleVoters)}");
        }
    }

    private string BlameNic(List<ZeepkistNetworkPlayer> networkPlayers)
    {
        string str = "[";
        foreach (ZeepkistNetworkPlayer zeepkistNetworkPlayer in networkPlayers)
        {
            str += zeepkistNetworkPlayer.Username + ", ";
        }

        str = str.Substring(0, str.Length - 2) + "]";
        return str;
    }

    private void OnPlayerJoined(ZeepkistNetworkPlayer player)
    {
        StateMachine.EligibleVoters.Add(player);
    }


    public override void Exit()
    {
        MultiplayerApi.ConnectedToGame -= OnConnectedToOnlineLobby;
        MultiplayerApi.DisconnectedFromGame -= OnDisconnectedToOnlineLobby;
        RacingApi.LevelLoaded -= OnLevelLoaded;
        MultiplayerApi.PlayerJoined -= OnPlayerJoined;
        StateMachine.Enabled = true;
    }
}