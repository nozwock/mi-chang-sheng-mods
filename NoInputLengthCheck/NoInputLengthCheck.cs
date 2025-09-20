using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Fungus;
using JSONClass;
using HarmonyLib.Tools;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace NoInputLengthCheck;

[BepInPlugin(PLUGIN_GUID, "NoInputLengthCheck", "1.0")]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "EtherealCat.NoInputLengthCheck";

    internal static ManualLogSource Log;

    static Harmony harmony;

    void Awake()
    {
        Log = base.Logger;

        harmony = new Harmony(PLUGIN_GUID);
        harmony.PatchAll(typeof(Plugin));

        foreach (var m in harmony.GetPatchedMethods())
        {
            Log.LogInfo($"Patched: {m?.DeclaringType}::{m?.Name}");
        }

        // try
        // {
        //     var methods = new MethodInfo[] {
        //         Get_UInputBox_Show_delegate(typeof(CmdSetDaoLvChengHu), nameof(CmdSetDaoLvChengHu.OpenInputBox)),
        //         Get_UInputBox_Show_delegate(typeof(CmdSetDongFuName), nameof(CmdSetDongFuName.OpenInputBox)),
        //         AccessTools.Method(typeof(MainUISetName), nameof(MainUISetName.NextMethod)),
        //         AccessTools.Method(typeof(CreateAvatarMag), nameof(CreateAvatarMag.startGameClick))
        //     };
        //     foreach (var m in methods)
        //     {
        //         Log.LogInfo($"Target method: {m?.DeclaringType}::{m?.Name}");
        //         harmony.Patch(m, transpiler: new HarmonyMethod(typeof(Plugin), nameof(RemoveLengthCheckTranspiler)));
        //     }
        // }
        // catch (Exception e)
        // {
        //     Log.LogError($"{e}");
        // }
    }

    void OnDestroy()
    {
        harmony?.UnpatchSelf();
        harmony = null;
    }

    // static MethodInfo Get_UInputBox_Show_delegate(Type type, string methodName) =>
    //     AccessTools.FirstMethod(type, m =>
    //         m.Name.Contains($"<{methodName}>b__")
    //         && m.GetParameters().Length == 1
    //         && m.GetParameters()[0].ParameterType == typeof(string)
    //     );

    // static IEnumerable<CodeInstruction> RemoveLengthCheckTranspiler(IEnumerable<CodeInstruction> instructions)
    // {
    //     var codes = new List<CodeInstruction>(instructions);

    //     for (int i = 0; i < codes.Count; i++)
    //     {
    //         // Look for: callvirt get_Length -> ldc.i4.* -> ble
    //         if (
    //             i + 2 < codes.Count
    //             && codes[i].opcode == OpCodes.Callvirt
    //             && codes[i].operand is System.Reflection.MethodInfo mi
    //             && mi.Name == "get_Length"
    //             // && (codes[i + 1].opcode == OpCodes.Ldc_I4_S || codes[i + 1].opcode == OpCodes.Ldc_I4)
    //             && codes[i + 2].opcode.FlowControl == FlowControl.Cond_Branch // ble, bgt, etc
    //             && codes[i + 2].operand is System.Reflection.Emit.Label label
    //         )
    //         {
    //             codes[i - 1] = new CodeInstruction(OpCodes.Nop);
    //             codes[i] = new CodeInstruction(OpCodes.Nop);
    //             codes[i + 1] = new CodeInstruction(OpCodes.Nop);
    //             codes[i + 2] = new CodeInstruction(OpCodes.Br_S, label); // skip conditional block
    //         }
    //     }
    //     return codes;
    // }

    // [HarmonyDebug]
    // [HarmonyPatch]
    // class Patch
    // {
    //     public static IEnumerable<MethodBase> TargetMethods()
    //     {
    //         MethodInfo[] methods = [];
    //         try
    //         {
    //             methods = [
    //                 Get_UInputBox_Show_delegate(typeof(CmdSetDongFuName), nameof(CmdSetDongFuName.OpenInputBox)),
    //                 Get_UInputBox_Show_delegate(typeof(CmdSetDaoLvChengHu), nameof(CmdSetDaoLvChengHu.OpenInputBox)),
    //                 AccessTools.Method(typeof(MainUISetName), nameof(MainUISetName.NextMethod)),
    //                 AccessTools.Method(typeof(CreateAvatarMag), nameof(CreateAvatarMag.startGameClick))
    //             ];
    //         }
    //         catch (Exception e)
    //         {
    //             Log.LogError($"{e}");
    //         }
    //         foreach (var m in methods)
    //             Log.LogInfo($"Target method: {m?.DeclaringType}::{m?.Name}");
    //         return methods.AsEnumerable();
    //     }

    //     [HarmonyDebug]
    //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         return RemoveLengthCheckTranspiler(instructions);
    //     }
    // }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CmdSetDaoLvChengHu), nameof(CmdSetDaoLvChengHu.OpenInputBox))]
    static bool Patch_CmdSetDaoLvChengHu_OpenInputBox_Prefix(CmdSetDaoLvChengHu __instance, IntegerVariable ___NPCID)
    {
        UInputBox.Show("设定称呼", delegate (string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                __instance.OpenInputBox();
            }
            // else if (s.Length > 6)
            // {
            // 	UIPopTip.Inst.Pop("称呼太长了");
            // 	OpenInputBox();
            // }
            else
            {
                PlayerEx.SetDaoLvChengHu(___NPCID.Value, s);
                __instance.Continue();
            }
        });

        return false; // skip original
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CmdSetDongFuName), nameof(CmdSetDongFuName.OpenInputBox))]
    static bool Patch_CmdSetDongFuName_OpenInputBox_Prefix(CmdSetDongFuName __instance, int ___dongFuID)
    {
        List<string> randomStrings = DongFuRandomNameJsonData.DataList.Select((DongFuRandomNameJsonData x) => x.Name).ToList();
        UInputBox.Show("为洞府命名", delegate (string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                __instance.OpenInputBox();
            }
            // else if (s.Length > 6)
            // {
            // 	UIPopTip.Inst.Pop("名字太长了");
            // 	OpenInputBox();
            // }
            else
            {
                DongFuManager.SetDongFuName(___dongFuID, s);
            }
        }, randomStrings, "dongfu");

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MainUISetName), nameof(MainUISetName.NextMethod))]
    static bool Patch_MainUISetName_NextMethod_Prefix(MainUISetName __instance, InputField ___xinInputField, InputField ___minInputField)
    {
        string text = ___xinInputField.text + ___minInputField.text;
        // if (text.Length > 10)
        // {
        // 	UIPopTip.Inst.Pop("名称字数过长");
        // 	return false;
        // }
        if (string.IsNullOrWhiteSpace(text))
        {
            UIPopTip.Inst.Pop("没有填写名字");
            return false;
        }
        if (!Tools.instance.CheckBadWord(text))
        {
            UIPopTip.Inst.Pop("名称不合法,请换个名称");
            return false;
        }
        MainUIPlayerInfo.inst.firstName = ___xinInputField.text;
        MainUIPlayerInfo.inst.lastName = ___minInputField.text;
        MainUIPlayerInfo.inst.playerName = text;
        if (jsonData.instance.AvatarRandomJsonData.HasField("1"))
        {
            jsonData.instance.AvatarRandomJsonData["1"].SetField("Name", text);
        }
        __instance.gameObject.SetActive(value: false);
        MainUIMag.inst.createAvatarPanel.setFacePanel.Init();
        MainUIMag.inst.createAvatarPanel.facePanel.SetActive(value: true);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreateAvatarMag), nameof(CreateAvatarMag.startGameClick))]
    static bool Patch_CreateAvatarMag_startGameClick_Prefix(CreateAvatarMag __instance, CreateAvatarMag.createAvatardelegate aa)
    {
        LevelSelectManager component = GameObject.Find("Main Menu/MainMenuCanvas").GetComponent<LevelSelectManager>();
        string text = component.getFirstName() + component.getLastName();
        // if (text.Length > 10)
        // {
        //     UIPopTip.Inst.Pop("名称字数过长");
        //     return false;
        // }
        if (!Tools.instance.CheckBadWord(text))
        {
            UIPopTip.Inst.Pop("名称不合法,请换个名称");
            return false;
        }
        __instance.Eventdel = aa;
        __instance.gameObject.SetActive(value: true);
        __instance.showSetFace();
        return false;
    }

}
