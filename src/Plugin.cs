using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KoC.commands;
using KoC.etc;
using KoC.models;
using KoC.states;
using Newtonsoft.Json;
using ZeepSDK.ChatCommands;
using ZeepSDK.Messaging;
using ZeepSDK.Storage;

namespace KoC;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;

    public ITaggedMessenger Messenger { get; private set; }
    public StateMachine Machine { get; private set; }

    public static Plugin Instance { get; private set; }

    public ConfigEntry<bool> OnlyEligiblePlayersCanVote { get; set; }
    public string AutoMessage { get; set; } = "DO NOT TAKE THE 'MAPPER FINISH'. IF YOU TAKE IT YOU WILL BE AUTOKICKED. IF YOU CAN'T READ YOU HAVE TO FEEL :Yannicsmile:";
    public string ResultServerMessage { get; set; } = "%l by %a<br>%r";
    public string JoinMessageNormal { get; set; } = "Welcome to Kick or Clutch!<br>This session gets recorded and uploaded to Owls YouTube (youtube.com/@owlplague)<br>Make sure you behave in chat and subscribe to Owl :YannicSmile:";

    public string JoinMessageVoting { get; set; } =
        "Welcome to Kick or Clutch!<br>This session gets recorded and uploaded to Owls YouTube (youtube.com/@owlplague)<br>Make sure you behave in chat and subscribe to Owl :YannicSmile:<br>-------<br>DO NOT DRIVE INTO THE 'MAPPER FINISH'!! If you do you WILL get kicked!";

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        Instance = this;
        Messenger = MessengerApi.CreateTaggedMessenger("KoC");
        OnlyEligiblePlayersCanVote = Config.Bind("Voting", "Restricted Voting<br>-> If this setting is <#00FF00>ON<#FFFFFF> only players who were present on the previous submission map can vote", true,
            new ConfigDescription("If this is set to 'true' only players who were present in the previous submission map can vote."));
        RegisterCommands();
        RegisterEvents();
        Machine = new StateMachine();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    public ManualLogSource GetLogger()
    {
        return Logger;
    }

    private void RegisterCommands()
    {
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandVotingResult>();
        ChatCommandApi.RegisterLocalChatCommand<CommandCreateVotingLevel>();
        ChatCommandApi.RegisterLocalChatCommand<CommandRegisterSubmissionLevel>();
        ChatCommandApi.RegisterLocalChatCommand<CommandZivecef>();
    }

    private void RegisterEvents()
    {
        CommandCreateVotingLevel.OnHandle += SaveCurrentLevelAsVotingLevelToJson;
    }

    public List<VotingLevel> GetVotingLevels()
    {
        IModStorage modStorage = StorageApi.CreateModStorage(this);
        VotingLevelsJsonWrapper jsonWrapper = new VotingLevelsJsonWrapper();

        try
        {
            object json = modStorage.LoadFromJson("VotingLevels");
            jsonWrapper = JsonConvert.DeserializeObject<VotingLevelsJsonWrapper>(json.ToString());
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            jsonWrapper.VotingLevels = new List<VotingLevel>();
            modStorage.SaveToJson("VotingLevels", jsonWrapper);
        }

        return jsonWrapper.VotingLevels;
    }

    private void SaveCurrentLevelAsVotingLevelToJson()
    {
        IModStorage modStorage = StorageApi.CreateModStorage(this);

        string currentLevelName = PlayerManager.Instance.currentMaster.GlobalLevel.Name;
        string currentLevelUid = PlayerManager.Instance.currentMaster.GlobalLevel.UID;
        VotingLevelsJsonWrapper jsonWrapper = new VotingLevelsJsonWrapper();

        try
        {
            object json = modStorage.LoadFromJson("VotingLevels");
            Logger.LogInfo(json);
            jsonWrapper = JsonConvert.DeserializeObject<VotingLevelsJsonWrapper>(json.ToString());
            Logger.LogInfo($"{jsonWrapper}");
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            jsonWrapper.VotingLevels = new List<VotingLevel>();
            modStorage.SaveToJson("VotingLevels", jsonWrapper);
        }

        VotingLevel votingLevel = new VotingLevel
        {
            KickFinishTime = 0,
            ClutchFinishTime = 0,
            LevelUid = currentLevelUid,
            LevelName = currentLevelName
        };
        jsonWrapper.VotingLevels.Add(votingLevel);
        modStorage.SaveToJson("VotingLevels", jsonWrapper);
    }
}