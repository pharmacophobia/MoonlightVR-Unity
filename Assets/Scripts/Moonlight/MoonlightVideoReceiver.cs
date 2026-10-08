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

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace MoonlightVR.Moonlight
{
    /// <summary>
    /// Handles the video streaming pipeline for the virtual desktop screen.
    /// Manages the video texture, RTP packet reception, hardware decoder bridge (on Android/Quest 3),
    /// and built-in procedural test stream generation for simulator mode.
    /// </summary>
    public class MoonlightVideoReceiver : MonoBehaviour
    {
        public static MoonlightVideoReceiver Instance { get; private set; }

        [Header("Target Display Material")]
        [SerializeField] private Material screenMaterial;
        [SerializeField] private string texturePropertyName = "_MainTex";

        [Header("Stream Properties")]
        [SerializeField] private int videoWidth = 1920;
        [SerializeField] private int videoHeight = 1080;
        [SerializeField] private int targetFps = 90;
        [SerializeField] private bool useSimulatorPattern = false;

        [Header("Telemetry")]
        [SerializeField] private int receivedFramesCount = 0;
        [SerializeField] private float currentStreamFps = 0f;
        [SerializeField] private bool isReceiving = false;

        private Texture2D _videoTexture;
        private Color32[] _simPixels;
        private Thread _networkWorkerThread;
        private volatile bool _isRunning = false;
        private UdpClient _rtpClient;
        private float _fpsTimer = 0f;
        private int _fpsCounter = 0;

        public Texture2D VideoTexture => _videoTexture;
        public bool IsReceiving => isReceiving;
        public float CurrentFps => currentStreamFps;

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

            InitializeVideoTexture(videoWidth, videoHeight);
        }

        private void Start()
        {
            if (useSimulatorPattern || Application.isEditor)
            {
                StartCoroutine(SimulatorStreamRoutine());
            }
        }

        private void OnDestroy()
        {
            StopStream();
        }

        public void InitializeVideoTexture(int width, int height)
        {
            videoWidth = width;
            videoHeight = height;

            if (_videoTexture != null)
            {
                Destroy(_videoTexture);
            }

            _videoTexture = new Texture2D(videoWidth, videoHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (screenMaterial != null)
            {
                screenMaterial.SetTexture(texturePropertyName, _videoTexture);
            }

            _simPixels = new Color32[videoWidth * videoHeight];
        }

        public void StartStream(int rtpPort = 47998)
        {
            StopStream();
            _isRunning = true;
            isReceiving = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Initialize Android MediaCodec zero-copy hardware bridge
            InitializeAndroidHardwareDecoder();
#else
            // Launch UDP RTP packet receiver thread
            _networkWorkerThread = new Thread(() => NetworkWorkerLoop(rtpPort))
            {
                IsBackground = true,
                Name = "MoonlightRtpReceiver"
            };
            _networkWorkerThread.Start();
#endif
        }

        public void StopStream()
        {
            _isRunning = false;
            isReceiving = false;

            if (_rtpClient != null)
            {
                try { _rtpClient.Close(); } catch { }
                _rtpClient = null;
            }

            if (_networkWorkerThread != null && _networkWorkerThread.IsAlive)
            {
                _networkWorkerThread.Join(500);
                _networkWorkerThread = null;
            }
        }

        private void NetworkWorkerLoop(int port)
        {
            try
            {
                _rtpClient = new UdpClient(port);
                IPEndPoint anyEp = new IPEndPoint(IPAddress.Any, port);

                while (_isRunning)
                {
                    if (_rtpClient.Available > 0)
                    {
                        byte[] packet = _rtpClient.Receive(ref anyEp);
                        receivedFramesCount++;
                        _fpsCounter++;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Debug.LogWarning($"[MoonlightVideoReceiver] RTP Receiver loop error: {ex.Message}");
                }
            }
        }

        private void Update()
        {
            _fpsTimer += Time.deltaTime;
            if (_fpsTimer >= 1.0f)
            {
                currentStreamFps = _fpsCounter / _fpsTimer;
                _fpsCounter = 0;
                _fpsTimer = 0f;
            }
        }

        /// <summary>
        /// Generates a realistic high-FPS desktop simulator pattern for testing in Unity Editor.
        /// </summary>
        private IEnumerator SimulatorStreamRoutine()
        {
            var wait = new WaitForSeconds(1.0f / targetFps);
            float time = 0f;

            while (true)
            {
                if (useSimulatorPattern || Application.isEditor)
                {
                    time += Time.deltaTime;
                    GenerateTestPattern(time);
                    _videoTexture.SetPixels32(_simPixels);
                    _videoTexture.Apply(false);
                    receivedFramesCount++;
                    _fpsCounter++;
                }
                yield return wait;
            }
        }

        private void GenerateTestPattern(float t)
        {
            int w = videoWidth;
            int h = videoHeight;
            byte scanlineOffset = (byte)((t * 120) % 255);

            // Render high-tech cyber desktop test pattern
            for (int y = 0; y < h; y += 4)
            {
                float ny = (float)y / h;
                byte bgBlue = (byte)(30 + 40 * ny);
                byte bgRed = (byte)(15 + 20 * Mathf.Sin(t + ny * 6));
                byte bgGreen = (byte)(20 + 25 * Mathf.Cos(t + ny * 6));

                int rowStart = y * w;
                for (int x = 0; x < w; x += 4)
                {
                    float nx = (float)x / w;
                    bool isGrid = (x % 64 == 0) || (y % 64 == 0);
                    byte r = isGrid ? (byte)90 : bgRed;
                    byte g = isGrid ? (byte)150 : bgGreen;
                    byte b = isGrid ? (byte)230 : bgBlue;

                    var c = new Color32(r, g, b, 255);
                    for (int dy = 0; dy < 4 && (y + dy) < h; dy++)
                    {
                        int row = (y + dy) * w;
                        for (int dx = 0; dx < 4 && (x + dx) < w; dx++)
                        {
                            _simPixels[row + x + dx] = c;
                        }
                    }
                }
            }
        }

        private void InitializeAndroidHardwareDecoder()
        {
            try
            {
                using var pluginClass = new AndroidJavaClass("com.pharmacophobia.moonlightvr.MoonlightDecoderBridge");
                if (pluginClass != null)
                {
                    int externalTexId = pluginClass.CallStatic<int>("createDecoderSurfaceTexture", videoWidth, videoHeight);
                    if (externalTexId != 0 && screenMaterial != null)
                    {
                        var externalTexture = Texture2D.CreateExternalTexture(
                            videoWidth,
                            videoHeight,
                            TextureFormat.RGBA32,
                            false,
                            false,
                            new IntPtr(externalTexId)
                        );
                        screenMaterial.SetTexture(texturePropertyName, externalTexture);
                        Debug.Log($"[MoonlightVideoReceiver] Bound Android external SurfaceTexture ID: {externalTexId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MoonlightVideoReceiver] Android native decoder initialization fallback: {ex.Message}");
            }
        }
    }
}
