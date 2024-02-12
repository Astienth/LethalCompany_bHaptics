using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Bhaptics.Tact;
using LethalCompany_bHaptics;

namespace MyBhapticsTactsuit
{

    public class TactsuitVR
    {
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);
        private static ManualResetEvent Rumble_mrse = new ManualResetEvent(false);
        private static ManualResetEvent Jetpack_mrse = new ManualResetEvent(false);
        // dictionary of all feedback patterns found in the bHaptics directory
        public Dictionary<String, FileInfo> FeedbackMap = new Dictionary<String, FileInfo>();

#pragma warning disable CS0618 // remove warning that the C# library is deprecated
        public HapticPlayer hapticPlayer;
#pragma warning restore CS0618 

        private static RotationOption defaultRotationOption = new RotationOption(0.0f, 0.0f);

        public string heartBeatEffect = "HeartBeat";
        public float rumbleIntensity = 1.0f;
        public int heartbeatCount = 0;
        public int maxHeartBeat = 15;

        public void HeartBeatFunc()
        {
            while (true)
            {
                // Check if reset event is active
                HeartBeat_mrse.WaitOne();
                PlaybackHaptics(heartBeatEffect);
                if (heartbeatCount > maxHeartBeat)
                {
                    StopHeartBeat();
                }
                heartbeatCount++;
                Thread.Sleep(1000);
            }
        }
        public void RumbleFunc()
        {
            while (true)
            {
                // Check if reset event is active
                Rumble_mrse.WaitOne();
                PlaybackHaptics("Rumble_Head", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Left_Arms", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Right_Arms", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Vest", true, rumbleIntensity);
                Thread.Sleep(1000);
            }
        }
        public void JetpackFunc()
        {
            while (true)
            {
                // Check if reset event is active
                Jetpack_mrse.WaitOne();
                PlaybackHaptics("ImpactRear", true, 0.5f);
                Thread.Sleep(1000);
            }
        }

        public void PlayJumpScareLight()
        {
            PlaybackHaptics("JumpScareLight_Vest");
            PlaybackHaptics("JumpScare_Left_Arms", true, 0.4f);
            PlaybackHaptics("JumpScare_Right_Arms", true, 0.4f);
            PlaybackHaptics("ShotVisor", true, 0.4f);
        }
        public void PlayJumpScareStrong()
        {
            PlaybackHaptics("JumpScare_Vest");
            PlaybackHaptics("JumpScare_Left_Arms");
            PlaybackHaptics("JumpScare_Right_Arms");
            PlaybackHaptics("ShotVisor");
            PlayHapticsWithDelay("HeartBeatFast", 400);
            PlayHapticsWithDelay("HeartBeatFast", 1400);
        }

        public void RumbleOnce(float rumbleIntensity = 1.0f, bool withDelay = false, int delay = 0)
        {
            if (withDelay)
            {
                Thread thread = new Thread(() =>
                {
                    Thread.Sleep(delay);
                    PlaybackHaptics("Rumble_Head", true, rumbleIntensity);
                    PlaybackHaptics("Rumble_Left_Arms", true, rumbleIntensity);
                    PlaybackHaptics("Rumble_Right_Arms", true, rumbleIntensity);
                    PlaybackHaptics("Rumble_Vest", true, rumbleIntensity);
                });
                thread.Start();
            }
            else
            {
                PlaybackHaptics("Rumble_Head", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Left_Arms", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Right_Arms", true, rumbleIntensity);
                PlaybackHaptics("Rumble_Vest", true, rumbleIntensity);
            }
        }

        public TactsuitVR()
        {

            LOG("Initializing suit");
            try
            {
#pragma warning disable CS0618 // remove warning that the C# library is deprecated
                hapticPlayer = new HapticPlayer("BendyInkMachine_bHaptics", "BendyInkMachine_bHaptics");
#pragma warning restore CS0618
                suitDisabled = false;
            }
            catch { LOG("Suit initialization failed!"); }
            RegisterAllTactFiles();
            LOG("Starting HeartBeat thread...");
            Thread HeartBeatThread = new Thread(HeartBeatFunc);
            HeartBeatThread.Start();
            Thread RumbleThread = new Thread(RumbleFunc);
            RumbleThread.Start();
            Thread JetpackThread = new Thread(JetpackFunc);
            JetpackThread.Start();
        }

        public void LOG(string logStr)
        {
            Plugin.Log.LogMessage(logStr);
        }


        void RegisterAllTactFiles()
        {
            if (suitDisabled) { return; }
            // Get location of the compiled assembly and search through "bHaptics" directory and contained patterns
            string assemblyFile = Assembly.GetExecutingAssembly().Location;
            string myPath = Path.GetDirectoryName(assemblyFile);
            LOG("Assembly path: " + myPath);
            string configPath = myPath + "\\bHaptics";
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.tact", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    hapticPlayer.RegisterTactFileStr(prefix, tactFileStr);
                    LOG("Pattern registered: " + prefix);
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(prefix, Files[i]);
            }
            systemInitialized = true;
        }

        public void PlayHapticsWithDelay(String key, int delay)
        {
            Thread thread = new Thread(() =>
            {
                Thread.Sleep(delay);
                PlaybackHaptics(key);
            });
            thread.Start();
        }

        public void PlaybackHaptics(String key, bool forced = true, float intensity = 1.0f, float duration = 1.0f)
        {
            if (suitDisabled) { return; }
            Dictionary<String, FileInfo> MapCopy = new Dictionary<String, FileInfo>(FeedbackMap);
            if (MapCopy.ContainsKey(key))
            {
                ScaleOption scaleOption = new ScaleOption(intensity, duration);
                if (hapticPlayer.IsPlaying() && !forced)
                {
                    return;
                }
                else
                {
                    hapticPlayer.SubmitRegisteredVestRotation(key, key, defaultRotationOption, scaleOption);
                }
            }
            else
            {
                LOG("Feedback not registered: " + key);
            }
        }

        public void StartHeartBeat(bool fast = false)
        {
            heartBeatEffect = (fast) ? "HeartBeatFast" : "HeartBeat";
            HeartBeat_mrse.Set();
        }

        public void StopHeartBeat()
        {
            HeartBeat_mrse.Reset();
            heartbeatCount = 0;
        }

        public void StartRumble(float intensity)
        {
            rumbleIntensity = intensity;
            Rumble_mrse.Set();
        }

        public void StopRumble()
        {
            Rumble_mrse.Reset();
        }

        public void StartJetPack()
        {
            Jetpack_mrse.Set();
        }

        public void StopJetPack()
        {
            Jetpack_mrse.Reset();
        }

        public void StopHapticFeedback(String effect)
        {
            hapticPlayer.TurnOff(effect);
        }

        public void StopAllHapticFeedback()
        {
            StopThreads();
            foreach (String key in FeedbackMap.Keys)
            {
                hapticPlayer.TurnOff(key);
            }
        }

        public void StopThreads()
        {
            StopHeartBeat();
            StopRumble();
            StopJetPack();
        }


    }
}
