using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace FightSpade
{
    [BepInPlugin("com.kuborro.plugins.fp2.fightspade", "FightSpade", "1.0.1")]
    [BepInProcess("FP2.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle moddedBundle;
        public static GameObject spadeObject;
        public static bool FightStarted = false;
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

    //We load our assets, as well as create a new "challenge" in the ArenaSpawner
    //I have decided to go with this method instead of simple replacement as it helps with understanding the creation of fully custom challenges and fights.

    class PatchBossFight
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ArenaSpawner), "Start", MethodType.Normal)]
        static void Prefix(ArenaSpawner __instance)
        {
            if (FPStage.stageNameString == "Training" && (FPSaveManager.currentArenaChallenge == 5 || FPSaveManager.currentArenaChallenge == 6)) FPAudio.StopMusic();
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
                    Plugin.FightStarted = false;

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
                }
            }
        }
    }
    //Fix for spawning behavior
    class PatchBossSpade
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerBossSpade), "Start", MethodType.Normal)]
        static void Postfix(PlayerBossSpade __instance, ref Vector2 ___start)
        {
            __instance.position = new Vector2(486, -336); //Both position and start location have to be corrected here, due to assets being loaded from external bundle
            ___start = new Vector2(486, -336);
            __instance.genericTimer = -30f; //Spade's code lacks this line compared to other PlayerBosses - setting this prevents him from instantly shooting you at the start of the fight
        }
    }

    //Black magic to fix outdated code in the game's files. Spade lacks a check if FPStage's timeEnabled bool is true, making him attack you before round starts (unlike other PlayerBoss instances)
    //This fixes it by simulating that functionality
    class PatchBossSpadeRunning
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerBossSpade), "State_Running", MethodType.Normal)]
        static bool Prefix(PlayerBossSpade __instance)
        {
            if (!Plugin.FightStarted)
            {
                Plugin.FightStarted = FPStage.timeEnabled; //Global variable is used so we can easily edit it from other patches
            }
            __instance.targetToPursue = FPStage.FindNearestPlayer(__instance, 640f);
            return Plugin.FightStarted;
        }
    }
    //Needed to make him actually target you, the base code doesnt do it by itself
    class PatchBossSpadeCardThrow
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerBossSpade), "State_ThrowCards", MethodType.Normal)]
        static void Prefix(PlayerBossSpade __instance)
        {
            __instance.Action_FacePlayer(); 
        }
    }

    //Patch to replace Askal with Spade in Shang Mu Dojo
    class PatchBossList
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuArenaBossSelect), "Start", MethodType.Normal)]
        static void Postfix(MenuArenaBossSelect __instance)
        {
            if (FPStage.stageNameString == "Royal Palace") { //Make sure we dont edit the BattleSphere
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
    }

    class PatchBossNames
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuText), "Start", MethodType.Normal)]
        static void Postfix(ref string[] ___paragraph, MenuText __instance)
        {
            if (___paragraph != null && FPStage.stageNameString == "Royal Palace" && __instance.name == "Name") //Same deal as above, we also check if its the MenuText we want
            {
                if (___paragraph.Length > 2) //One other MenuText matches, but it has lenght of 1. We make sure we arent trying to manipulate that one
                ___paragraph[Array.IndexOf(___paragraph, "Askal")] = "Spade";
            }

        }
    }
}
