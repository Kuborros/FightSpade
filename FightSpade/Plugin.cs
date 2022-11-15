using BepInEx;
using HarmonyLib;
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
        }
    }

    class PatchBossFight
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ArenaSpawner), "Start", MethodType.Normal)]
        static void Prefix(ArenaSpawner __instance)
        {
            if (FPStage.stageNameString == "Training")
            {
                Object[] modKuboPre = Plugin.moddedBundle.LoadAllAssets();
                foreach (var mod in modKuboPre)
                {
                    if (mod.GetType() == typeof(GameObject))
                    {
                        Plugin.spadeObject = (GameObject)GameObject.Instantiate(mod);
                        Plugin.spadeObject.name = "Boss Spade";
                    }
                }

                if (Plugin.spadeObject != null && FPSaveManager.currentArenaChallenge == 5)
                {
                    __instance.syncChallengeID = false;

                    ArenaRoundSpawnList spadeList = new ArenaRoundSpawnList
                    {
                        bossBattle = true,
                        waitForObjectDestruction = false,
                        objectList = new FPBaseObject[] { Plugin.spadeObject.GetComponent<PlayerBossSpade>() }
                    };
                    ArenaSpawnList spawnList = new ArenaSpawnList
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
                    Plugin.spadeObject.GetComponent<PlayerBossSpade>().position = new Vector2(-486,336);
                }
            }
        }
    }
}
