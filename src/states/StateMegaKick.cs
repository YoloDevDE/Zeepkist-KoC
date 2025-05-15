using System.Collections;
using Crosstales;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace KoC.states;

public class StateMegaKick(KoC koC) : BaseState(koC)
{
    private Coroutine megaKickCoroutine;

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        megaKickCoroutine = Plugin.Instance.StartCoroutine(MegaKickSequence());
    }

    private void OnLevelLoaded()
    {
        KoC.TransitionTo(new StateRegisterSubmission(KoC));
    }

    private IEnumerator MegaKickSequence()
    {
        bool foundMapper = ZeepkistNetwork.TryGetPlayer(KoC.SubmissionLevel.AuthorSteamId, out ZeepkistNetworkPlayer mapper);
        string mapperName = foundMapper ? $"<#{mapper.chatColor.CTToHexRGB()}>{mapper.GetTaggedUsername()}</color>" : $"<color=#FFA500>{KoC.SubmissionLevel.Author}</color>";

        // Initial mega kick message  
        for (int i = 2; i >= 0; i--)
        {
            ChatApi.SendMessage(
                $"/servermessage white 0 <align=left><size=-2><color=yellow><color=red><b>:skull: MEGA KICKED :skull:</b></color> initiated! :Yannicmegas:</b></color></size><br><size=-5>starting procedure in {i} seconds</size></align>");
            yield return new WaitForSeconds(1f);
        }

        // Countdown sequence
        for (int i = 5; i >= 0; i--)
        {
            ChatApi.SendMessage(
                $"/servermessage white 0 <align=left><size=-2><color=yellow><b>{mapperName}</b><br>You will be <color=red><b>:skull: MEGA KICKED :skull:</b></color> in <color=red>{i}</color> seconds...</color></size><br><size=-5></size></align>");
            yield return new WaitForSeconds(1f);
        }

        if (mapper != null)
        {
            ZeepkistNetwork.KickPlayer(mapper);
            Plugin.Instance.Messenger.LogSuccess("Mega Kick successful!");
        }
        else
        {
            Plugin.Instance.Messenger.LogError("No Mapper found for Mega Kick.");
        }

        // Final message before kick
        ChatApi.SendMessage("/servermessage white 0 <align=left><size=-2><color=red><b>:skull: MEGA KICKED! :skull:</b></color></size><br><size=-5>Waiting for Owl to skip...</size></align>");
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        if (megaKickCoroutine != null)
        {
            Plugin.Instance.StopCoroutine(megaKickCoroutine);
        }
    }
}