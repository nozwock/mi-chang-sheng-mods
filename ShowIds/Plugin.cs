using System;
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
using JSONClass;

namespace ShowIds;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public const string PLUGIN_GUID = "EtheralCat.ShowIds";
	public const string PLUGIN_NAME = "Show Ids";
	public const string PLUGIN_VERSION = "1.0.0";

	static Harmony harmony;

	// (type, skillId)
	// type is also needed because skillId isn't unique between passive and active skills
	static Dictionary<(int, int), int> skillIdToBookId = new();

	void Start()
	{
		harmony = new Harmony(PLUGIN_GUID);
		harmony.PatchAll();

		MessageMag.Instance.Register(MessageName.MSG_GameInitFinish, new Action<MessageData>(OnGameInit));
	}

	void OnDestroy()
	{
		harmony?.UnpatchSelf();
		harmony = null;
	}

	void OnGameInit(MessageData msg = null)
	{
		foreach (var it in _ItemJsonData.DataDict)
		{
			var item = it.Value;
			if (item.type == 3 || item.type == 4) // Skill/Cultivation
			{
				var skillId = Mathf.RoundToInt(float.Parse(item.desc));
				skillIdToBookId[(item.type, skillId)] = item.id % jsonData.QingJiaoItemIDSegment;
			}
		}
	}

	static IEnumerable<MethodBase> GetMethodsWithOverrides(Type type, string method)
	{
		static string MethodId(MethodInfo m)
		{
			return $"{m.Module.ScopeName}.{m.MetadataToken}";
		}

		var seen = new HashSet<string>();
		var m = AccessTools.Method(type, method);
		if (m != null && !m.IsAbstract && seen.Add(MethodId(m)))
		{
			yield return m;
		}

		foreach (var subtype in type.Assembly.GetTypes())
		{
			if (subtype.IsSubclassOf(type))
			{
				m = AccessTools.Method(subtype, method);
				if (m != null && !m.IsAbstract && seen.Add(MethodId(m)))
					yield return m;
			}
		}
	}

	[HarmonyDebug]
	[HarmonyPatch]
	class Patch_BaseItem
	{
		// Patch BaseItem.GetDesc1 and all overrides in subclasses
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var item in GetMethodsWithOverrides(typeof(Bag.BaseItem), nameof(Bag.BaseItem.GetDesc1)))
			{
				yield return item;
			}
		}

		[HarmonyPostfix]
		static void GetDesc1_Postfix(Bag.BaseItem __instance, ref string __result)
		{
			var idText = $"Item ID: {__instance.Id}";
			if (!__result.StartsWith(idText))
				__result = $"{idText}\n{__result}";
		}
	}

	[HarmonyDebug]
	[HarmonyPatch]
	class Patch_BaseSkill
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var item in GetMethodsWithOverrides(typeof(Bag.BaseSkill), nameof(Bag.BaseSkill.GetDesc1)))
			{
				yield return item;
			}
		}

		[HarmonyPostfix]
		static void GetDesc1_Postfix(Bag.BaseSkill __instance, ref string __result)
		{
			var type = __instance.CanPutSlotType == CanSlotType.功法 ? 4 : 3; // 4-Cultivation, 3-Skill
			var idText = $"Skill ID: {__instance.SkillId}";
			if (skillIdToBookId.TryGetValue((type, __instance.SkillId), out var bookId))
			{
				idText += $" Item ID: {bookId}";
			}
			if (!__result.StartsWith(idText))
				__result = $"{idText}\n{__result}";
		}
	}

	// [HarmonyPatch]
	// class Patch_ToolTipMag
	// {
	// 	static IEnumerable<MethodBase> TargetMethods()
	// 	{
	// 		var type = typeof(ToolTipsMag);
	// 		var method = nameof(ToolTipsMag.Show);

	// 		yield return AccessTools.Method(type, method, [typeof(Bag.BaseItem)]);
	// 		yield return AccessTools.Method(type, method, [typeof(Bag.BaseItem), typeof(Vector2)]);
	// 	}

	// 	[HarmonyDebug]
	// 	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	// 	{
	// 		var codes = new List<CodeInstruction>(instructions);

	// 		// Instead, inject after the first `Desc1.SetText(...)`

	// 		var updateSize = AccessTools.Method(typeof(ToolTipsMag), nameof(ToolTipsMag.UpdateSize));
	// 		var setPosition = AccessTools.Method(typeof(ToolTipsMag), "SetPosition");

	// 		var injected = false;

	// 		for (int i = 0; i < codes.Count; i++)
	// 		{
	// 			// Insert before call to UpdateSize|SetPosition + ldarg for it
	// 			if (
	// 				!injected
	// 				&& i + 1 < codes.Count
	// 				&& (codes[i + 1].Calls(setPosition) || codes[i + 1].Calls(updateSize))
	// 			)
	// 			{
	// 				yield return new CodeInstruction(OpCodes.Ldarg_0); // __instance
	// 				yield return new CodeInstruction(OpCodes.Ldarg_1); // baseItem
	// 				yield return CodeInstruction.Call(
	// 					typeof(Patch_ToolTipMag),
	// 					nameof(Show_BeforeLayoutUpdate)
	// 				);
	// 				injected = true;
	// 			}

	// 			yield return codes[i];
	// 		}
	// 	}

	// 	static void Show_BeforeLayoutUpdate(ToolTipsMag __instance, Bag.BaseItem baseItem)
	// 	{
	// 		var desc = __instance.Desc1;
	// 		desc.SetText($"Item ID: {baseItem.Id}\n{desc.text}");
	// 	}

	// 	// [HarmonyPostfix]
	// 	// static void Show_Postfix(ToolTipsMag __instance, Bag.BaseItem baseItem, Text ___Desc1)
	// 	// {
	// 	// 	static IEnumerator DelayedResize(ToolTipsMag inst)
	// 	// 	{
	// 	// 		yield return null; // wait one frame
	// 	// 		inst.UpdateSize();
	// 	// 	}

	// 	// 	___Desc1.SetText($"Item ID: {baseItem.Id}\n{___Desc1.text}");
	// 	// 	__instance.StartCoroutine(DelayedResize(__instance));
	// 	// }
	// }
}
