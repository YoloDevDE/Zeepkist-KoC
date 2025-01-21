using System;
using KoC.models;
using ZeepkistClient;

namespace KoC.utils;

public class ChatUtils
{
    public static void AddNewChatMessage(ZeepkistChatMessage message)
    {
        ZeepkistNetwork.ChatMessages.Add(message);
        if (ZeepkistNetwork.ChatMessages.Count > 20)
        {
            ZeepkistNetwork.ChatMessages.RemoveAt(0);
        }

        Action<ZeepkistChatMessage> chatMessageReceived = ZeepkistNetwork.ChatMessageReceived;
        if (chatMessageReceived != null)
        {
            chatMessageReceived(message);
        }
    }

    public static string UpdateVotingResultsMessage(int votesKick, int votesClutch, SubmissionLevel submissionLevel = null)
    {
        if (submissionLevel == null)
        {
            submissionLevel = new SubmissionLevel(
                123456789,
                "DefaultLevelUid",
                "Test Level",
                "Test Author"
            );
        }

        int totalVotes = votesKick + votesClutch;

        double clutchRatio = totalVotes > 0 ? (double)votesClutch / totalVotes : 0.5;
        double kickRatio = 1.0 - clutchRatio;
        string clutchColorHex = "#00ff00";
        string kickColorHex = "#ff0000";
        int ratioMeterLength = 31;

        int centerPosition = ratioMeterLength / 2;
        string kickText = "Kick";
        string kickVotes = votesKick.ToString();
        string clutchVotes = votesClutch.ToString();
        string clutchText = "Clutch";
        int totalLength = kickText.Length + kickVotes.Length + clutchText.Length + clutchVotes.Length + 3; // +3 for the spaces and '|'

        int paddingLeftForKick = Math.Max(0, centerPosition - (kickText.Length + kickVotes.Length));
        int paddingRightForClutch = Math.Max(0, ratioMeterLength - totalLength - paddingLeftForKick);

        string clutchPercentageStr = $"{clutchRatio * 100:0}%";
        string kickPercentageStr = $"{kickRatio * 100:0}%";


        string kocText = $"<color=#ff8800>{kickText.PadRight(centerPosition - 1)}<color=#4444ff>{"or".PadRight(centerPosition - clutchText.Length)}</color>{clutchText}</color>";
        string kocTextPercentage = $"{kickPercentageStr.PadRight(centerPosition)}|{clutchPercentageStr.PadLeft(ratioMeterLength - centerPosition)}";
        string ratioMeterString = new string('.', ratioMeterLength);
        string paddingLeft = "".PadLeft((int)(kickRatio * ratioMeterLength));
        string indicatorLine = "|".PadLeft((int)Math.Ceiling(kickRatio * ratioMeterLength));
        string kickColorMark = $"<mark color={kickColorHex}88>" + new string('.', (int)Math.Ceiling(kickRatio * ratioMeterLength)) + "</mark>";
        string clutchColorMark = paddingLeft + $"<mark color={clutchColorHex}88>" + new string('.', (int)Math.Ceiling(clutchRatio * ratioMeterLength)) + "</mark>";
        string measureSticks = "|".PadRight((int)(ratioMeterLength * 0.5)) + "|".PadRight((int)(ratioMeterLength * 0.5)) + "|";


        return $"/servermessage white 0 <align=\"left\"><margin-left=\"50%\"><size=\"25%\"><br>" +
               $"<line-height=50%><u><b>{submissionLevel.Name}</b> <#ffffff>by <#ff9900>{submissionLevel.Author}</u><br><br><#ffffff>" +
               $"<line-height=95%><#ffffff><b><u>{kocText}</u></b><br>" +
               $"<pos=0>{ratioMeterString}" +
               $"<pos=0>{measureSticks}" +
               $"<pos=0>{kickColorMark}" +
               $"<pos=0>{clutchColorMark}" +
               $"<pos=0>{indicatorLine}<br>" +
               $"<pos=0><color={kickColorHex}>{kickVotes}{kickPercentageStr.PadLeft(centerPosition - kickVotes.Length)}</color>|<color={clutchColorHex}>{clutchPercentageStr.PadRight(centerPosition - clutchVotes.Length)}{clutchVotes}</color><br>";
    }
}