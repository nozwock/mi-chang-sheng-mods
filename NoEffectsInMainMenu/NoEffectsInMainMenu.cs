using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoEffectsInMainMenu;

[BepInPlugin("EtherealCat.NoEffectsInMainMenu", "NoEffectsInMainMenu", "1.0")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    void Awake()
    {
        Log = base.Logger;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IEnumerator RemoveEffectCoroutine()
        {
            int totalTries = 30;
            for (int i = 0; i < totalTries; i++)
            {
                var target = GameObject.Find("NewMain(Clone)")?.transform.Find("特效层级");
                if (target != null)
                {
                    target.gameObject.SetActive(false);
                    Log.LogInfo("Disabled NewMain(Clone)/特效层级");
                    yield break;
                }
                yield return null; // wait 1 frame
            }
            Log.LogInfo($"Failed to disable NewMain(Clone)/特效层级 after {totalTries} tries");
        }

        if (scene.name == "MainMenu")
        {
            StartCoroutine(RemoveEffectCoroutine());
        }
    }
}
