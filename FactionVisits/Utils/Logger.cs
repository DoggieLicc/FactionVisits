using System;
using Home.Common;
using Server.Shared.Messages;
using Server.Shared.State.Chat;
using Services;
using UnityEngine;

public class ModLogger
{
    private static bool shownErrorToUser = false;

    public static void Log(object message)
    {
        Log(message.ToString());
    }

    public static void Log(string message)
    {
        Debug.Log($"[FactionVisits] {message}");
    }

    public static void LogErrorToUserOnce(string message)
    {
        Debug.Log($"[FactionVisits] {message}");
        if (!shownErrorToUser)
        {
            try
            {
                ChatLogMessage chatLogMessage = new(
                    new ChatLogCustomTextEntry()
                    {
                        customText = $"<color=\"red\">[FactionVisits] {message}</color>",
                        showInChat = true,
                        showInChatLog = false,
                        style = "error",
                    }
                );
                Service.Game.Sim.simulation.HandleChatLog(chatLogMessage);
                shownErrorToUser = true;
                UIController.PlaySoundStatic("Audio/UI/Error.wav", false);
            }
            catch { }
        }
    }
}
