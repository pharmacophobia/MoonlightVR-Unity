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

        [Header("Diagnostics")]
        [SerializeField] private TextMeshProUGUI fpsDiagnosticsText;

        [Header("Scene References")]
        [SerializeField] private CurvedScreenGenerator screenGenerator;
        [SerializeField] private SpatialScreenInteraction screenInteraction;
        [SerializeField] private MetaPassthroughManager passthroughManager;

        private SettingsData _settings;
        private string _activePin = "";

        private void Start()
        {
            _settings = SettingsData.Load();
            ApplyLoadedSettings();
            RegisterEventHandlers();
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
            passthroughManager?.SetPassthrough(enable);
        }

        private void UpdateStatusText(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        #endregion
    }
}
