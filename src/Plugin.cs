using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KoC.commands;
using KoC.etc;
using KoC.models;
using KoC.utils;
using Newtonsoft.Json;
using ZeepkistClient;
using ZeepSDK.ChatCommands;
using ZeepSDK.Messaging;
using ZeepSDK.Storage;

namespace KoC;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private float _clutchTime;
    private Harmony _harmony;
    private float _kickTime;
    private bool _waitingForClutchTime;

    private bool _waitingForKickTime;
    private int clutchVotes;

    private int kickVotes;

    public ITaggedMessenger Messenger { get; private set; }
    public states.KoC Machine { get; private set; }

    public static Plugin Instance { get; private set; }

    public ConfigEntry<bool> OnlyEligiblePlayersCanVote { get; set; }

    public string AutoMessage { get; set; } =
        "<color=#00ccff><b><u>Voting has started!</u></b></color>" +
        "<br><#ffcc00>MAPPER FINISH</color> - Reserved for map creators only :YannicSmile:" +
        "<br><#00ff00>CLUTCH FINISH</color> - Choose this if you enjoyed the map :sparkles:" +
        "<br><color=#ff0000>KICK FINISH</color> - Choose this if you didn't enjoy the map :skull:";

    public string ResultServerMessage { get; set; } = "%l by %a<br>%r";

    public string JoinMessageNormal { get; set; } =
        "Welcome to Kick or Clutch!<br>This session gets recorded and uploaded to Owls YouTube (youtube.com/@owlplague)<br>Make sure you behave in chat and subscribe to Owl :YannicSmile:";

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
        OnlyEligiblePlayersCanVote = Config.Bind("Voting",
            "Restricted Voting",
            true,
            new ConfigDescription(
                "If this is set to 'true' only players who were present in the previous submission map can vote."));
        RegisterCommands();
        RegisterEvents();
        Machine = new states.KoC();
    }

    // private void Update()
    // {
    //     bool pressed = false;
    //     // Check for key presses and update the votes accordingly
    //     if (Input.GetKeyDown(KeyCode.Keypad7))
    //     {
    //         clutchVotes++;
    //         pressed = true;
    //     }
    //     else if (Input.GetKeyDown(KeyCode.Keypad1))
    //     {
    //         clutchVotes = Math.Max(0, clutchVotes - 1); // Ensure clutch votes are not less than 0
    //         pressed = true;
    //     }
    //     else if (Input.GetKeyDown(KeyCode.Keypad9))
    //     {
    //         kickVotes++;
    //         pressed = true;
    //     }
    //     else if (Input.GetKeyDown(KeyCode.Keypad3))
    //     {
    //         kickVotes = Math.Max(0, kickVotes - 1); // Ensure kick votes are not less than 0
    //         pressed = true;
    //     }
    //
    //
    //     else if (Input.GetKeyDown(KeyCode.Keypad5))
    //     {
    //         kickVotes = 0;
    //         clutchVotes = 0;
    //         pressed = true;
    //     }
    //
    //     if (!pressed)
    //     {
    //         return;
    //     }
    //
    //     // Log or update results somewhere if needed. Example:
    //     Logger.LogInfo($"Kick Votes: {kickVotes}, Clutch Votes: {clutchVotes}");
    //
    //     // If you want to send updates to a result message or similar:
    //     ChatApi.SendMessage(ChatUtils.UpdateVotingResultsMessage(votesClutch: clutchVotes, votesKick: kickVotes));
    // }


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
        // Start listening for chat messages
        ZeepkistNetwork.ChatMessageReceived += OnChatMessageReceived;
        CommandCreateVotingLevel.OnHandle -= SaveCurrentLevelAsVotingLevelToJson;
        // Ask for kick time first
        _waitingForKickTime = true;
        ChatUtils.SendCustomChatMessage("Please enter the KICK finish time in seconds (e.g. '120' for 2 minutes):");
    }

    private void OnChatMessageReceived(ZeepkistChatMessage message)
    {
        if (message?.Player == null)
        {
            return;
        }

        if (!message.Player.IsLocal)
        {
            return;
        }

        string extractedMessage = Regex.Replace(message.Message.ToLowerInvariant(), @"[^\d" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "]", "");
        if (_waitingForKickTime)
        {
            if (float.TryParse(extractedMessage, out float kickTime) && kickTime > 0)
            {
                _kickTime = kickTime;
                _waitingForKickTime = false;
                _waitingForClutchTime = true;
                ChatUtils.SendCustomChatMessage("Please enter the CLUTCH finish time in seconds (e.g. '60' for 1 minute):");
            }
            else
            {
                ChatUtils.SendCustomChatMessage("Invalid time format. Please enter a number greater than 0 in seconds (e.g. '120'):");
            }
        }
        else if (_waitingForClutchTime)
        {
            if (float.TryParse(extractedMessage, out float clutchTime) && clutchTime > 0 && clutchTime < _kickTime)
            {
                _clutchTime = clutchTime;
                _waitingForClutchTime = false;

                // Unsubscribe from chat messages
                ZeepkistNetwork.ChatMessageReceived -= OnChatMessageReceived;
                CommandCreateVotingLevel.OnHandle += SaveCurrentLevelAsVotingLevelToJson;
                // Complete the saving process
                CompleteSavingProcess();
            }
            else
            {
                ChatUtils.SendCustomChatMessage($"Invalid time. Please enter a number between 0 and {_kickTime} seconds:");
            }
        }
    }

    private void CompleteSavingProcess()
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
            Messenger.LogError($"Failed to load voting levels: {e.Message}", 5f);
        }

        VotingLevel votingLevel = new VotingLevel
        {
            KickFinishTime = _kickTime,
            ClutchFinishTime = _clutchTime,
            LevelUid = currentLevelUid,
            LevelName = currentLevelName
        };

        jsonWrapper.VotingLevels.Add(votingLevel);
        modStorage.SaveToJson("VotingLevels", jsonWrapper);
        Messenger.LogSuccess($"Successfully registered '{currentLevelName}' as a voting level with Kick time: {_kickTime}s and Clutch time: {_clutchTime}s", 5f);
        Machine.VotingLevels = GetVotingLevels();
        // Reset the values
        _kickTime = 0;
        _clutchTime = 0;
    }
}