using System;
using UnityEngine;
using MoonlightVR.Moonlight;

namespace MoonlightVR.UI
{
    [System.Serializable]
    public class SettingsData
    {
        public string savedHostIp = "192.168.1.100";
        public int resolutionWidth = 2560;
        public int resolutionHeight = 1440;
        public int targetFps = 90;
        public int bitrateKbps = 45000;
        public VideoCodec codec = VideoCodec.HEVC;

        public float curveAngle = 45f;
        public float screenDistance = 1.8f;
        public float screenScale = 1.0f;
        public bool passthrough = true;
        public int environmentIndex = 0; // 0=Passthrough, 1=Void, 2=Cosmic, 3=Alpine, 4=Cyberpunk, 5=Nordic, 6=Loft

        private const string PrefKey = "MoonlightVR_SavedSettings";

        public static SettingsData Load()
        {
            if (PlayerPrefs.HasKey(PrefKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PrefKey);
                    return JsonUtility.FromJson<SettingsData>(json);
                }
                catch (Exception) { }
            }
            return new SettingsData();
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(PrefKey, json);
            PlayerPrefs.Save();
        }
    }
}
