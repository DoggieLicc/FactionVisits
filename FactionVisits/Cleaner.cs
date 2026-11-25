using BetterTOS2;
using Game.Simulation;
using HarmonyLib;
using Server.Shared.Info;
using Server.Shared.State;
using Services;

namespace FactionVisits;

[HarmonyPatch(typeof(GameObservations))]
internal class Cleaner
{
    static bool hooked = false;
    static bool clearedDayIcons = false;
    static bool clearedNightIcons = false;

    [HarmonyPatch("HandleGameInfo")]
    [HarmonyPostfix]
    static void ClearIcons(GameInfoObservation gameInfoObservation)
    {
        if (Pepper.IsGamePhasePlay() && !hooked)
        {
            hooked = true;
            ModLogger.Log("Adding hook");
            Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged +=
                Interpreter.HandleMessages;
        }
        else if (!Pepper.IsGamePhasePlay() && hooked)
        {
            hooked = false;
            ModLogger.Log("Removing hook");
            Service.Game.Sim.simulation.incomingChatLogMessage.OnChanged -=
                Interpreter.HandleMessages;
        }
        // Clear Day Icons as we enter Night Phase
        if (Pepper.IsGamePhasePlay() && Pepper.IsNight())
        {
            // tos2 retriggers this if someone dcs at night, make sure to not clear our night icons
            if (!clearedDayIcons)
            {
                ModLogger.Log(
                    $"Requesting icons clear because of playphase: "
                        + gameInfoObservation.Data.playPhase
                );
                Manager.Instance.Clear();
                clearedDayIcons = true;
            }
            clearedNightIcons = false;
        }
        // Clear Night Icons as we enter Day Phase
        if (Pepper.IsGamePhasePlay() && Pepper.GetCurrentPlayPhase() == PlayPhase.NIGHT_WRAP_UP)
        {
            //just in case
            if (!clearedNightIcons)
            {
                ModLogger.Log(
                    $"Requesting icons clear because of playphase: "
                        + gameInfoObservation.Data.playPhase
                );
                Manager.Instance.Clear();
                clearedNightIcons = true;
            }
            clearedDayIcons = false;
            Manager.Instance.overchargedTeammate = -1;
        }
        if (Pepper.IsGamePhasePlay() && Pepper.GetCurrentPlayPhase() == PlayPhase.FIRST_DISCUSSION)
        {
            Manager.Instance.Reset();
            Manager.Instance.SetHandleOvercharged();
            Interpreter.Reset();
            ModLogger.Log("Do we handle overcharges?: " + Manager.Instance.handleOvercharged);
            ModLogger.Log("Are we modded?: " + Manager.IsModded());
            if (Manager.IsModded())
            {
                FactionType myFaction = Pepper.GetMyFaction();
                Role myRole = Pepper.GetMyRole();
                if (myFaction == FactionTypePlus.JACKAL && myRole != RolePlus.JACKAL)
                    Manager.Instance.amIRecruited = true;
                ModLogger.Log("Are we recruited?: " + Manager.Instance.amIRecruited);
            }
        }
    }
}
