using System;
using System.IO;
using UnityEngine;

namespace MoonlightVR.Moonlight
{
    public enum VideoCodec
    {
        H264,
        HEVC,
        AV1
    }

    [System.Serializable]
    public class StreamConfig
    {
        public int width = 1920;
        public int height = 1080;
        public int fps = 90;
        public int bitrateKbps = 35000;
        public VideoCodec codec = VideoCodec.HEVC;
        public bool enableAudio = true;
        public bool hdr = false;

        public static StreamConfig DefaultQuest3 => new StreamConfig
        {
            width = 2560,
            height = 1440,
            fps = 90,
            bitrateKbps = 45000,
            codec = VideoCodec.HEVC,
            enableAudio = true,
            hdr = false
        };
    }

    /// <summary>
    /// Binary packet structures for the Moonlight / Sunshine UDP Control and RTP stream protocols.
    /// </summary>
    public static class MoonlightPacketProtocol
    {
        // Control Packet Types
        public const int PACKET_TYPE_MOUSE_MOVE = 0x00000005;
        public const int PACKET_TYPE_MOUSE_BUTTON = 0x00000007;
        public const int PACKET_TYPE_MOUSE_SCROLL = 0x00000009;
        public const int PACKET_TYPE_KEYBOARD = 0x0000000A;
        public const int PACKET_TYPE_GAMEPAD = 0x0000000C;
        public const int PACKET_TYPE_UTF8_TEXT = 0x00000017;

        // Mouse Buttons
        public const byte BUTTON_LEFT = 0x01;
        public const byte BUTTON_MIDDLE = 0x02;
        public const byte BUTTON_RIGHT = 0x03;

        // Button Actions
        public const byte ACTION_PRESS = 0x01;
        public const byte ACTION_RELEASE = 0x02;

        // Gamepad Buttons (XInput / Xbox standard mask)
        public const ushort GAMEPAD_DPAD_UP = 0x0001;
        public const ushort GAMEPAD_DPAD_DOWN = 0x0002;
        public const ushort GAMEPAD_DPAD_LEFT = 0x0004;
        public const ushort GAMEPAD_DPAD_RIGHT = 0x0008;
        public const ushort GAMEPAD_START = 0x0010;
        public const ushort GAMEPAD_BACK = 0x0020;
        public const ushort GAMEPAD_LEFT_THUMB = 0x0040;
        public const ushort GAMEPAD_RIGHT_THUMB = 0x0080;
        public const ushort GAMEPAD_LEFT_SHOULDER = 0x0100;
        public const ushort GAMEPAD_RIGHT_SHOULDER = 0x0200;
        public const ushort GAMEPAD_A = 0x1000;
        public const ushort GAMEPAD_B = 0x2000;
        public const ushort GAMEPAD_X = 0x4000;
        public const ushort GAMEPAD_Y = 0x8000;

        /// <summary>
        /// Creates a Moonlight Mouse Move packet (Big-Endian integer packing).
        /// </summary>
        public static byte[] CreateMouseMovePacket(short x, short y)
        {
            byte[] packet = new byte[10];
            WriteInt32BE(packet, 0, PACKET_TYPE_MOUSE_MOVE);
            WriteInt16BE(packet, 4, 6); // payload length
            WriteInt16BE(packet, 6, x);
            WriteInt16BE(packet, 8, y);
            return packet;
        }

        /// <summary>
        /// Creates a Moonlight Mouse Button packet (Press or Release).
        /// </summary>
        public static byte[] CreateMouseButtonPacket(byte button, byte action)
        {
            byte[] packet = new byte[8];
            WriteInt32BE(packet, 0, PACKET_TYPE_MOUSE_BUTTON);
            WriteInt16BE(packet, 4, 2); // payload length
            packet[6] = button;
            packet[7] = action;
            return packet;
        }

        /// <summary>
        /// Creates a Moonlight Mouse Vertical Scroll packet.
        /// </summary>
        public static byte[] CreateMouseScrollPacket(short scrollDelta)
        {
            byte[] packet = new byte[8];
            WriteInt32BE(packet, 0, PACKET_TYPE_MOUSE_SCROLL);
            WriteInt16BE(packet, 4, 2);
            WriteInt16BE(packet, 6, scrollDelta);
            return packet;
        }

        /// <summary>
        /// Creates a Moonlight Keyboard Event packet.
        /// </summary>
        public static byte[] CreateKeyboardPacket(short keyCode, bool isKeyDown)
        {
            byte[] packet = new byte[10];
            WriteInt32BE(packet, 0, PACKET_TYPE_KEYBOARD);
            WriteInt16BE(packet, 4, 4);
            packet[6] = (byte)(isKeyDown ? 0x03 : 0x04); // KeyAction
            packet[7] = 0; // KeyModifiers
            WriteInt16BE(packet, 8, keyCode);
            return packet;
        }

        /// <summary>
        /// Creates a Moonlight Gamepad packet (Xbox 360 controller layout).
        /// </summary>
        public static byte[] CreateGamepadPacket(
            ushort buttonMask,
            byte leftTrigger,
            byte rightTrigger,
            short leftStickX,
            short leftStickY,
            short rightStickX,
            short rightStickY)
        {
            byte[] packet = new byte[20];
            WriteInt32BE(packet, 0, PACKET_TYPE_GAMEPAD);
            WriteInt16BE(packet, 4, 14); // payload length
            WriteUInt16BE(packet, 6, buttonMask);
            packet[8] = leftTrigger;
            packet[9] = rightTrigger;
            WriteInt16BE(packet, 10, leftStickX);
            WriteInt16BE(packet, 12, leftStickY);
            WriteInt16BE(packet, 14, rightStickX);
            WriteInt16BE(packet, 16, rightStickY);
            return packet;
        }

        /// <summary>
        /// Creates a Sunshine UTF-8 text injection packet for dictation and string inputs.
        /// </summary>
        public static byte[] CreateUtf8TextPacket(string text)
        {
            if (string.IsNullOrEmpty(text)) text = "";
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            short payloadLen = (short)textBytes.Length;

            byte[] packet = new byte[6 + payloadLen];
            WriteInt32BE(packet, 0, PACKET_TYPE_UTF8_TEXT);
            WriteInt16BE(packet, 4, payloadLen);
            Buffer.BlockCopy(textBytes, 0, packet, 6, payloadLen);
            return packet;
        }

        #region Byte Packing Helpers (Network Big-Endian)

        public static void WriteInt32BE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        public static void WriteInt16BE(byte[] buffer, int offset, short value)
        {
            buffer[offset] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        public static void WriteUInt16BE(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        #endregion
    }
}
