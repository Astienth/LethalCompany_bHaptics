using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MyBhapticsTactsuit;
using System.Threading;

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
    /*
    [HarmonyPatch(typeof(PlayerController), "Die")]
    public class bhaptics_OnDeath
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerController __instance)
        {
            Plugin.tactsuitVr.PlaybackHaptics("Death");
            Plugin.tactsuitVr.StopThreads();
        }
    }
    */
}
