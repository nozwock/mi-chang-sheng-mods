using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
// using KBEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BetterNpcInfo;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public const string PLUGIN_GUID = "EtheralCat.BetterNpcInfo";
	public const string PLUGIN_NAME = "Better Npc Info";
	public const string PLUGIN_VERSION = "1.0.0";

	static Harmony harmony;

	void Start()
	{
		harmony = new Harmony(PLUGIN_GUID);
		harmony.PatchAll();

        // Learned about game having a message system from here
        // https://github.com/Ventulus-lab/MCS/blob/master/MoreNPCInfo/MoreNPCInfo/Class1.cs
        MessageMag.Instance.Register(MessageName.MSG_GameInitFinish, new Action<MessageData>(OnGameInit));
	}

	void OnDestroy()
	{
		harmony?.UnpatchSelf();
		harmony = null;
	}

    void OnGameInit(MessageData msg = null)
    {
        StartCoroutine(ModifyUINPCInfoPanelCoroutine());
    }

    IEnumerator ModifyUINPCInfoPanelCoroutine()
    {
        var npcInfoPanel = UINPCJiaoHu.Inst.InfoPanel;

        var npcShow = npcInfoPanel.transform.Find("NPCShow");

        // Create NpcId text object
        var title = npcInfoPanel.transform.Find("ShuXing").Find("Title").GetComponentInChildren<Text>().transform;
        var npcId = UnityEngine.Object.Instantiate<GameObject>(title.gameObject, npcShow).transform;
        npcId.name = "NpcId";
        npcId.localPosition = new Vector3(-280, 320, 0);

        // Npc realm text
        npcInfoPanel.XiuWei.horizontalOverflow = HorizontalWrapMode.Wrap;

        yield return null;
    }

    static string NpcIdStr(int id)
    {
        id = NPCEx.NPCIDToNew(id);
        var oldId = NPCEx.NPCIDToOld(id);
        var str = id >= 20000 ? id.ToString() : string.Empty;
        if (oldId < 20000)
            str += $" ({oldId})";
        return str;
    }

    [HarmonyPatch(typeof(UINPCInfoPanel))]
    class Patch_UINPCInfoPanel
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UINPCInfoPanel.SetNPCInfo))]
        static void SetNPCInfo_Postfix(UINPCInfoPanel __instance)
        {
            UINPCData npc = __instance.npc;

            // Exp percentage
            var maxExp = jsonData.instance.LevelUpDataJsonData[npc.Level.ToString()]["MaxExp"].I;
            var percentage = (float)npc.Exp / (float)maxExp * 100f;
            __instance.XiuWei.text = $"({(int)percentage}%) {npc.LevelStr}";

            // Npc Id
            var npcShow = __instance.transform.Find("NPCShow");
            var npcId = npcShow.Find("NpcId").GetComponent<Text>();
            npcId.text = $"ID: {NpcIdStr(npc.ID)}";
        }
    }
}
