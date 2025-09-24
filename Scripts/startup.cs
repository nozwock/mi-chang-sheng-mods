// Meant for UnityExplorer

// JSONTemplates.TOJSON() for serialization

public class g
{
    public enum ItemType
    {
        Weapon = (int)ItemTypes.武器,
        Clothing = (int)ItemTypes.衣服,
        Accessory = (int)ItemTypes.饰品,
        SkillBook = (int)ItemTypes.技能书,
        CultivationBook = (int)ItemTypes.功法,
        Pill = (int)ItemTypes.丹药,
        Herb = (int)ItemTypes.药材,
        Quest = (int)ItemTypes.任务,
        Ore = (int)ItemTypes.矿石,
        Furnace = (int)ItemTypes.丹炉,
        Recipe = (int)ItemTypes.丹方,
        Dreg = (int)ItemTypes.药渣,
        Book = (int)ItemTypes.书籍,
        SecretBook = (int)ItemTypes.秘籍,
        Boat = (int)ItemTypes.灵舟,
        PillSpecial = (int)ItemTypes.秘药,
        Other = (int)ItemTypes.其他
    }

    // Bag.BaseItem.GetItemType(type) - gives group
    public enum ItemGroup
    {
        All = (int)Bag.ItemType.全部,
        Artifact = (int)Bag.ItemType.法宝,
        Pill = (int)Bag.ItemType.丹药,
        Book = (int)Bag.ItemType.秘籍,
        Material = (int)Bag.ItemType.材料,
        Herb = (int)Bag.ItemType.草药,
        Other = (int)Bag.ItemType.其他
    }

    public static KBEngine.Avatar player { get => Tools.instance?.getPlayer(); }

    public static void Log(object message)
    {
        UnityExplorer.ExplorerCore.Log(message);
    }

    public static void UnlockAllAcensionSkills()
    {
        var player = g.player;
        if (player == null || player.level < 13) // Early Deity Transformation
        {
            return;
        }
        foreach (var item in JSONClass.TianJieMiShuData.DataDict)
        {
            if (item.Value.Type == 0) // i.e. needs to be comprehended
            {
                var id = item.Value.id;
                var found = false;
                foreach (var item2 in player.TianJieCanLingWuSkills.list)
                {
                    if (item2.Str == id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Log($"Adding {id}");
                    player.TianJieCanLingWuSkills.Add(id);
                }
            }
        }
    }

    static void ResolveItemQuality(int type, ref int quality)
    {
        if (type == 3 || type == 4)
        {
            quality *= 2;
        }
        if (type == 0 || type == 1 || type == 2)
        {
            quality++;
        }
    }

    static ItemGroup GetItemGroup(int type)
    {
        return (ItemGroup)Bag.BaseItem.GetItemType(type);
    }

    public static int AddItemGroup(
        ItemGroup group,
        int count,
        int quality = -1,
        // bool allowNew = false,
        bool atmost = true, // Ensure items have atleast `count` value as their quantity
        bool pillRatio = true, // Multiply `count` with max pill usages
        bool maxQuality = true // Treat `quality` as upper threshold for quality of items to affect
    )
    {
        var player = g.player;
        if (player == null)
            return 1;

        var items = player.itemList.values;
        foreach (var item in items)
        {
            // var data = new GUIPackage.item(item.itemId);
            var data = JSONClass._ItemJsonData.DataDict[item.itemId]; // for allowNew use addItem
            var itemQuality = data.quality;
            ResolveItemQuality(data.type, ref itemQuality);

            var itemGroup = GetItemGroup(data.type);
            if (itemGroup == group && data.maxNum > 1)
            {
                if (
                    quality > 0
                    && (maxQuality
                    ? quality < itemQuality
                    : quality != itemQuality)
                )
                {
                    continue;
                }

                int num = count;
                if (itemGroup == ItemGroup.Pill && pillRatio)
                {
                    num *= data.CanUse;
                }

                if (!atmost || count < 0)
                {
                    item.itemCount = (uint)Mathf.Clamp(num + item.itemCount, 1, data.maxNum);
                }
                else if (item.itemCount < num)
                {
                    item.itemCount = (uint)Mathf.Clamp(num, 1, data.maxNum);
                }
            }
        }

        return 0;
    }

    public static int SetNPCExp(float expFraction, int npcId = -1)
    {
        expFraction = Mathf.Clamp(expFraction, 0, 1);

        var panel = UnityEngine.Resources.FindObjectsOfTypeAll<UINPCInfoPanel>().FirstOrDefault();

        var isPanelOpen = panel?.gameObject.active == true;
        if (isPanelOpen || npcId != -1)
        {
            // NPCXiuLian - Npc cultivation
            // NpcJieSuanManager - Npc settlement manager
            if (npcId != -1)
            {
                npcId = NPCEx.NPCIDToNew(npcId);
            }
            else {
                npcId = NPCEx.NPCIDToNew(panel.npc.ID);
            }

            var data = npcId.NPCJson();
            if (data == null)
                return 2;
            var name = jsonData.instance.AvatarRandomJsonData[npcId.ToString()]["Name"].Str;
            var maxExp = jsonData.instance.LevelUpDataJsonData[data["Level"].I.ToString()]["MaxExp"].I;
            // Or data["NextExp"].I
            var newExp = (int)((float)maxExp * expFraction);

            Log($"#{npcId} {name} Exp:{data["exp"].I}->{newExp}");
            data.SetField("exp", newExp);
            if (isPanelOpen)
            {
                panel.npc.Exp = newExp;
            }

            var lv = data["Level"].I;
            if (expFraction == 1 && (lv == 3 || lv == 6 || lv == 9 || lv == 12))
            {
                NpcJieSuanManager.inst.npcStatus.SetNpcStatus(npcId, 2); // bottleneck
            }
            else if (data["Status"]["StatusId"].I == 2)
            {
                NpcJieSuanManager.inst.npcStatus.SetNpcStatus(npcId, 1); // normal
            }

            return 0;
        }

        return 1;
    }
}
