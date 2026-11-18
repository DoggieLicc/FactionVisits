using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Interface;
using Game.Simulation;
using Server.Shared.Info;
using Server.Shared.State;
using Services;
using SML;
using UnityEngine;
using UnityEngine.UI;

namespace FactionVisits;

internal class Manager
{
    internal Dictionary<int, List<Image>> visits = [];
    public bool handleOvercharged = false;
    public int overchargedTeammate = -1; //Can only handle 1 overcharged teammate at a time due to game limits
    public bool amIRecruited = false;
    TosAbilityPanel _panel = null;
    static Manager _instance = null;
    internal static Manager Instance
    {
        get
        {
            _instance ??= new Manager();
            return _instance;
        }
    }
    internal TosAbilityPanel Panel
    {
        get
        {
            if (_panel == null)
            {
                foreach (
                    TosAbilityPanel pan in UnityEngine.Object.FindObjectsOfType<TosAbilityPanel>()
                )
                {
                    if (pan.playerListPlayers.Count > 0)
                    {
                        _panel = pan;
                        visits = [];
                        for (int i = 0; i < pan.playerListPlayers.Count; i++)
                        {
                            visits.Add(i, []);
                        }
                        break;
                    }
                }
            }
            return _panel;
        }
    }

    internal void Reset()
    {
        handleOvercharged = false;
        overchargedTeammate = -1;
        amIRecruited = false;
        _panel = null;
        visits = [];
    }

    internal bool CanOverchargeHappen()
    {
        if (ModSettings.GetString("Handle Overcharged") == "Never")
            return false;
        List<Role> modifiers = Service.Game.Sim.simulation.roleDeckBuilder.Data.modifierCards;
        if (
            (!IsModded() && modifiers.Contains(Role.ALL_OUTLIERS))
            || (IsModded() && modifiers.Contains((Role)231))
        )
            return true; //Role 231 is All Outliers in BTOS2
        List<RoleDeckSlot> roleDeckSlots = Service
            .Game
            .Sim
            .simulation
            .roleDeckBuilder
            .Data
            .roleDeckSlots;
        if (roleDeckSlots.Count == 0)
            return true; //Replays dont show any slots sometimes, assume true if so
        foreach (RoleDeckSlot roleDeckSlot in roleDeckSlots)
        {
            if (
                !IsModded()
                && (roleDeckSlot.Role1 == Role.CATALYST || roleDeckSlot.Role2 == Role.CATALYST)
            )
                return true;
            if (IsModded() && (roleDeckSlot.Role1 == (Role)63 || roleDeckSlot.Role2 == (Role)63))
                return true; //Role 63 is BTOS2 Catalyst
        }
        return false;
    }

    internal void SetHandleOvercharged()
    {
        handleOvercharged = CanOverchargeHappen();
    }

    public static bool IsModded()
    {
        if (Settings.betterTos == null || !ModStates.IsEnabled("curtis.tuba.better.tos2"))
            return false;
        Type btosInfo = Settings.betterTos.GetType("BetterTOS2.BTOSInfo");
        return (bool)
            btosInfo
                .GetField("IS_MODDED", BindingFlags.Static | BindingFlags.Public)
                .GetValue(null);
    }

    public static bool ShowOwnActions()
    {
        switch (ModSettings.GetString("Show Own Actions"))
        {
            default:
            case "Never":
                return false;
            case "Only as Factional Evil":
                GameSimulation gameSimulation = Service.Game.Sim.simulation;
                PlayerIdentityData myIdentity = (PlayerIdentityData)gameSimulation.myIdentity;
                FactionType faction = myIdentity.faction;
                if (!Interpreter.factionsWithChat.Contains(faction))
                {
                    return false;
                }
                if (Manager.IsModded() && myIdentity.role == (Role)55)
                    return false; //Role 55 is BTOS2 Jackal
                return true;
            case "Always":
                return true;
        }
    }

    internal void AddTarget(
        MenuChoiceType abilityId,
        int targetPlayer,
        Sprite sprite,
        object role,
        int actorPlayer
    )
    {
        //Adds the sprite to a list with a special name to mark it aparta by player, role and ability
        TosAbilityPanelListItem tagetPlayerPanel = Panel.playerListPlayers[targetPlayer];
        string targetName = GetRoleName(abilityId, role, actorPlayer);
        try
        {
            Image image = UnityEngine.Object.Instantiate(
                Panel.playerListPlayers[targetPlayer].effectImage2
            );
            image.gameObject.name = targetName;
            image.name = targetName;
            if (Panel.playerListPlayers[targetPlayer].roleIconButton.isActiveAndEnabled)
            {
                image.transform.SetParent(tagetPlayerPanel.roleIconButton.transform);
            }
            else
            {
                image.transform.SetParent(tagetPlayerPanel.playerNameButton.transform);
            }
            Console.WriteLine("FactionVisits adding icon " + image.name);
            image.transform.localScale = Vector3.one;
            image.sprite = sprite;
            visits[targetPlayer].Add(image);
            image.transform.localPosition = new Vector3(
                80 + 32 * (visits[targetPlayer].Count - 1),
                0,
                0
            );
            image.gameObject.SetActive(true);
        }
        catch (Exception e)
        {
            Console.WriteLine("FactionVisits Error adding icon " + targetName);
            Console.WriteLine("FactionVisits Error! " + e.Message);
            Console.WriteLine("FactionVisits ErrorSource: " + e.Source);
            Console.WriteLine("FactionVisits ErrorTrace: --\n" + e.StackTrace);
        }
    }

    internal int GetTarget(MenuChoiceType abilityId, object role, int actorPlayer)
    {
        int counter = 0;
        string roleName = GetRoleName(abilityId, role, actorPlayer);
        Console.WriteLine("FactionVisits get target: " + roleName);
        foreach (List<Image> imgs in visits.Values)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                try
                {
                    if (imgs[i].gameObject.name == roleName)
                        return counter;
                }
                catch
                {
                    Console.WriteLine($"FactionVisits ignoring error when getting {roleName}");
                }
            }
            counter++;
        }
        return -1;
    }

    internal int TargetsCount(MenuChoiceType abilityId, object role, int actorPlayer)
    {
        int counter = 0;
        string roleName = GetRoleName(abilityId, role, actorPlayer);
        Console.WriteLine("FactionVisits count target: " + roleName);
        foreach (List<Image> imgs in visits.Values)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                try
                {
                    if (imgs[i].gameObject.name == roleName)
                        counter++;
                }
                catch
                {
                    Console.WriteLine($"FactionVisits ignoring error when counting {roleName}");
                }
            }
        }
        return counter;
    }

    internal void CancelTarget(MenuChoiceType abilityId, object role, int actorPlayer)
    {
        //Removes the requested sprite from the list of sprites
        bool removed = false;
        string roleName = GetRoleName(abilityId, role, actorPlayer);
        Console.WriteLine("FactionVisits removal target: " + roleName);
        foreach (List<Image> imgs in visits.Values)
        {
            for (int i = 0; i < imgs.Count; i++)
            {
                try
                {
                    if (imgs[i].gameObject.name.Contains(roleName))
                    {
                        Image temp = imgs[i];
                        Console.WriteLine(
                            "FactionVisits removing "
                                + temp.gameObject.name
                                + " because of target change or cancel"
                        );
                        imgs.RemoveAt(i);
                        UnityEngine.Object.DestroyImmediate(temp);
                        removed = true;
                    }
                    if (removed && i < imgs.Count)
                    {
                        for (int j = i; j < imgs.Count; j++)
                        {
                            imgs[j].transform.localPosition -= new Vector3(32, 0, 0);
                        }
                        i--;
                    }
                    removed = false;
                }
                catch
                {
                    Console.WriteLine($"FactionVisits ignoring error when removing {roleName}");
                }
            }
        }
    }

    internal void ChangeTarget(
        MenuChoiceType abilityId,
        int targetPlayer,
        Sprite sprite,
        Role role,
        int actorPlayer
    )
    {
        //First removes all relevant sprites, then adds any relevant sprites to the list
        Console.WriteLine("FactionVisits requesting cancels for the change of target");
        switch (role)
        {
            case Role.BODYGUARD:
            case Role.CLERIC:
            case Role.TRICKSTER:
            case Role.BAKER:
            case Role.ARSONIST:
            case Role.POTIONMASTER:
            case Role.RITUALIST:
            case Role.VOODOOMASTER:
                CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                break;
            case Role.RETRIBUTIONIST:
            case Role.NECROMANCER:
                if (
                    abilityId == MenuChoiceType.NightAbility
                    || (abilityId == MenuChoiceType.SpecialAbility && targetPlayer == -1)
                )
                {
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                }
                else
                {
                    CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                    CancelTarget(abilityId, role, actorPlayer);
                }
                break;
            case (Role)58: //Auditor in BTOS2, Covenite in Vanilla
            case (Role)59: //Inquisitor in BTOS2, Catalyst in Vanilla
            case (Role)62: //Warlock in BTOS2
            case (Role)65: //Socialite in BTOS2
            case Role.DREAMWEAVER:
            case Role.JAILOR:
            case Role.SOCIALITE:
            case Role.MONARCH:
            case Role.PIRATE:
            case Role.POISONER:
            case Role.MEDUSA:
            case Role.VAMPIRE:
            case Role.WEREWOLF:
                CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                break;
            case Role.ORACLE:
            case (Role)61: //Oracle in BTOS2
            case Role.VETERAN:
            case Role.ILLUSIONIST:
            case Role.COVENLEADER:
                CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                break;
            case Role.DOOMSAYER:
                if (TargetsCount(MenuChoiceType.SpecialAbility, role, actorPlayer) >= 3)
                {
                    CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                }
                break;
            case (Role)66: //BTOS2 Pacifist
                if (!IsModded())
                    return;
                CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                for (int i = 0; i < _panel.playerListPlayers.Count; i++)
                {
                    CancelTarget(MenuChoiceType.NightAbility2, "PACIFIST-" + i, actorPlayer);
                }
                break;
            case Role.SEER:
                if (IsModded())
                {
                    if (abilityId == MenuChoiceType.NightAbility)
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                        CancelTarget(MenuChoiceType.SpecialAbility, role, actorPlayer);
                    }
                    else
                    {
                        CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                        CancelTarget(abilityId, role, actorPlayer);
                    }
                }
                else
                {
                    CancelTarget(abilityId, role, actorPlayer);
                }
                break;
            case Role.SERIALKILLER:
            case Role.SHROUD:
                if (abilityId == MenuChoiceType.SpecialAbility)
                    return;
                CancelTarget(MenuChoiceType.NightAbility, role, actorPlayer);
                CancelTarget(MenuChoiceType.NightAbility2, role, actorPlayer);
                break;
            default:
                CancelTarget(abilityId, role, actorPlayer);
                break;
        }
        if (targetPlayer == -1 || sprite == null)
            return;
        Console.WriteLine("FactionVisits adding icon to new target");
        AddTarget(abilityId, targetPlayer, sprite, role, actorPlayer);
    }

    internal string GetRoleName(MenuChoiceType abilityId, object role, int actorPlayer)
    {
        string roleName = $"{role}({actorPlayer})";
        if (role == null)
            roleName = $"({actorPlayer})";
        if (abilityId == MenuChoiceType.NightAbility)
        {
            roleName += "1";
        }
        if (abilityId == MenuChoiceType.NightAbility2)
        {
            roleName += "2";
        }
        else if (abilityId == MenuChoiceType.SpecialAbility)
        {
            roleName += "S";
        }
        if (actorPlayer == overchargedTeammate)
        {
            roleName += "C";
        }
        return roleName;
    }

    internal void Clear()
    {
        //End of night full sprite clear
        foreach (List<Image> imgs in visits.Values)
        {
            for (int i = imgs.Count - 1; i >= 0; i--)
            {
                Image temp = imgs[i];
                imgs.RemoveAt(i);
                if (temp != null && temp.gameObject != null)
                {
                    Console.WriteLine("TOSTIV deleting icon " + temp.gameObject.name);
                    temp.gameObject.SetActive(true);
                    UnityEngine.Object.DestroyImmediate(temp.gameObject);
                }
            }
        }
        Interpreter.summonTargets.Clear();
    }

    internal static Sprite GetSprite(
        UIRoleData.UIRoleDataInstance instance,
        FactionType faction,
        int ability = 0
    )
    {
        Sprite sprite;
        if (ModStates.IsEnabled("alchlcsystm.fancy.ui") && Settings.fancyUI != null)
        {
            try
            {
                //Get sprite from Fancy UI if found
                sprite = GetFancyUISprite(instance.role, faction, ability);
                if (
                    sprite
                    != (
                        (Sprite)
                            Settings
                                .fancyUI.GetType("FancyUI.Assets.FancyAssetManager")
                                .GetProperty("Blank", BindingFlags.Static | BindingFlags.Public)
                                .GetValue(null)
                    )
                )
                    return sprite;
                Console.WriteLine(
                    $"FactionVisits no FancyUI sprite found for {instance.role}({ability})[{faction}], gonna retry with base faction"
                );
                sprite = GetFancyUISprite(instance.role, null, ability);
                if (
                    sprite
                    != (
                        (Sprite)
                            Settings
                                .fancyUI.GetType("FancyUI.Assets.FancyAssetManager")
                                .GetProperty("Blank", BindingFlags.Static | BindingFlags.Public)
                                .GetValue(null)
                    )
                )
                    return sprite;
                Console.WriteLine(
                    $"FactionVisits no FancyUI sprite found for {instance.role}({ability}), using vanilla sprite."
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"!!!!! FactionVisits got unexpected error when getting FancyUI sprite!!! !!!!!"
                );
                Console.WriteLine(
                    $"!!!!! FactionVisits Falling back to vanilla sprite, but get doggie to fix this. !!!!!"
                );
                Console.WriteLine("FactionVisits Error! " + e.Message);
                Console.WriteLine("FactionVisitsFancy ErrorSource: " + e.Source);
                Console.WriteLine("FactionVisitsFancy ErrorTrace: --\n" + e.StackTrace);
            }
        }
        //Get vannila icons
        sprite = ability switch
        {
            1 => instance.abilityIcon,
            2 => instance.abilityIcon2,
            3 => instance.specialAbilityIcon,
            _ => instance.roleIcon,
        };
        return sprite;
    }

    private static Sprite GetFancyUISprite(Role role, FactionType? faction, int ability)
    {
        //Load types
        Type fancyuiassman = Settings.fancyUI.GetType("FancyUI.Assets.FancyAssetManager");
        Type facnyuiutils = Settings.fancyUI.GetType("FancyUI.Utils");
        //Get role name
        string RoleName = (string)
            facnyuiutils
                .GetMethod("RoleName", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, [role, null]);
        //Add ability to name if needed
        switch (ability)
        {
            default:
                break;
            case 1:
                if (role == Role.JAILOR)
                    RoleName += "_Ability_2";
                else
                    RoleName += "_Ability_1";
                break;
            case 2:
                RoleName += "_Ability_2";
                break;
            case 3:
                RoleName += "_Special";
                break;
        }
        //Get faction name
        faction ??= (FactionType)
            facnyuiutils
                .GetMethod("GetFactionType", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, [role, null]);
        string FactionName = (string)
            facnyuiutils
                .GetMethod(
                    "FactionName",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    [
                        typeof(FactionType),
                        Settings.fancyUI.GetType("FancyUI.GameModType"),
                        typeof(bool),
                        typeof(bool),
                        typeof(bool),
                    ],
                    null
                )
                .Invoke(null, [faction, null, true, false, true]);
        //Get sprite
        Sprite sprite = (Sprite)
            fancyuiassman
                .GetMethod(
                    "GetSprite",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    [typeof(string), typeof(bool), typeof(string), typeof(string), typeof(bool)],
                    null
                )
                .Invoke(null, [RoleName, true, FactionName, null, false]);
        //Make a 2nd check for ability 1 due to 2 options of file name
        if (
            ability == 1
            && sprite
                == (
                    (Sprite)
                        Settings
                            .fancyUI.GetType("FancyUI.Assets.FancyAssetManager")
                            .GetProperty("Blank", BindingFlags.Static | BindingFlags.Public)
                            .GetValue(null)
                )
        )
        {
            RoleName = RoleName[..RoleName.IndexOf("_1")];
            sprite = (Sprite)
                fancyuiassman
                    .GetMethod(
                        "GetSprite",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        [
                            typeof(string),
                            typeof(bool),
                            typeof(string),
                            typeof(string),
                            typeof(bool),
                        ],
                        null
                    )
                    .Invoke(null, [RoleName, true, FactionName, null, false]);
        }
        return sprite;
    }
}
