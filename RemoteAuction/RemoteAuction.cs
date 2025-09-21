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

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "Arkania.Remote.Auction";
    public const string PLUGIN_NAME = "Arkania Remote Auction";
    public const string PLUGIN_VERSION = "1.0.0";

    static Harmony harmony;
    static UnityEngine.GameObject btnGo;

    void Start()
    {
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin), PLUGIN_GUID);
        StartCoroutine(FindGameObject("OkBtn"));
    }

    void OnDestroy()
    {
        harmony?.UnpatchSelf();
        harmony = null;
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
        btnGo = GetChild<RectTransform>(obj, name, showError: true);
        if (btnGo != null)
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
        UnityEngine.GameObject obj = Object.Instantiate<UnityEngine.GameObject>(btnGo, __instance.transform);
        obj.transform.localPosition = new Vector3(0f, -345f);
        obj.transform.localScale = Vector3.one;
        obj.GetComponent<FpBtn>().enabled = false;
        obj.AddComponent<Button>().onClick.AddListener(delegate
        {
            GameObejetUtils.Inst(ResManager.inst.LoadPrefab("PaiMai/NewPaiMaiUI")).GetComponent<NewPaiMaiJoin>().Init(emailData.PaiMaiInfo.PaiMaiId, emailData.npcId);
        });
    }
}
