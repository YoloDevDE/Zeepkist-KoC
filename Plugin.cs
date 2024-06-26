using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KoC.Commands;
using KoC.Data;
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
    public ITaggedMessenger Messenger;
    public ManualLogSource MyLogger;
    public StateMachine Machine { get; private set; }

    public static Plugin Instance { get; private set; }
    public ConfigEntry<string> AutoMessage { get; set; }
    public ConfigEntry<string> ClutchMessage { get; set; }
    public ConfigEntry<string> KickMessage { get; set; }
    public ConfigEntry<string> ResultServerMessage { get; set; }
    public ConfigEntry<string> JoinMessageNormal { get; set; }
    public ConfigEntry<string> JoinMessageVoting { get; set; }

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        MyLogger = Logger;
    }

    private void Start()
    {
        Instance = this;


        Messenger = MessengerApi.CreateTaggedMessenger("KoC");
        RegisterCommands();
        InitConfigBindings();
        SyncVotingLevelsConfigWithJson();
        Machine = new StateMachine();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    private void InitConfigBindings()
    {
        AutoMessage = Config.Bind("Messages (Chat)", "Mapper Finish",
            "DO NOT TAKE THE MAPPER FINISH OR YOU WILL GET EJECTED TO THE MOON!");
        KickMessage = Config.Bind("Messages (Chat)", "On Kick", "Sorry %a - It is a Kick! cya o/");
        ClutchMessage = Config.Bind("Messages (Chat)", "On Clutch", "Congratulations %a - It is a Clutch! :party:");
        ResultServerMessage = Config.Bind("Messages (Server)", "Result", "%l by %a -> %r");
        JoinMessageNormal =
            Config.Bind("Messages (Server)", "Joinmessage", "Welcome to Kick or Clutch. Subscribe to Owl!");
        JoinMessageVoting = Config.Bind("Messages (Server)", "Joinmessage (Votinglevel)",
            "Welcome to Kick or Clutch. Subscribe to Owl! DO NOT USE THE MAPPER FINISH!");
    }


    private void RegisterCommands()
    {
        ChatCommandApi.RegisterLocalChatCommand<CommandStart>();
        ChatCommandApi.RegisterLocalChatCommand<CommandStop>();
        ChatCommandApi.RegisterLocalChatCommand<CommandVotingResult>();
        ChatCommandApi.RegisterLocalChatCommand<CommandCreateVotingLevel>();
        ChatCommandApi.RegisterLocalChatCommand<CommandRegisterSubmissionLevel>();


        CommandCreateVotingLevel.OnHandle += SaveCurrentLevelAsVotingLevelToJson;
    }

    public void SyncVotingLevelsConfigWithJson()
    {
        List<VotingLevel> votingLevels = GetVotingLevels();
        foreach (VotingLevel votingLevel in votingLevels)
        {
            //  string section, 
            //  string key, 
            //  T defaultValue, 
            //  ConfigDescription configDescription = null
            Config.Bind(votingLevel.LevelName, "Clutch", votingLevel.ClutchFinishTime, ConfigDescription.Empty);
            Config.Bind(votingLevel.LevelName, "Kick", votingLevel.KickFinishTime, ConfigDescription.Empty);
        }
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