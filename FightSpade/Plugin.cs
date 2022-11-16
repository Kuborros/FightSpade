using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace FightSpade
{
    [BepInPlugin("com.kuborro.plugins.fp2.fightspade", "FightSpade", "1.0.0")]
    [BepInProcess("FP2.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle moddedBundle;
        public static GameObject spadeObject;
        private void Awake()
        {
            string assetPath = Path.Combine(Path.GetFullPath("."), "mod_overrides");
            moddedBundle = AssetBundle.LoadFromFile(Path.Combine(assetPath, "fightspade.assets"));
            if (moddedBundle == null)
            {
                Logger.LogError("Failed to load AssetBundle! Mod cannot work without it, exiting. Please reinstall it.");
                return;
            }

            var harmony = new Harmony("com.kuborro.plugins.fp2.fightspade");
            harmony.PatchAll(typeof(PatchBossFight));
            harmony.PatchAll(typeof(PatchBossSpade));
            harmony.PatchAll(typeof(PatchBossSpadeRunning));
            harmony.PatchAll(typeof(PatchBossSpadeCardThrow));
            harmony.PatchAll(typeof(PatchBossList));
            harmony.PatchAll(typeof(PatchBossNames));
        }
    }

    class PatchBossFight
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ArenaSpawner), "Start", MethodType.Normal)]
        static void Prefix(ArenaSpawner __instance)
        {
            if (FPStage.stageNameString == "Training" && Plugin.spadeObject == null)
            {
                UnityEngine.Object[] modKuboPre = Plugin.moddedBundle.LoadAllAssets();
                foreach (var mod in modKuboPre)
                {
                    if (mod.GetType() == typeof(GameObject))
                    {
                        Plugin.spadeObject = (GameObject)GameObject.Instantiate(mod);
                        Plugin.spadeObject.name = "Boss Spade";
                    }
                }

                if (Plugin.spadeObject != null && (FPSaveManager.currentArenaChallenge == 5 || FPSaveManager.currentArenaChallenge == 6))
                {
                    __instance.syncChallengeID = false;

                    ArenaRoundSpawnList spadeList = new()
                    {
                        bossBattle = true,
                        waitForObjectDestruction = false,
                        objectList = new FPBaseObject[] { Plugin.spadeObject.GetComponent<PlayerBossSpade>() }
                    };
                    ArenaSpawnList spawnList = new()
                    {
                        name = "SpadeBoss",
                        challengeID = 36,
                        rewardCrystals = 1000,
                        rewardTimeCapsule = false,
                        timeCapsuleID = 0,
                        spawnAllies = false,
                        alliesAreHostile = false,
                        disableCorePickups = false,
                        spawnAtStart = new FPBaseObject[] { Plugin.spadeObject.GetComponent<PlayerBossSpade>() },
                        roundObjectList = new ArenaRoundSpawnList[] { spadeList },
                        spawnDelay = new float[] { 0 },
                        endCutscene = "",
                        victoryDelayOffset = 0
                    };

                    __instance.challenges = __instance.challenges.AddToArray(spawnList);
                    __instance.currentChallenge = 6;
                    FPSaveManager.currentArenaChallenge = 6;
                    Plugin.spadeObject.GetComponent<PlayerBossSpade>().position = new Vector2(-486, 336);
                }
            }
        }
    }

    class PatchBossSpade
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerBossSpade), "Start", MethodType.Normal)]
        static void Postfix(PlayerBossSpade __instance, ref Vector2 ___start)
        {
            __instance.position = new Vector2(486, -336);
            ___start = new Vector2(486, -336);
            __instance.genericTimer = -30f;
        }
    }
    class PatchBossSpadeRunning
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerBossSpade), "State_Running", MethodType.Normal)]
        static bool Prefix(PlayerBossSpade __instance)
        {
            __instance.targetToPursue = FPStage.FindNearestPlayer(__instance, 640f);
            return FPStage.timeEnabled;
        }
    }
    class PatchBossSpadeCardThrow
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerBossSpade), "State_ThrowCards", MethodType.Normal)]
        static void Prefix(PlayerBossSpade __instance)
        {
            if (FPStage.timeEnabled && __instance.state != new FPObjectState(__instance.State_KO))
            {
                __instance.Action_FacePlayer();
            }
        }
    }


    class PatchBossList
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuArenaBossSelect), "Start", MethodType.Normal)]
        static void Postfix(MenuArenaBossSelect __instance)
        {
            SpriteRenderer[] components = __instance.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer component in components)
            {
                if (component.sprite.name == "arena_bosses_6")
                {
                    component.sprite = Plugin.moddedBundle.LoadAsset<Sprite>("spade_portrait");
                }
            }

        }
    }

    class PatchBossNames
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuText), "Start", MethodType.Normal)]
        static void Postfix(ref string[] ___paragraph, MenuText __instance)
        {
            if (___paragraph != null && FPStage.stageNameString == "Royal Palace" && __instance.name == "Name")
            {
                ___paragraph[Array.IndexOf(___paragraph, "Askal")] = "Spade";
            }

        }
    }
}
