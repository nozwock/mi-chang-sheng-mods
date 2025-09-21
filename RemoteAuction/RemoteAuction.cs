using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KBEngine;
using PaiMai;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Remote_Auction;

[BepInPlugin("Arkania.Remote.Auction", "Arkania Remote Auction", "1.0.0")]
public class Main : BaseUnityPlugin
{
    static Main Inst;

    UnityEngine.GameObject go;

    void Start()
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        Inst = this;
        StartCoroutine(FindGameObject("OkBtn"));
    }

    IEnumerator FindGameObject(string name)
    {
        UnityEngine.GameObject obj;
        do
        {
            obj = ResManager.inst.LoadPrefab("PaiMai/NewPaiMaiUI");
            yield return new WaitForFixedUpdate();
        }
        while (obj == null);
        go = GetChild<RectTransform>(obj, name, showError: true);
        if (go != null)
        {
            Logger.Log(LogLevel.Message, "Load " + name + " has been done");
        }
    }

    UnityEngine.GameObject GetChild<T>(UnityEngine.GameObject gameObject, string name, bool showError = true) where T : Component
    {
        T[] componentsInChildren = gameObject.GetComponentsInChildren<T>(true);
        foreach (T val in componentsInChildren)
        {
            if (val.name == name)
            {
                return val.gameObject;
            }
        }
        if (showError)
        {
            Debug.LogError("对象" + gameObject.name + "不存在子对象" + name);
        }
        return null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CyEmailCell), "Init")]
    static void CyEmailCell_Init_Patch(EmailData emailData, bool isDeath, CyEmailCell __instance)
    {
        if (isDeath || emailData.PaiMaiInfo == null)
        {
            return;
        }
        KBEngine.Avatar player = Tools.instance.getPlayer();
        if (!(emailData.PaiMaiInfo.EndTime >= player.worldTimeMag.getNowTime()))
        {
            return;
        }
        UnityEngine.GameObject obj = Object.Instantiate<UnityEngine.GameObject>(Inst.go, __instance.transform);
        obj.transform.localPosition = new Vector3(0f, -345f);
        obj.transform.localScale = Vector3.one;
        obj.GetComponent<FpBtn>().enabled = false;
        obj.AddComponent<Button>().onClick.AddListener(delegate
        {
            if (player.level < 6 || player.money < 50000)
            {
                UIPopTip.Inst.Pop("要求筑基后期和50000灵石！");
            }
            else
            {
                KBEngine.Avatar obj2 = player;
                obj2.money -= 50000;
                GameObejetUtils.Inst(ResManager.inst.LoadPrefab("PaiMai/NewPaiMaiUI")).GetComponent<NewPaiMaiJoin>().Init(emailData.PaiMaiInfo.PaiMaiId, emailData.npcId);
            }
        });
    }
}
