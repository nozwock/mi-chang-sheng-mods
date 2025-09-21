using BepInEx;
using System.Reflection.Emit;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using System;
using KBEngine;
using System.Collections.Generic;
using System.Linq;
using JSONClass;

namespace AutoSortSkills;

// SkillStaticDatebase

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "EtherealCat.AutoSortSkills";
    public const string PLUGIN_NAME = "Auto Sort Skills";
    public const string PLUGIN_VERSION = "1.0";

    public static new ManualLogSource Logger;

    static Dictionary<int, int> ActiveSkillTypeOrder;
    static Dictionary<int, int> PassiveSkillTypeOrder;

    enum PassiveSkillType
    {
        Metal = 0,
        Wood = 1,
        Water = 2,
        Fire = 3,
        Earth = 4,
        Qi = 5,
        Agility = 6,
        Unknown1 = 7,
        Sword = 8,
        Body = 9,
    }

    enum ActiveSkillType
    {
        Metal = 0,
        Wood = 1,
        Water = 2,
        Fire = 3,
        Earth = 4,
        Qi = 5,
        Sense = 6,
        Sword = 7,
        Array = 8,
        SecretArt = 9, // >=9
    }

    void Start()
    {
        Logger = base.Logger;

        var activeDefault = "0,1,2,3,4,5,6,7,8";  // Metal -> Array
        var passiveDefault = "0,1,2,3,4,5,6,7,8,9"; // Metal -> Body

        var activeOrderConfig = Config.Bind("Sorting", "ActiveSkillTypeOrder", activeDefault,
            "Comma-separated Active skill type IDs in desired sort order (e.g. Metal=0, Wood=1, Water=2, Fire=3, Earth=4, Qi=5, Sense=6, Sword=7, Array=8, and SecretArt>=9 is always last).");

        var passiveOrderConfig = Config.Bind("Sorting", "PassiveSkillTypeOrder", passiveDefault,
            "Comma-separated Passive skill type IDs in desired sort order (e.g. Metal=0, Wood=1, Water=2, Fire=3, Earth=4, Qi=5, Agility=6, ??=7, Sword=8, Body=9).");

        ActiveSkillTypeOrder = ParseSkillTypeOrder(activeOrderConfig.Value);
        PassiveSkillTypeOrder = ParseSkillTypeOrder(passiveOrderConfig.Value);

        Harmony.CreateAndPatchAll(typeof(Plugin), PLUGIN_GUID);
        Logger.LogInfo("Plugin loaded with Active skill order: " + activeOrderConfig.Value);
        Logger.LogInfo("Plugin loaded with Passive skill order: " + passiveOrderConfig.Value);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(YSNewSaveSystem), nameof(YSNewSaveSystem.LoadSave))]
    static void LoadSave_Patch_Postfix(StartGame __instance)
    {
        Logger.LogInfo("YSNewSaveSystem::LoadSave()");
        SortPassiveSkillList();
        SortActiveSkillList();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Avatar), nameof(Avatar.addHasStaticSkillList))]
    static void SortHasStaticSkillList_Patch_Postfix(Avatar __instance)
    {
        SortPassiveSkillList();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Avatar), nameof(Avatar.addHasSkillList))]
    static void SortHasSkillList_Patch_Postfix(Avatar __instance)
    {
        SortActiveSkillList();
    }

    Dictionary<int, int> ParseSkillTypeOrder(string csv)
    {
        var parts = csv.Split(',');
        var dict = new Dictionary<int, int>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i].Trim(), out int typeId))
            {
                dict[typeId] = i;
            }
        }
        return dict;
    }

    static int GetSortOrder(int typeId, Dictionary<int, int> skillTypeOrder)
    {
        return skillTypeOrder.TryGetValue(typeId, out int order) ? order : int.MaxValue;
    }

    static void SortPassiveSkillList()
    {
        var player = Tools.instance.getPlayer();
        if (player == null)
        {
            Logger.LogWarning("Player entity not found, cannot sort passive skills");
            return;
        }

        Logger.LogInfo("Sorting passive skills (hasStaticSkillList)");
        player.hasStaticSkillList.Sort((a, b) =>
        {
            var data1 = GetPassiveSkillData(a.itemId);
            var data2 = GetPassiveSkillData(b.itemId);
            // int data.id
            // int data.Skill_LV (Quality)
            // int data.AttackType (SkillType)

            // 1. By Skill_LV (quality), descending
            int cmp = data2.Skill_LV.CompareTo(data1.Skill_LV);
            if (cmp != 0) return cmp;

            // 2. By AttackType
            cmp = GetSortOrder(data1.AttackType, PassiveSkillTypeOrder)
                .CompareTo(GetSortOrder(data2.AttackType, PassiveSkillTypeOrder));
            if (cmp != 0) return cmp;

            // 3. By Skill_ID
            return data1.Skill_ID.CompareTo(data2.Skill_ID);
        });
    }

    static void SortActiveSkillList()
    {
        var player = Tools.instance.getPlayer();
        if (player == null)
        {
            Logger.LogWarning("Player entity not found, cannot sort active skills");
            return;
        }

        Logger.LogInfo("Sorting active skills (hasSkillList)");
        player.hasSkillList.Sort((a, b) =>
        {
            var data1 = GetActiveSkillData(a.itemId);
            var data2 = GetActiveSkillData(b.itemId);
            // int data.id
            // int data.Skill_LV (Quality)
            // List data.AttackType (SkillType)

            int cmp = data2.Skill_LV.CompareTo(data1.Skill_LV);
            if (cmp != 0) return cmp;

            int type1 = ResolveActiveSkillType(data1, out bool isSecret1);
            int type2 = ResolveActiveSkillType(data2, out bool isSecret2);

            if (isSecret1 != isSecret2)
            {
                return isSecret1 ? 1 : -1; // SecretArts come last
            }

            cmp = GetSortOrder(type1, ActiveSkillTypeOrder)
                .CompareTo(GetSortOrder(type2, ActiveSkillTypeOrder));
            if (cmp != 0) return cmp;

            return data1.Skill_ID.CompareTo(data2.Skill_ID);
        });
    }

    static int ResolveActiveSkillType(_skillJsonData data, out bool isSecretArt)
    {
        isSecretArt = false;

        if (data.AttackType != null && data.AttackType.Count == 1)
        {
            int type = data.AttackType[0];
            if (type >= (int)ActiveSkillType.SecretArt)
            {
                isSecretArt = true;
                return type;
            }
            return type;
        }

        // Multiple types: fallback to first digit of Skill_ID
        int id = data.Skill_ID;
        while (id >= 10)
        {
            id /= 10;
        }
        return id;
    }

    static StaticSkillJsonData GetPassiveSkillData(int skillId)
    {
        foreach (StaticSkillJsonData data in StaticSkillJsonData.DataList)
        {
            if (data.Skill_ID == skillId)
            {
                return data;
            }
        }

        throw new Exception("Invalid skill id");
    }

    static _skillJsonData GetActiveSkillData(int skillId)
    {
        foreach (_skillJsonData data in _skillJsonData.DataList)
        {
            if (data.Skill_ID == skillId)
            {
                return data;
            }
        }

        throw new Exception("Invalid skill id");
    }

    // [HarmonyPatch(typeof(Avatar), "SortItem")]
    // [HarmonyPostfix]
    // public static void LogSkillOnSort_Postfix(Avatar __instance)
    // {
    //     try
    //     {
    //         // No idea why DataDict doesn't work... function silently crashes
    //         Logger.LogInfo($"Passive Skills:");
    //         foreach (SkillItem _skill in __instance.hasStaticSkillList) {
    //             foreach (StaticSkillJsonData data in StaticSkillJsonData.DataList) {
    //                 if (data.Skill_ID == _skill.itemId) {
    //                     Logger.LogInfo($"{data.id}, {data.name}, AttackType: {data.AttackType}, Quality: {data.Skill_LV}");
    //                     break;
    //                 }
    //             }
    //         }
    //
    //         Logger.LogInfo($"Active Skills:");
    //         foreach (SkillItem _skill in __instance.hasSkillList) {
    //             foreach (_skillJsonData data in _skillJsonData.DataList) {
    //                 if (data.Skill_ID == _skill.itemId) {
    //                     Logger.LogInfo($"{data.id}, {data.name}, AttackType: {string.Join(";", data.AttackType)}, Quality: {data.Skill_LV}");
    //                     break;
    //                 }
    //             }
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Logger.LogWarning($"{ex.Message}");
    //     }
    // }

    // public static void LogObjectFields(object obj)
    // {
    //     var type = obj.GetType();
    //     var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //     foreach (var field in fields)
    //     {
    //         try
    //         {
    //             var value = field.GetValue(obj);
    //             Logger.LogInfo($"Field: {field.Name}, Value: {value}");
    //         }
    //         catch (Exception ex)
    //         {
    //             Logger.LogWarning($"Could not read field {field.Name}: {ex.Message}");
    //         }
    //     }

    //     var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    //     foreach (var prop in props)
    //     {
    //         try
    //         {
    //             if (prop.CanRead)
    //             {
    //                 var value = prop.GetValue(obj);
    //                 Logger.LogInfo($"Property: {prop.Name}, Value: {value}");
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Logger.LogWarning($"Could not read property {prop.Name}: {ex.Message}");
    //         }
    //     }
    // }
}
