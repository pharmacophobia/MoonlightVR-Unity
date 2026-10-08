using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonlightVR.Moonlight;
using MoonlightVR.VR;

namespace MoonlightVR.UI
{
    /// <summary>
    /// Interactive world-space 3D virtual keyboard for Meta Quest 3 with integrated
    /// voice dictation software.
    /// Provides full QWERTY laser-pointer/touch typing, direct text injection, and
    /// speech-to-text voice recognition into Sunshine host PC.
    /// </summary>
    public class VRVirtualKeyboard : MonoBehaviour
    {
        public static VRVirtualKeyboard Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject keyboardPanelRoot;
        [SerializeField] private TextMeshProUGUI previewDisplayText;
        [SerializeField] private TextMeshProUGUI dictationStatusText;
        [SerializeField] private Image dictationMicIcon;
        [SerializeField] private Image audioWaveformBar;
        [SerializeField] private Button dictationButton;

        [Header("Key Containers")]
        [SerializeField] private Transform keysContainer;

        [Header("Ergonomics & Placement")]
        [SerializeField] private float distanceFromHead = 0.65f;
        [SerializeField] private float heightOffset = -0.25f;
        [SerializeField] private float tiltAngle = 30f;
        [SerializeField] private bool followHmdOnOpen = true;

        [Header("State")]
        [SerializeField] private bool isShiftActive = false;
        [SerializeField] private bool isSymbolsMode = false;
        private string _inputBuffer = "";

        // Standard Windows Virtual-Key Codes
        private const short VK_BACK = 0x08;
        private const short VK_TAB = 0x09;
        private const short VK_RETURN = 0x0D;
        private const short VK_SPACE = 0x20;

        private readonly List<Button> _keyButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> _keyLabels = new List<TextMeshProUGUI>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (keyboardPanelRoot == null) keyboardPanelRoot = gameObject;

            SetupDictationEvents();
            BuildProceduralKeyboardIfEmpty();
            UpdatePreviewDisplay();
        }

        private void SetupDictationEvents()
        {
            if (dictationButton != null)
            {
                dictationButton.onClick.AddListener(OnDictationButtonClicked);
            }

            if (VRDictationManager.Instance != null)
            {
                VRDictationManager.Instance.OnStateChanged += HandleDictationStateChanged;
                VRDictationManager.Instance.OnPartialResult += HandleDictationPartial;
                VRDictationManager.Instance.OnFinalResult += HandleDictationFinal;
                VRDictationManager.Instance.OnAudioLevelChanged += HandleAudioWaveform;
            }
        }

        private void OnDestroy()
        {
            if (VRDictationManager.Instance != null)
            {
                VRDictationManager.Instance.OnStateChanged -= HandleDictationStateChanged;
                VRDictationManager.Instance.OnPartialResult -= HandleDictationPartial;
                VRDictationManager.Instance.OnFinalResult -= HandleDictationFinal;
                VRDictationManager.Instance.OnAudioLevelChanged -= HandleAudioWaveform;
            }
        }

        /// <summary>
        /// Toggles the virtual keyboard visibility.
        /// </summary>
        public void ToggleKeyboard()
        {
            bool willShow = !keyboardPanelRoot.activeSelf;
            SetKeyboardVisible(willShow);
        }

        public void SetKeyboardVisible(bool visible)
        {
            if (keyboardPanelRoot != null)
            {
                keyboardPanelRoot.SetActive(visible);
            }

            if (visible && followHmdOnOpen)
            {
                PositionInFrontOfUser();
            }
        }

        /// <summary>
        /// Positions the floating keyboard in comfortable ergonomic view in front of the headset.
        /// </summary>
        public void PositionInFrontOfUser()
        {
            Transform hmd = Camera.main != null ? Camera.main.transform : null;
            if (hmd == null) return;

            Vector3 forward = hmd.forward;
            forward.y = 0;
            forward.Normalize();
            if (forward == Vector3.zero) forward = Vector3.forward;

            Vector3 targetPos = hmd.position + forward * distanceFromHead + Vector3.up * heightOffset;
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(tiltAngle, 0, 0);
        }

        #region Key Input Processing

        public void OnKeyPress(string character)
        {
            if (string.IsNullOrEmpty(character)) return;

            char c = character[0];
            if (isShiftActive && char.IsLetter(c))
            {
                c = char.ToUpper(c);
            }

            _inputBuffer += c;
            UpdatePreviewDisplay();

            // Send to host Sunshine PC
            if (MoonlightInputManager.Instance != null && MoonlightInputManager.Instance.IsConnected)
            {
                // Send standard Windows virtual-key or character string
                if (char.IsLetter(c))
                {
                    short vk = (short)char.ToUpper(c);
                    MoonlightInputManager.Instance.SendKeyTap(vk);
                }
                else
                {
                    MoonlightInputManager.Instance.SendTextString(c.ToString());
                }
            }

            if (isShiftActive)
            {
                isShiftActive = false;
                UpdateKeyLabels();
            }
        }

        public void OnBackspacePress()
        {
            if (_inputBuffer.Length > 0)
            {
                _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                UpdatePreviewDisplay();
            }

            if (MoonlightInputManager.Instance != null && MoonlightInputManager.Instance.IsConnected)
            {
                MoonlightInputManager.Instance.SendKeyTap(VK_BACK);
            }
        }

        public void OnSpacePress()
        {
            _inputBuffer += " ";
            UpdatePreviewDisplay();

            if (MoonlightInputManager.Instance != null && MoonlightInputManager.Instance.IsConnected)
            {
                MoonlightInputManager.Instance.SendKeyTap(VK_SPACE);
            }
        }

        public void OnEnterPress()
        {
            if (MoonlightInputManager.Instance != null && MoonlightInputManager.Instance.IsConnected)
            {
                MoonlightInputManager.Instance.SendKeyTap(VK_RETURN);
            }
            _inputBuffer = "";
            UpdatePreviewDisplay();
        }

        public void OnShiftToggle()
        {
            isShiftActive = !isShiftActive;
            UpdateKeyLabels();
        }

        public void OnSymbolsToggle()
        {
            isSymbolsMode = !isSymbolsMode;
            UpdateKeyLabels();
        }

        public void ClearBuffer()
        {
            _inputBuffer = "";
            UpdatePreviewDisplay();
        }

        private void UpdatePreviewDisplay()
        {
            if (previewDisplayText != null)
            {
                previewDisplayText.text = string.IsNullOrEmpty(_inputBuffer) ? "<color=#64748b>Type or tap 🎙️ to dictate...</color>" : _inputBuffer + "<color=#38bdf8>|</color>";
            }
        }

        #endregion

        #region Dictation Software Integration

        private void OnDictationButtonClicked()
        {
            if (VRDictationManager.Instance != null)
            {
                VRDictationManager.Instance.ToggleDictation();
            }
            else
            {
                Debug.LogWarning("[VRVirtualKeyboard] VRDictationManager instance not found in scene.");
            }
        }

        private void HandleDictationStateChanged(DictationState state, string message)
        {
            if (dictationStatusText != null)
            {
                dictationStatusText.text = message;
            }

            if (dictationMicIcon != null)
            {
                switch (state)
                {
                    case DictationState.Listening:
                        dictationMicIcon.color = new Color(0.9f, 0.2f, 0.2f); // Red active recording
                        break;
                    case DictationState.Processing:
                        dictationMicIcon.color = new Color(0.9f, 0.7f, 0.1f); // Yellow transcribing
                        break;
                    case DictationState.Idle:
                    default:
                        dictationMicIcon.color = new Color(0.2f, 0.8f, 0.4f); // Green ready
                        break;
                }
            }
        }

        private void HandleDictationPartial(string partial)
        {
            if (dictationStatusText != null)
            {
                dictationStatusText.text = $"🎙️ \"{partial}...\"";
            }
        }

        private void HandleDictationFinal(string text)
        {
            _inputBuffer += (_inputBuffer.Length > 0 ? " " : "") + text;
            UpdatePreviewDisplay();

            if (dictationStatusText != null)
            {
                dictationStatusText.text = $"Transcribed: \"{text}\"";
            }
        }

        private void HandleAudioWaveform(float level)
        {
            if (audioWaveformBar != null)
            {
                audioWaveformBar.transform.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 2.0f, level), 1f);
            }
        }

        #endregion

        #region Procedural UI Hierarchy Builder

        private void BuildProceduralKeyboardIfEmpty()
        {
            if (keysContainer != null && keysContainer.childCount > 0) return;

            // Create Canvas and background frame if missing
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("KeyboardCanvas");
                canvasObj.transform.SetParent(transform);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                RectTransform rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(800, 360);
                rt.localScale = Vector3.one * 0.001f; // Scaled to ~0.8m width in VR space
                canvasObj.transform.localPosition = Vector3.zero;
            }

            // Background dark panel
            GameObject bgPanel = new GameObject("BgPanel");
            bgPanel.transform.SetParent(canvas.transform, false);
            Image bgImg = bgPanel.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            RectTransform bgRt = bgPanel.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(780, 340);

            // Top Header: Preview text & Dictation Status bar
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(bgPanel.transform, false);
            RectTransform topRt = topBar.AddComponent<RectTransform>();
            topRt.sizeDelta = new Vector2(740, 60);
            topRt.anchoredPosition = new Vector2(0, 125);

            // Preview Text Box
            GameObject previewObj = new GameObject("PreviewText");
            previewObj.transform.SetParent(topBar.transform, false);
            previewDisplayText = previewObj.AddComponent<TextMeshProUGUI>();
            previewDisplayText.fontSize = 22;
            previewDisplayText.color = Color.white;
            previewDisplayText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform prevRt = previewObj.GetComponent<RectTransform>();
            prevRt.sizeDelta = new Vector2(500, 50);
            prevRt.anchoredPosition = new Vector2(-110, 0);

            // Dictation Status Indicator
            GameObject statusObj = new GameObject("DictationStatus");
            statusObj.transform.SetParent(topBar.transform, false);
            dictationStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            dictationStatusText.fontSize = 16;
            dictationStatusText.color = new Color(0.58f, 0.65f, 0.75f);
            dictationStatusText.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform statRt = statusObj.GetComponent<RectTransform>();
            statRt.sizeDelta = new Vector2(200, 50);
            statRt.anchoredPosition = new Vector2(250, 0);

            // Keys Container
            GameObject keysRoot = new GameObject("KeysContainer");
            keysRoot.transform.SetParent(bgPanel.transform, false);
            keysContainer = keysRoot.transform;
            RectTransform keysRt = keysRoot.AddComponent<RectTransform>();
            keysRt.sizeDelta = new Vector2(740, 240);
            keysRt.anchoredPosition = new Vector2(0, -35);

            BuildKeyboardKeyRows(keysRoot.transform);
        }

        private void BuildKeyboardKeyRows(Transform container)
        {
            string[][] rows = new string[][]
            {
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" },
                new string[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
                new string[] { "a", "s", "d", "f", "g", "h", "j", "k", "l" },
                new string[] { "Shift", "z", "x", "c", "v", "b", "n", "m", "⌫" },
                new string[] { "?123", "🎙️ Dictate", "Space", "↵ Enter", "✕" }
            };

            float startY = 80f;
            float rowSpacing = 46f;

            for (int r = 0; r < rows.Length; r++)
            {
                string[] row = rows[r];
                float y = startY - r * rowSpacing;
                int count = row.Length;
                float keyWidth = 54f;
                float keySpacing = 60f;
                float startX = -((count - 1) * keySpacing) * 0.5f;

                for (int c = 0; c < count; c++)
                {
                    string label = row[c];
                    float currentWidth = keyWidth;
                    Color btnColor = new Color(0.15f, 0.20f, 0.30f);

                    // Special wide button layouts
                    if (label == "Shift" || label == "⌫") currentWidth = 80f;
                    else if (label == "Space") currentWidth = 180f;
                    else if (label == "🎙️ Dictate")
                    {
                        currentWidth = 130f;
                        btnColor = new Color(0.12f, 0.45f, 0.35f); // Emerald green for dictation
                    }
                    else if (label == "↵ Enter")
                    {
                        currentWidth = 90f;
                        btnColor = new Color(0.15f, 0.35f, 0.70f); // Bright blue for enter
                    }

                    CreateKeyButton(container, label, new Vector2(startX + c * keySpacing, y), new Vector2(currentWidth, 40f), btnColor);
                }
            }
        }

        private void CreateKeyButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor)
        {
            GameObject btnObj = new GameObject($"Key_{label}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.3f;
            cb.pressedColor = bgColor * 0.7f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = label.Length > 2 ? 14 : 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = size;

            _keyButtons.Add(btn);
            _keyLabels.Add(tmp);

            // Wire action
            if (label == "Shift")
            {
                btn.onClick.AddListener(OnShiftToggle);
            }
            else if (label == "⌫")
            {
                btn.onClick.AddListener(OnBackspacePress);
            }
            else if (label == "Space")
            {
                btn.onClick.AddListener(OnSpacePress);
            }
            else if (label == "↵ Enter")
            {
                btn.onClick.AddListener(OnEnterPress);
            }
            else if (label == "?123")
            {
                btn.onClick.AddListener(OnSymbolsToggle);
            }
            else if (label == "🎙️ Dictate")
            {
                dictationButton = btn;
                dictationMicIcon = img;
                btn.onClick.AddListener(OnDictationButtonClicked);
            }
            else if (label == "✕")
            {
                btn.onClick.AddListener(() => SetKeyboardVisible(false));
            }
            else
            {
                string keyChar = label;
                btn.onClick.AddListener(() => OnKeyPress(keyChar));
            }
        }

        private void UpdateKeyLabels()
        {
            for (int i = 0; i < _keyLabels.Count; i++)
            {
                string original = _keyLabels[i].text;
                if (original.Length == 1 && char.IsLetter(original[0]))
                {
                    _keyLabels[i].text = isShiftActive ? original.ToUpper() : original.ToLower();
                }
            }
        }

        #endregion
    }
}
