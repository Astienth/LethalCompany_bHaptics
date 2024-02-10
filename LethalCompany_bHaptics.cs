using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MyBhapticsTactsuit;
using System.Threading;
using UnityEngine;

namespace LethalCompany_bHaptics
{
    [BepInPlugin("org.bepinex.plugins.LethalCompany_bHaptics", "LethalCompany_bHaptics integration", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;
        public delegate void MyMethod();
        public static int playerHealthPreUpdate;
        public static bool jumped = false;

        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin LethalCompany_bHaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.LethalCompany_bHaptics");
            harmony.PatchAll();
        }

        public static void RunFunctionWithDelay(MyMethod method, int delay)
        {
            Thread thread = new Thread(() =>
            {
                Thread.Sleep(delay);
                method.Invoke();
            });
            thread.Start();
        }
    }
    
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "DamagePlayer")]
    public class bhaptics_DamagePlayer
    {
        [HarmonyPostfix]
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            if (!__instance.IsOwner || __instance.isPlayerDead || !__instance.AllowPlayerDeath())
                return;
            Plugin.tactsuitVr.PlaybackHaptics("Impact");
            Plugin.tactsuitVr.PlaybackHaptics("ImpactRear");
            Plugin.tactsuitVr.PlaybackHaptics("ShotVisor");
        }
    }

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "KillPlayer")]
    public class bhaptics_KillPlayer
    {
        [HarmonyPostfix]
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            if (!__instance.IsOwner || __instance.isPlayerDead || !__instance.AllowPlayerDeath())
                return;
            Plugin.tactsuitVr.PlaybackHaptics("Death");
            Plugin.tactsuitVr.StopThreads();
        }
    }

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "LateUpdate")]
    public class bhaptics_LateUpdate
    {
        [HarmonyPrefix]
        public static void Prefix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            Plugin.playerHealthPreUpdate = __instance.health;
        }
        [HarmonyPostfix]
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            int newHealth = __instance.health;
            if (newHealth - Plugin.playerHealthPreUpdate > 0 && newHealth % 5 == 0)
            {
                Plugin.tactsuitVr.PlaybackHaptics("Heal");
            }
            if (newHealth > 20)
            {
                Plugin.tactsuitVr.StopHeartBeat();
            }
            else
            {
                Plugin.tactsuitVr.StartHeartBeat();
            }
            if (Traverse.Create(__instance).Field("isJumping").GetValue<bool>())
            {
                if(!Plugin.jumped)
                {
                    Plugin.jumped = true;
                    Plugin.tactsuitVr.PlaybackHaptics("OnJump");
                }
            }
            else
            {
                Plugin.jumped = false;
            }
        }
    }

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "PlayerHitGroundEffects")]
    public class bhaptics_PlayerHitGroundEffects
    {
        [HarmonyPostfix]
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump");
        }
    }
}
