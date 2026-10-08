// =============================================================================
// MoonlightVR for Meta Quest 3
// Copyright (C) 2026 Pharmacophobia & Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonlightVR.Moonlight;
using MoonlightVR.VR;

namespace MoonlightVR.UI
{
    /// <summary>
    /// World-space VR dashboard floating below the virtual screen.
    /// Manages host pairing, streaming controls, display preferences, and passthrough toggles.
    /// </summary>
    public class VRMenuHUD : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject displayPanel;
        [SerializeField] private GameObject screenPanel;

        [Header("Connection UI Elements")]
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI pinDisplayModalText;
        [SerializeField] private GameObject pinModalRoot;
        [SerializeField] private Button scanHostsButton;
        [SerializeField] private Button pairButton;
        [SerializeField] private Button streamButton;
        [SerializeField] private Button disconnectButton;

        [Header("Display UI Elements")]
        [SerializeField] private Slider bitrateSlider;
        [SerializeField] private TextMeshProUGUI bitrateText;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fpsDropdown;
        [SerializeField] private TMP_Dropdown codecDropdown;

        [Header("Screen Tuning Elements")]
        [SerializeField] private Slider curveSlider;
        [SerializeField] private TextMeshProUGUI curveText;
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Button recenterButton;
        [SerializeField] private Toggle passthroughToggle;

        [Header("Environments & Keyboard UI Elements")]
        [SerializeField] private TMP_Dropdown environmentDropdown;
        [SerializeField] private Button nextEnvironmentButton;
        [SerializeField] private TextMeshProUGUI environmentLabelText;
        [SerializeField] private Button toggleKeyboardButton;
        [SerializeField] private Button quickDictationButton;

        [Header("Open Source & Legal Compliance (GPL-3.0)")]
        [SerializeField] private Button aboutButton;
        [SerializeField] private GameObject aboutModalRoot;
        [SerializeField] private TextMeshProUGUI aboutModalText;
        [SerializeField] private Button aboutCloseButton;

        [Header("Diagnostics")]
        [SerializeField] private TextMeshProUGUI fpsDiagnosticsText;

        [Header("Scene References")]
        [SerializeField] private CurvedScreenGenerator screenGenerator;
        [SerializeField] private SpatialScreenInteraction screenInteraction;
        [SerializeField] private MetaPassthroughManager passthroughManager;
        [SerializeField] private VREnvironmentManager environmentManager;
        [SerializeField] private VRVirtualKeyboard virtualKeyboard;

        private SettingsData _settings;
        private string _activePin = "";

        private void Start()
        {
            EnsureSubsystemsExist();
            _settings = SettingsData.Load();
            ApplyLoadedSettings();
            RegisterEventHandlers();
        }

        private void EnsureSubsystemsExist()
        {
            if (VREnvironmentManager.Instance == null)
            {
                var envGo = new GameObject("VREnvironmentManager");
                environmentManager = envGo.AddComponent<VREnvironmentManager>();
            }
            if (VRDictationManager.Instance == null)
            {
                var dictGo = new GameObject("VRDictationManager");
                dictGo.AddComponent<VRDictationManager>();
            }
            if (VRVirtualKeyboard.Instance == null)
            {
                var kbGo = new GameObject("VRVirtualKeyboard");
                virtualKeyboard = kbGo.AddComponent<VRVirtualKeyboard>();
            }
        }

        private void RegisterEventHandlers()
        {
            if (scanHostsButton != null) scanHostsButton.onClick.AddListener(OnScanHostsClicked);
            if (pairButton != null) pairButton.onClick.AddListener(OnPairClicked);
            if (streamButton != null) streamButton.onClick.AddListener(OnStreamClicked);
            if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
            if (recenterButton != null) recenterButton.onClick.AddListener(OnRecenterClicked);

            if (bitrateSlider != null) bitrateSlider.onValueChanged.AddListener(OnBitrateChanged);
            if (curveSlider != null) curveSlider.onValueChanged.AddListener(OnCurveChanged);
            if (distanceSlider != null) distanceSlider.onValueChanged.AddListener(OnDistanceChanged);
            if (scaleSlider != null) scaleSlider.onValueChanged.AddListener(OnScaleChanged);
            if (passthroughToggle != null) passthroughToggle.onValueChanged.AddListener(OnPassthroughToggled);

            if (environmentDropdown != null)
            {
                environmentDropdown.ClearOptions();
                environmentDropdown.AddOptions(new List<string> {
                    "👁️ Passthrough",
                    "🎬 Cinema Void",
                    "🌌 Cosmic Nebula",
                    "🏔️ Alpine Sunset",
                    "🏙️ Cyberpunk Penthouse",
                    "🌲 Nordic Sanctuary",
                    "🏛️ Modern Loft"
                });
                environmentDropdown.onValueChanged.AddListener(OnEnvironmentDropdownChanged);
            }
            if (nextEnvironmentButton != null) nextEnvironmentButton.onClick.AddListener(OnNextEnvironmentClicked);
            if (toggleKeyboardButton != null) toggleKeyboardButton.onClick.AddListener(OnToggleKeyboardClicked);
            if (quickDictationButton != null) quickDictationButton.onClick.AddListener(OnQuickDictationClicked);
            if (aboutButton != null) aboutButton.onClick.AddListener(OnAboutClicked);
            if (aboutCloseButton != null) aboutCloseButton.onClick.AddListener(OnCloseAboutClicked);

            if (SunshineClient.Instance != null)
            {
                SunshineClient.Instance.OnStatusChanged += UpdateStatusText;
                SunshineClient.Instance.OnPairingComplete += HandlePairingResult;
                SunshineClient.Instance.OnStreamLaunched += HandleStreamLaunched;
            }
        }

        private void OnDestroy()
        {
            if (SunshineClient.Instance != null)
            {
                SunshineClient.Instance.OnStatusChanged -= UpdateStatusText;
                SunshineClient.Instance.OnPairingComplete -= HandlePairingResult;
                SunshineClient.Instance.OnStreamLaunched -= HandleStreamLaunched;
            }
        }

        private void Update()
        {
            if (fpsDiagnosticsText != null && MoonlightVideoReceiver.Instance != null)
            {
                fpsDiagnosticsText.text = $"Stream: {MoonlightVideoReceiver.Instance.CurrentFps:F1} FPS";
            }
        }

        private void ApplyLoadedSettings()
        {
            if (ipInputField != null) ipInputField.text = _settings.savedHostIp;
            if (bitrateSlider != null)
            {
                bitrateSlider.value = _settings.bitrateKbps / 1000f;
                OnBitrateChanged(bitrateSlider.value);
            }
            if (curveSlider != null)
            {
                curveSlider.value = _settings.curveAngle;
                OnCurveChanged(curveSlider.value);
            }
            if (distanceSlider != null)
            {
                distanceSlider.value = _settings.screenDistance;
                OnDistanceChanged(distanceSlider.value);
            }
            if (scaleSlider != null)
            {
                scaleSlider.value = _settings.screenScale;
                OnScaleChanged(scaleSlider.value);
            }
            if (passthroughToggle != null)
            {
                passthroughToggle.isOn = _settings.passthrough;
                OnPassthroughToggled(_settings.passthrough);
            }
            if (environmentDropdown != null)
            {
                environmentDropdown.value = _settings.environmentIndex;
                OnEnvironmentDropdownChanged(_settings.environmentIndex);
            }
            else if (VREnvironmentManager.Instance != null)
            {
                VREnvironmentManager.Instance.SetEnvironment((VREnvironmentType)_settings.environmentIndex);
            }
        }

        #region UI Callbacks

        private async void OnScanHostsClicked()
        {
            UpdateStatusText("Scanning LAN for Sunshine hosts...");
            if (SunshineClient.Instance != null)
            {
                var hosts = await SunshineClient.Instance.DiscoverHostsAsync();
                if (hosts.Count > 0)
                {
                    ipInputField.text = hosts[0].ipAddress;
                    UpdateStatusText($"Found: {hosts[0].hostname} ({hosts[0].ipAddress})");
                }
                else
                {
                    UpdateStatusText("No hosts found via broadcast. Enter IP manually.");
                }
            }
        }

        private async void OnPairClicked()
        {
            string hostIp = ipInputField != null ? ipInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(hostIp))
            {
                UpdateStatusText("Please enter a valid Host IP.");
                return;
            }

            _activePin = SunshineCrypto.GeneratePin();
            if (pinModalRoot != null) pinModalRoot.SetActive(true);
            if (pinDisplayModalText != null)
            {
                pinDisplayModalText.text = $"ENTER PIN IN SUNSHINE WEB UI (PIN: {_activePin})\nOpen https://{hostIp}:47990 on PC";
            }

            UpdateStatusText($"Awaiting PIN confirmation for {_activePin}...");
            if (SunshineClient.Instance != null)
            {
                await SunshineClient.Instance.PairHostAsync(hostIp, _activePin);
            }
        }

        private void HandlePairingResult(bool success, string message)
        {
            if (pinModalRoot != null) pinModalRoot.SetActive(false);
            UpdateStatusText(message);
        }

        private async void OnStreamClicked()
        {
            string hostIp = ipInputField != null ? ipInputField.text.Trim() : "";
            _settings.savedHostIp = hostIp;
            _settings.Save();

            var config = new StreamConfig
            {
                width = _settings.resolutionWidth,
                height = _settings.resolutionHeight,
                fps = _settings.targetFps,
                bitrateKbps = _settings.bitrateKbps,
                codec = _settings.codec
            };

            UpdateStatusText("Starting streaming session...");
            if (SunshineClient.Instance != null)
            {
                bool ok = await SunshineClient.Instance.LaunchStreamAsync(hostIp, "1", config);
                if (ok)
                {
                    MoonlightInputManager.Instance?.InitializeChannel(hostIp, 47999, config.width, config.height);
                    MoonlightVideoReceiver.Instance?.StartStream(47998);
                }
            }
        }

        private void HandleStreamLaunched(bool success, string message)
        {
            UpdateStatusText(success ? "Connected & Streaming" : $"Launch Error: {message}");
        }

        private async void OnDisconnectClicked()
        {
            MoonlightVideoReceiver.Instance?.StopStream();
            MoonlightInputManager.Instance?.CloseConnection();
            if (SunshineClient.Instance != null && !string.IsNullOrEmpty(_settings.savedHostIp))
            {
                await SunshineClient.Instance.QuitStreamAsync(_settings.savedHostIp);
            }
            UpdateStatusText("Disconnected.");
        }

        private void OnRecenterClicked()
        {
            screenInteraction?.RecenterScreen();
        }

        private void OnBitrateChanged(float mbps)
        {
            _settings.bitrateKbps = Mathf.RoundToInt(mbps * 1000);
            if (bitrateText != null) bitrateText.text = $"{mbps:F0} Mbps";
        }

        private void OnCurveChanged(float angle)
        {
            _settings.curveAngle = angle;
            if (curveText != null) curveText.text = $"{angle:F0}°";
            screenGenerator?.SetCurvature(angle);
        }

        private void OnDistanceChanged(float dist)
        {
            _settings.screenDistance = dist;
            screenInteraction?.SetDistance(dist);
        }

        private void OnScaleChanged(float scale)
        {
            _settings.screenScale = scale;
            screenInteraction?.SetScale(scale);
        }

        private void OnPassthroughToggled(bool enable)
        {
            _settings.passthrough = enable;
            _settings.environmentIndex = enable ? (int)VREnvironmentType.Passthrough : (int)VREnvironmentType.CinemaVoid;
            if (environmentDropdown != null) environmentDropdown.value = _settings.environmentIndex;
            passthroughManager?.SetPassthrough(enable);
            if (VREnvironmentManager.Instance != null)
            {
                VREnvironmentManager.Instance.SetEnvironment((VREnvironmentType)_settings.environmentIndex);
            }
        }

        private void OnEnvironmentDropdownChanged(int index)
        {
            _settings.environmentIndex = index;
            _settings.passthrough = (index == 0);
            _settings.Save();

            if (passthroughToggle != null)
            {
                passthroughToggle.SetIsOnWithoutNotify(index == 0);
            }

            if (VREnvironmentManager.Instance != null)
            {
                VREnvironmentManager.Instance.SetEnvironment((VREnvironmentType)index);
            }

            if (environmentLabelText != null)
            {
                environmentLabelText.text = ((VREnvironmentType)index).ToString();
            }
        }

        private void OnNextEnvironmentClicked()
        {
            if (VREnvironmentManager.Instance != null)
            {
                VREnvironmentManager.Instance.NextEnvironment();
                int idx = (int)VREnvironmentManager.Instance.CurrentEnvironment;
                if (environmentDropdown != null) environmentDropdown.value = idx;
                _settings.environmentIndex = idx;
                _settings.passthrough = (idx == 0);
                _settings.Save();
            }
        }

        private void OnToggleKeyboardClicked()
        {
            if (VRVirtualKeyboard.Instance != null)
            {
                VRVirtualKeyboard.Instance.ToggleKeyboard();
            }
        }

        private void OnQuickDictationClicked()
        {
            if (VRDictationManager.Instance != null)
            {
                VRDictationManager.Instance.ToggleDictation();
            }
        }

        private void OnAboutClicked()
        {
            EnsureAboutModal();
            if (aboutModalRoot != null) aboutModalRoot.SetActive(true);
        }

        private void OnCloseAboutClicked()
        {
            if (aboutModalRoot != null) aboutModalRoot.SetActive(false);
        }

        private void EnsureAboutModal()
        {
            if (aboutModalRoot != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            aboutModalRoot = new GameObject("GPL_AboutModal");
            if (canvas != null) aboutModalRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = aboutModalRoot.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(560, 420);
            rootRt.anchoredPosition = new Vector2(0, 0);

            // Semi-transparent background card
            Image bgImg = aboutModalRoot.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);

            // Title & Content Text
            GameObject textObj = new GameObject("AboutText");
            textObj.transform.SetParent(aboutModalRoot.transform, false);
            aboutModalText = textObj.AddComponent<TextMeshProUGUI>();
            aboutModalText.fontSize = 13;
            aboutModalText.color = Color.white;
            aboutModalText.lineSpacing = 1.2f;
            aboutModalText.richText = true;
            aboutModalText.alignment = TextAlignmentOptions.TopLeft;

            aboutModalText.text = 
                "<b><size=16>🌌 MoonlightVR for Meta Quest 3</size></b> (v1.0.0)\n" +
                "<b>License:</b> GNU General Public License v3.0 (GPL-3.0)\n\n" +
                "This application is free software: you can redistribute it and/or modify " +
                "it under the terms of the GNU General Public License as published by " +
                "the Free Software Foundation, version 3.\n\n" +
                "<b>Source Code Delivery (Section 6, GPL-3.0):</b>\n" +
                "The complete corresponding source code is freely available at:\n" +
                "<color=#38bdf8><u>https://github.com/pharmacophobia/MoonlightVR-Unity</u></color>\n\n" +
                "<b>Upstream Projects & Credits:</b>\n" +
                "• <b>Moonlight GameStream</b> (GPL-3.0) — <color=#94a3b8>moonlight-stream.org</color>\n" +
                "• <b>Sunshine Server</b> (GPL-3.0) — <color=#94a3b8>app.sunshinevr.com</color>\n" +
                "• <b>Moonlight Android XR</b> (GPL-3.0) — <color=#94a3b8>Cameron Gilleece</color>";

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(510, 320);
            textRt.anchoredPosition = new Vector2(0, 30);

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseAboutButton");
            closeBtnObj.transform.SetParent(aboutModalRoot.transform, false);
            RectTransform btnRt = closeBtnObj.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(160, 40);
            btnRt.anchoredPosition = new Vector2(0, -170);

            Image btnImg = closeBtnObj.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.35f, 0.70f);

            aboutCloseButton = closeBtnObj.AddComponent<Button>();
            aboutCloseButton.onClick.AddListener(OnCloseAboutClicked);

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "Close";
            btnTmp.fontSize = 15;
            btnTmp.color = Color.white;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTextObj.GetComponent<RectTransform>().sizeDelta = btnRt.sizeDelta;
        }

        private void UpdateStatusText(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        #endregion
    }
}
