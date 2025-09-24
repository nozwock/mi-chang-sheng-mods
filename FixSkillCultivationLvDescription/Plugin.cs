using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KBEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FixSkillCultivationLvDescription;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public const string PLUGIN_GUID = "EtheralCat.FixSkillCultivationLvDescription";
	public const string PLUGIN_NAME = "Fix Skill Cultivation Lv Description";
	public const string PLUGIN_VERSION = "1.0.0";

	static Harmony harmony;

	void Start()
	{
		harmony = new Harmony(PLUGIN_GUID);
		harmony.PatchAll();
	}

	void OnDestroy()
	{
		harmony?.UnpatchSelf();
		harmony = null;
	}

	[HarmonyDebug]
	[HarmonyPatch(typeof(UIBiGuanTuPoPanel))]
	class Patch_UIBiGuanTuPoPanel
	{
		[HarmonyPostfix]
        [HarmonyPatch(nameof(UIBiGuanTuPoPanel.OnPanelShow))]
		static void OnPanelShow_Postfix(UIBiGuanTuPoPanel __instance)
		{
            foreach (var txt in __instance.DescItem)
            {
                if (txt == null) continue;

                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.resizeTextForBestFit = false;
                txt.alignment = TextAnchor.MiddleLeft;
            }
		}
	}
}
