using BepInEx;
using System.Reflection.Emit;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnlockAllTalents;

// MainUITianFuCell.cs

// IsUnlockedSpecialEvent
// WriteStringSave("UnlockedSpecialEvent", ...);

[BepInPlugin("EtherealCat.UnlockAllTalents", "UnlockAllTalents", "1.0")]
public class UnlockAllTalentsPlugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    private void Start()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(UnlockAllTalentsPlugin), null);
        Logger.LogInfo("Plugin loaded");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainUITianFuCell), "Init")]
    public static void MainUITianFuCell_ForceEnable_Patch_Postfix(MainUITianFuCell __instance)
    {
        __instance.toggle.isDisable = false;
        __instance.toggle.OnValueChange();
        // Logger.LogInfo("Forcibly enabled toggle");
    }

    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(MainUIToggle), "SetDisable")]
    // public static void DebugToggle_Patch_Prefix()
    // {
    //     Logger.LogInfo("Called MainUIToggle::SetDisable()");
    // }
}