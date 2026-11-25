using System;
using System.Runtime.ConstrainedExecution;
using Home.Common;
using Server.Shared.Messages;
using Server.Shared.State.Chat;
using Services;

public class ModLogger
{
    private static bool shownErrorToUser = false;

    public static void Log(object message)
    {
        Log(message.ToString());
    }

    public static void Log(string message)
    {
        Console.Write($"[FactionVisits] {message}");
    }

    public static void LogErrorToUserOnce(string message)
    {
        Log(message);
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
