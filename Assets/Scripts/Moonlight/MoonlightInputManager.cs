using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace MoonlightVR.Moonlight
{
    /// <summary>
    /// Manages the low-latency UDP control input channel to Sunshine.
    /// Serializes and sends mouse, keyboard, and virtual Xbox gamepad events.
    /// </summary>
    public class MoonlightInputManager : MonoBehaviour
    {
        public static MoonlightInputManager Instance { get; private set; }

        [Header("Network Configuration")]
        [SerializeField] private string hostIp = "192.168.1.100";
        [SerializeField] private int controlPort = 47999;

        [Header("Host Resolution")]
        [SerializeField] private int hostScreenWidth = 1920;
        [SerializeField] private int hostScreenHeight = 1080;

        private UdpClient _udpClient;
        private IPEndPoint _remoteEndpoint;
        private bool _isConnected = false;

        public bool IsConnected => _isConnected;
        public int HostScreenWidth => hostScreenWidth;
        public int HostScreenHeight => hostScreenHeight;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            CloseConnection();
        }

        public void InitializeChannel(string ip, int port, int width, int height)
        {
            CloseConnection();

            hostIp = ip;
            controlPort = port;
            hostScreenWidth = width;
            hostScreenHeight = height;

            try
            {
                _udpClient = new UdpClient();
                _remoteEndpoint = new IPEndPoint(IPAddress.Parse(hostIp), controlPort);
                _isConnected = true;
                Debug.Log($"[MoonlightInputManager] Initialized control channel to {hostIp}:{controlPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MoonlightInputManager] Failed to bind UDP control socket: {ex.Message}");
                _isConnected = false;
            }
        }

        public void CloseConnection()
        {
            _isConnected = false;
            _udpClient?.Close();
            _udpClient = null;
        }

        /// <summary>
        /// Sends normalized screen UV coordinates (0.0 to 1.0) converted to host pixel space.
        /// </summary>
        public void SendNormalizedMouseMove(float u, float v)
        {
            if (!_isConnected) return;

            // Clamping UV to valid bounds
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            // Windows desktop coordinates: (0,0) is top-left, while UV (0,0) is bottom-left
            short pixelX = (short)Mathf.RoundToInt(u * hostScreenWidth);
            short pixelY = (short)Mathf.RoundToInt((1.0f - v) * hostScreenHeight);

            byte[] packet = MoonlightPacketProtocol.CreateMouseMovePacket(pixelX, pixelY);
            SendPacket(packet);
        }

        /// <summary>
        /// Sends mouse button press or release.
        /// </summary>
        public void SendMouseButton(byte button, bool isDown)
        {
            if (!_isConnected) return;
            byte action = isDown ? MoonlightPacketProtocol.ACTION_PRESS : MoonlightPacketProtocol.ACTION_RELEASE;
            byte[] packet = MoonlightPacketProtocol.CreateMouseButtonPacket(button, action);
            SendPacket(packet);
        }

        /// <summary>
        /// Sends vertical mouse scroll ticks.
        /// </summary>
        public void SendMouseScroll(short scrollDelta)
        {
            if (!_isConnected) return;
            byte[] packet = MoonlightPacketProtocol.CreateMouseScrollPacket(scrollDelta);
            SendPacket(packet);
        }

        /// <summary>
        /// Sends Windows virtual key code event.
        /// </summary>
        public void SendKeyboardKey(short virtualKeyCode, bool isDown)
        {
            if (!_isConnected) return;
            byte[] packet = MoonlightPacketProtocol.CreateKeyboardPacket(virtualKeyCode, isDown);
            SendPacket(packet);
        }

        /// <summary>
        /// Sends a key tap (KeyDown followed by KeyUp).
        /// </summary>
        public void SendKeyTap(short virtualKeyCode)
        {
            SendKeyboardKey(virtualKeyCode, true);
            SendKeyboardKey(virtualKeyCode, false);
        }

        /// <summary>
        /// Injects an entire text string (such as transcribed voice dictation) into Sunshine.
        /// </summary>
        public void SendTextString(string text)
        {
            if (!_isConnected || string.IsNullOrEmpty(text)) return;
            byte[] packet = MoonlightPacketProtocol.CreateUtf8TextPacket(text);
            SendPacket(packet);
        }

        /// <summary>
        /// Sends complete virtual Xbox gamepad controller state.
        /// </summary>
        public void SendGamepadState(
            ushort buttonMask,
            byte leftTrigger,
            byte rightTrigger,
            short leftStickX,
            short leftStickY,
            short rightStickX,
            short rightStickY)
        {
            if (!_isConnected) return;
            byte[] packet = MoonlightPacketProtocol.CreateGamepadPacket(
                buttonMask,
                leftTrigger,
                rightTrigger,
                leftStickX,
                leftStickY,
                rightStickX,
                rightStickY);
            SendPacket(packet);
        }

        private void SendPacket(byte[] data)
        {
            if (_udpClient == null || _remoteEndpoint == null) return;
            try
            {
                _udpClient.Send(data, data.Length, _remoteEndpoint);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MoonlightInputManager] Packet send error: {ex.Message}");
            }
        }
    }
}
