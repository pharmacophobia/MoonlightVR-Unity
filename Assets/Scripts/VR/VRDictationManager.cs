using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonlightVR.Moonlight;

namespace MoonlightVR.VR
{
    public enum DictationState
    {
        Idle,
        Listening,
        Processing,
        Error
    }

    /// <summary>
    /// Voice dictation engine for the Quest 3 virtual keyboard.
    /// Provides low-latency speech-to-text powered by Android/Meta Quest SpeechRecognizer
    /// on device, with a Unity Microphone & simulation fallback for PC / Editor testing.
    /// Injects recognized words and punctuation straight into Sunshine via MoonlightInputManager.
    /// </summary>
    public class VRDictationManager : MonoBehaviour
    {
        public static VRDictationManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool autoSendToHost = true;
        [SerializeField] private bool autoPunctuation = true;
        [SerializeField] private float silenceTimeout = 3.0f;

        [Header("Runtime State")]
        [SerializeField] private DictationState currentState = DictationState.Idle;
        [SerializeField] private string lastTranscribedText = "";

        public DictationState CurrentState => currentState;
        public string LastTranscribedText => lastTranscribedText;
        public bool IsListening => currentState == DictationState.Listening;

        // Events
        public event Action<DictationState, string> OnStateChanged;
        public event Action<string> OnPartialResult;
        public event Action<string> OnFinalResult;
        public event Action<float> OnAudioLevelChanged; // 0.0 to 1.0 for visualizer waves

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _speechRecognizer;
        private AndroidJavaObject _recognizerIntent;
        private AndroidSpeechListenerProxy _listenerProxy;
#endif

        private AudioClip _editorMicClip;
        private string _editorMicDevice;
        private Coroutine _listeningCoroutine;

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
            InitializePlatformRecognizer();
        }

        private void InitializePlatformRecognizer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        var speechRecognizerClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
                        _speechRecognizer = speechRecognizerClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", currentActivity);

                        _listenerProxy = new AndroidSpeechListenerProxy(this);
                        _speechRecognizer.Call("setRecognitionListener", _listenerProxy);

                        var intentClass = new AndroidJavaClass("android.content.Intent");
                        var recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");

                        _recognizerIntent = new AndroidJavaObject("android.content.Intent", recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH"));
                        _recognizerIntent.Call<AndroidJavaObject>("putExtra", recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE_MODEL"), recognizerIntentClass.GetStatic<string>("LANGUAGE_MODEL_FREE_FORM"));
                        _recognizerIntent.Call<AndroidJavaObject>("putExtra", recognizerIntentClass.GetStatic<string>("EXTRA_PARTIAL_RESULTS"), true);
                        _recognizerIntent.Call<AndroidJavaObject>("putExtra", recognizerIntentClass.GetStatic<string>("EXTRA_MAX_RESULTS"), 1);

                        Debug.Log("[VRDictationManager] Initialized Android SpeechRecognizer on Quest 3");
                    }));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VRDictationManager] Failed to initialize Android SpeechRecognizer: {ex.Message}");
            }
#else
            if (Microphone.devices.Length > 0)
            {
                _editorMicDevice = Microphone.devices[0];
                Debug.Log($"[VRDictationManager] Initialized PC microphone fallback: {_editorMicDevice}");
            }
            else
            {
                Debug.LogWarning("[VRDictationManager] No physical microphone detected on PC. Editor test mode active.");
            }
#endif
        }

        /// <summary>
        /// Toggles dictation recording on or off.
        /// </summary>
        public void ToggleDictation()
        {
            if (IsListening)
            {
                StopDictation();
            }
            else
            {
                StartDictation();
            }
        }

        /// <summary>
        /// Starts voice recording and speech recognition.
        /// </summary>
        public void StartDictation()
        {
            if (currentState == DictationState.Listening) return;

            SetState(DictationState.Listening, "Listening...");

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_speechRecognizer != null && _recognizerIntent != null)
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            _speechRecognizer.Call("startListening", _recognizerIntent);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[VRDictationManager] startListening error: {ex.Message}");
                            SetState(DictationState.Error, ex.Message);
                        }
                    }));
                }
            }
#else
            // Editor / PC Fallback Simulation & Mic Loop
            if (_listeningCoroutine != null) StopCoroutine(_listeningCoroutine);
            _listeningCoroutine = StartCoroutine(EditorSimulatedMicRoutine());
#endif
        }

        /// <summary>
        /// Stops voice dictation and finalizes the transcribed speech.
        /// </summary>
        public void StopDictation()
        {
            if (currentState != DictationState.Listening) return;

            SetState(DictationState.Processing, "Processing speech...");

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_speechRecognizer != null)
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            _speechRecognizer.Call("stopListening");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[VRDictationManager] stopListening error: {ex.Message}");
                        }
                    }));
                }
            }
#else
            if (_listeningCoroutine != null)
            {
                StopCoroutine(_listeningCoroutine);
                _listeningCoroutine = null;
            }
            if (!string.IsNullOrEmpty(_editorMicDevice) && Microphone.IsRecording(_editorMicDevice))
            {
                Microphone.End(_editorMicDevice);
            }
            // Emit final result in editor
            string phrase = string.IsNullOrEmpty(lastTranscribedText) ? "Hello from Quest 3 VR dictation" : lastTranscribedText;
            HandleSpeechResult(phrase);
#endif
        }

        /// <summary>
        /// Processes and formats transcribed speech, then sends it to Sunshine if autoSend is enabled.
        /// </summary>
        public void HandleSpeechResult(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                SetState(DictationState.Idle, "Ready");
                return;
            }

            string formatted = autoPunctuation ? FormatSpokenPunctuation(rawText) : rawText;
            lastTranscribedText = formatted;

            Debug.Log($"[VRDictationManager] Transcribed: \"{formatted}\"");
            OnFinalResult?.Invoke(formatted);

            if (autoSendToHost && MoonlightInputManager.Instance != null && MoonlightInputManager.Instance.IsConnected)
            {
                // Injects text straight into Sunshine host Windows/Linux desktop!
                MoonlightInputManager.Instance.SendTextString(formatted + " ");
            }

            SetState(DictationState.Idle, "Ready");
        }

        public void HandlePartialResult(string partial)
        {
            lastTranscribedText = partial;
            OnPartialResult?.Invoke(partial);
        }

        public void ReportAudioLevel(float level)
        {
            OnAudioLevelChanged?.Invoke(Mathf.Clamp01(level));
        }

        private void SetState(DictationState state, string message)
        {
            currentState = state;
            OnStateChanged?.Invoke(state, message);
        }

        /// <summary>
        /// Formats common spoken voice punctuation keywords into real symbols.
        /// </summary>
        private string FormatSpokenPunctuation(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            string text = input;
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(period|full stop)\b", ".", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(comma)\b", ",", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(question mark)\b", "?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(exclamation mark|exclamation point)\b", "!", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(colon)\b", ":", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(semicolon)\b", ";", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\b(new line|newline)\b", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return text;
        }

        private IEnumerator EditorSimulatedMicRoutine()
        {
            if (!string.IsNullOrEmpty(_editorMicDevice))
            {
                _editorMicClip = Microphone.Start(_editorMicDevice, true, 10, 44100);
            }

            float timer = 0f;
            float[] sampleBuffer = new float[128];

            while (timer < silenceTimeout + 2f)
            {
                timer += Time.deltaTime;

                // Audio level waveform detection
                if (_editorMicClip != null)
                {
                    int micPos = Microphone.GetPosition(_editorMicDevice);
                    if (micPos > 128)
                    {
                        try
                        {
                            _editorMicClip.GetData(sampleBuffer, micPos - 128);
                            float sum = 0f;
                            for (int i = 0; i < sampleBuffer.Length; i++) sum += Mathf.Abs(sampleBuffer[i]);
                            ReportAudioLevel(sum / sampleBuffer.Length * 10f);
                        }
                        catch (Exception)
                        {
                            ReportAudioLevel(Mathf.PingPong(Time.time * 2f, 1f));
                        }
                    }
                }
                else
                {
                    // Synthesize pulse wave for UI visualizer
                    ReportAudioLevel(Mathf.PingPong(Time.time * 2f, 1f));
                }

                yield return null;
            }

            StopDictation();
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_speechRecognizer != null)
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            _speechRecognizer.Call("destroy");
                            _speechRecognizer = null;
                        }
                        catch (Exception) { }
                    }));
                }
            }
#endif
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android RecognitionListener callback proxy receiving native speech events from Meta Quest OS.
    /// </summary>
    public class AndroidSpeechListenerProxy : AndroidJavaProxy
    {
        private readonly VRDictationManager _manager;

        public AndroidSpeechListenerProxy(VRDictationManager manager) 
            : base("android.speech.RecognitionListener")
        {
            _manager = manager;
        }

        public void onReadyForSpeech(AndroidJavaObject @params) { }
        public void onBeginningOfSpeech() { }

        public void onRmsChanged(float rmsdB)
        {
            // Normalize RMS dB (-2.0 to 10.0 typical range) to 0.0 .. 1.0
            float normalized = Mathf.Clamp01((rmsdB + 2.0f) / 12.0f);
            _manager.ReportAudioLevel(normalized);
        }

        public void onBufferReceived(byte[] buffer) { }
        public void onEndOfSpeech() { }

        public void onError(int error)
        {
            Debug.LogWarning($"[AndroidSpeechListenerProxy] SpeechRecognizer error code: {error}");
            _manager.StopDictation();
        }

        public void onResults(AndroidJavaObject results)
        {
            try
            {
                var recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");
                string key = recognizerIntentClass.GetStatic<string>("EXTRA_RESULTS");
                var matches = results.Call<AndroidJavaObject>("getStringArrayList", key);
                if (matches != null)
                {
                    int size = matches.Call<int>("size");
                    if (size > 0)
                    {
                        string recognized = matches.Call<string>("get", 0);
                        _manager.HandleSpeechResult(recognized);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AndroidSpeechListenerProxy] onResults parse error: {ex.Message}");
            }
            _manager.HandleSpeechResult("");
        }

        public void onPartialResults(AndroidJavaObject partialResults)
        {
            try
            {
                var recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");
                string key = recognizerIntentClass.GetStatic<string>("EXTRA_RESULTS");
                var matches = partialResults.Call<AndroidJavaObject>("getStringArrayList", key);
                if (matches != null && matches.Call<int>("size") > 0)
                {
                    string partial = matches.Call<string>("get", 0);
                    _manager.HandlePartialResult(partial);
                }
            }
            catch (Exception) { }
        }

        public void onEvent(int eventType, AndroidJavaObject @params) { }
    }
#endif
}
