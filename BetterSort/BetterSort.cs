using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JSONClass;
using KBEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterSort;

[BepInPlugin("EtherealCat.BetterSort", "BetterSort", "1.0")]
public class BetterSortPlugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    enum ItemType
    {
        TaskWeapon = 0,
        Unknown1 = 1,
        Unknown2 = 2,
        SkillManual = 3,
        CultivationTechniqueManual = 4,
        Consumable = 5,
        MedicinalMaterial = 6,
        TaskItem = 7,
        Material = 8,
        Artifact = 9,
        Book = 13,
        Consumable2 = 15,
        Other = 16,
    }

    private static readonly Dictionary<int, int> ItemTypePriority = new()
    {
        { (int)ItemType.Consumable, 0 },
        { (int)ItemType.Consumable2, 0 },

        { (int)ItemType.TaskWeapon, 1 },
        { (int)ItemType.Unknown1, 1 },
        { (int)ItemType.Unknown2, 1 },
        { (int)ItemType.Artifact, 1 },

        { (int)ItemType.TaskItem, 2 },
        { (int)ItemType.Other, 2 },

        { (int)ItemType.Book, 3 },
        { (int)ItemType.SkillManual, 11 },
        { (int)ItemType.CultivationTechniqueManual, 11 },   
        // Everything else will get a default priority 10
    };

    private void Start()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(BetterSortPlugin), null);
        Logger.LogInfo("Plugin loaded");
    }

    // Will work for both the player inventory and storehouse
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ITEM_INFO_LIST), "SortItems")]
    public static bool BetterSortItems_Patch_Prefix(ITEM_INFO_LIST __instance)
    {
        Logger.LogInfo("ITEM_INFO_LIST::SortItems()");

        __instance.values.Sort((a, b) =>
        {
            var data1 = _ItemJsonData.DataDict[a.itemId];
            var data2 = _ItemJsonData.DataDict[b.itemId];

            // Handle quality from Seid if present
            JSONObject seid1 = a.Seid;
            JSONObject seid2 = b.Seid;

            int tiebreaker1 = data1.GetHashCode();
            int tiebreaker2 = data2.GetHashCode();

            int quality1 = data1.quality;
            int quality2 = data2.quality;

            if (seid1 != null && seid1.HasField("quality"))
            {
                quality1 = seid1["quality"].I;
                tiebreaker1 += seid1.GetHashCode();
            }
            if (seid2 != null && seid2.HasField("quality"))
            {
                quality2 = seid2["quality"].I;
                tiebreaker2 += seid2.GetHashCode();
            }

            static void ResolveItemQuality(_ItemJsonData data, ref int quality)
            {

                if (data.type == 3 || data.type == 4)
                {
                    quality *= 2;
                }
                if (data.type == 0 || data.type == 1 || data.type == 2)
                {
                    quality++;
                }
            }

            ResolveItemQuality(data1, ref quality1);
            ResolveItemQuality(data2, ref quality2);

            if (quality1 != quality2)
            {
                // Sort by quality descending
                return quality2.CompareTo(quality1);
            }

            int typePriority1 = ItemTypePriority.TryGetValue(data1.type, out int priority1) ? priority1 : 10;
            int typePriority2 = ItemTypePriority.TryGetValue(data2.type, out int priority2) ? priority2 : 10;
            if (typePriority1 != typePriority2)
            {
                return typePriority1.CompareTo(typePriority2);
            }

            if (data1.type != data2.type)
            {
                return data1.type.CompareTo(data2.type);
            }

            if (data1.id != data2.id)
            {
                return data1.id.CompareTo(data2.id);
            }

            // Final tiebreaker
            return tiebreaker1.CompareTo(tiebreaker2);
        });

        return false; // Prevent call to original method
    }

    // [HarmonyPostfix]
    // [HarmonyPatch(typeof(Avatar), "SortItem")]
    // public static void Test(Avatar __instance)
    // {
    //     // Print item types
    //     var types = new HashSet<int>();
    //     foreach (var item in ((AvatarBase)__instance).itemList.values)
    //     {
    //         var itemData = _ItemJsonData.DataDict[item.itemId];
    //         types.Add(itemData.type);
    //     }
    //     Logger.LogInfo("All item types:");
    //     foreach (var type in types.OrderBy(t => t))
    //     {
    //         Logger.LogInfo($"Item Type: {type}");
    //     }

    //     var itemList = ((AvatarBase)__instance).itemList.values;
    //     foreach (var item in itemList)
    //     {
    //         if (!_ItemJsonData.DataDict.TryGetValue(item.itemId, out var data)) continue;

    //         Logger.LogInfo($"Item: {data.name}, ID: {data.id}, Type: {data.type}");
    //     }
    // }

    public static void LogObjectFields(object obj)
    {
        var type = obj.GetType();
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            try
            {
                var value = field.GetValue(obj);
                Logger.LogInfo($"Field: {field.Name}, Value: {value}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read field {field.Name}: {ex.Message}");
            }
        }

        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            try
            {
                if (prop.CanRead)
                {
                    var value = prop.GetValue(obj);
                    Logger.LogInfo($"Property: {prop.Name}, Value: {value}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read property {prop.Name}: {ex.Message}");
            }
        }
    }
}

// Fields of _ItemJsonData:
// Field: id, Value: 5504
// Field: ItemIcon, Value: 5504
// Field: maxNum, Value: 9999999
// Field: TuJianType, Value: 4
// Field: ShopType, Value: 11
// Field: WuWeiType, Value: 0
// Field: ShuXingType, Value: 0
// Field: type, Value: 5
// Field: quality, Value: 6
// Field: typePinJie, Value: 0
// Field: StuTime, Value: 0
// Field: vagueType, Value: 1
// Field: price, Value: 80000
// Field: CanSale, Value: 0
// Field: DanDu, Value: 2
// Field: CanUse, Value: 1
// Field: NPCCanUse, Value: 1
// Field: yaoZhi1, Value: 0
// Field: yaoZhi2, Value: 0
// Field: yaoZhi3, Value: 0
// Field: ShuaXin, Value: 120
// Field: name, Value: Three Yang Forging Pill
// Field: FaBaoType, Value: 
// Field: desc, Value: Soul Sense +12
// Field: desc2, Value: Pill that enhances Soul Sense. ...
// Field: Affix, Value: System.Collections.Generic.List`1[System.Int32]
// Field: ItemFlag, Value: System.Collections.Generic.List`1[System.Int32]
// Field: seid, Value: System.Collections.Generic.List`1[System.Int32]
// Field: wuDao, Value: System.Collections.Generic.List`1[System.Int32]