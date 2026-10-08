# 🌌 MoonlightVR for Meta Quest 3

> **High-Performance Spatial Remote Desktop & GameStream Client built with Unity & OpenXR for Meta Quest 3, powered by Sunshine and Moonlight.**

---

## 📥 Direct APK Download & Install

| Direct 1-Tap Download | Camera-Scannable Mobile QR Code | Official GitHub Release |
| :---: | :---: | :---: |
| [![Download APK](https://img.shields.io/badge/Download-MoonlightVR--Unity.apk-brightgreen?style=for-the-badge&logo=android&logoColor=white)](https://github.com/pharmacophobia/MoonlightVR-Unity/raw/main/MoonlightVR-Unity.apk) <br><br> **Direct Raw Link (98.4 MB)**: <br> [`MoonlightVR-Unity.apk`](https://github.com/pharmacophobia/MoonlightVR-Unity/raw/main/MoonlightVR-Unity.apk) | ![MoonlightVR APK QR Code](https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=https://github.com/pharmacophobia/MoonlightVR-Unity/raw/main/MoonlightVR-Unity.apk) <br> *(Point your phone or Quest browser camera here to download)* | [![Release v1.0.0](https://img.shields.io/badge/Release-v1.0.0-blue?style=for-the-badge&logo=github)](https://github.com/pharmacophobia/MoonlightVR-Unity/releases/tag/v1.0.0) <br><br> **Release Binary**: <br> [Download v1.0.0 Asset](https://github.com/pharmacophobia/MoonlightVR-Unity/releases/download/v1.0.0/MoonlightVR-Unity.apk) |

### Quick Sideloading via ADB
```bash
# Connect Quest 3 via USB or Wi-Fi ADB and run:
adb install -r MoonlightVR-Unity.apk
```

---

## ✨ Features

- 🖥️ **Procedural Curved Virtual Monitor**: Real-time curvature adjustment from flat plane (0°) to deep cockpit wrap-around (120° FOV).
- 👁️ **Meta Quest 3 Mixed Reality Passthrough**: Seamlessly toggle between full MR passthrough (floating PC screen over your physical desk & keyboard) and an immersive dark cinema void.
- 🎯 **XR Laser Pointer Desktop Interaction**: High-precision raycasting from Quest Touch Plus controllers or hand tracking with automatic UV mapping to Windows desktop resolution.
  - **Index Trigger**: Left Mouse Click & Drag
  - **Hand Grip**: Right Mouse Click
  - **Thumbstick Y**: Smooth Mouse Scroll Wheel
- 🎮 **Virtual Gamepad Emulation**: Native conversion of Quest Touch Plus controller thumbsticks and buttons into virtual Xbox 360 controller packets sent to Sunshine.
- ⚡ **Snapdragon XR2 Gen 2 Hardware Decoding**: Zero-copy `MediaCodec` and `SurfaceTexture` hardware decoder bridge delivering 4K 90/120 FPS with minimal battery drain.
- 🔐 **Native Cryptographic Pairing**: Complete C# implementation of Moonlight's 4-digit PIN authentication, RSA-2048, and AES-128 challenge-response handshake with Sunshine.
- 🎛️ **Spatial 3D VR Dashboard**: Floating HUD beneath the display for one-tap host discovery, PIN pairing, bitrate tuning (15–80 Mbps), distance adjustments (0.8m–4.0m), and live streaming diagnostics.
- 🧪 **Offline Simulator Mode**: Built-in test generator for testing and editing inside Unity Editor on PC/Linux without needing an active Quest headset or Sunshine host.

---

## 📐 Architecture Overview

```
                      +---------------------------------------+
                      |       Host PC (Windows / Linux)       |
                      |  - Sunshine Server                    |
                      |  - DXGI Desktop Capture (DDA)         |
                      |  - NVENC / AMF / QuickSync Hardware   |
                      +---------------------------------------+
                                  ^   |             |
                     HTTPS / RTSP |   | RTP Video   | UDP Input
                   (47984, 48010) |   | (47998)     | (47999)
                                  |   v             v
+---------------------------------------------------------------------------------+
|                         Meta Quest 3 (MoonlightVR Client)                       |
|                                                                                 |
|  +--------------------+   +-----------------------+   +----------------------+  |
|  |   SunshineClient   |   | MoonlightVideoReceiver|   | MoonlightInputManager|  |
|  | - mDNS Discovery   |   | - UDP RTP Receiver    |   | - Mouse Raycast UV   |  |
|  | - 4-Digit PIN Pair |   | - MediaCodec Zero-Copy|   | - Virtual Xbox Pad   |  |
|  | - App Launch / Quit|   | - Sim Test Pattern    |   | - Keyboard Pass-thru |  |
|  +--------------------+   +-----------------------+   +----------------------+  |
|            |                          |                          |              |
|            v                          v                          v              |
|  +---------------------------------------------------------------------------+  |
|  |                     Spatial Display & XR Interaction                      |  |
|  | - CurvedScreenGenerator (Procedural Cylinder Mesh & Dynamic Collider)     |  |
|  | - SpatialScreenInteraction (6DoF Placement, Distance, Scale, Recenter)   |  |
|  | - MetaPassthroughManager (OpenXR Mixed Reality Passthrough Toggle)        |  |
|  | - VRMenuHUD (Floating Spatial UI Canvas with TMP Controls)                |  |
|  +---------------------------------------------------------------------------+  |
+---------------------------------------------------------------------------------+
```

---

## 🚀 Getting Started

### 1. Prerequisites (Host PC)
1. Install **[Sunshine](https://github.com/LizardByte/Sunshine/releases)** on your PC.
2. Open the Sunshine web configuration panel at `https://localhost:47990` in your PC browser.
3. Ensure Sunshine is running and enabled on your local network.

### 2. Running on Meta Quest 3
1. Download `MoonlightVR-Unity.apk` from the table above or build from source.
2. Sideload via SideQuest or ADB:
   ```bash
   adb install -r MoonlightVR-Unity.apk
   ```
3. Put on your Quest 3, open the App Library, filter by **Unknown Sources**, and launch **Moonlight VR**.
4. In the floating VR dashboard:
   - Click **Scan LAN** (or enter your PC's local IP address manually, e.g. `192.168.1.120`).
   - Click **Pair with PIN**. A 4-digit PIN will appear on screen.
   - Enter that PIN in Sunshine's Web UI (`Configuration -> PIN`).
   - Click **Connect & Stream** to project your PC desktop in full VR/MR!

---

## 🛠️ Building From Source

### Build Android APK Directly with Gradle
```bash
cd android
./gradlew assembleRelease
# The compiled APK will be at android/app/build/outputs/apk/release/app-release.apk
```

### Open and Edit in Unity Editor
1. Open Unity Hub.
2. Add project folder `/home/k/MoonlightVR-Unity`.
3. Open `Assets/Scenes/MainVRDesktopScene.unity`.
4. Press **Play** in Unity Editor to test with the interactive test pattern and mouse raycasting.

---

## 📁 Repository Structure

```
MoonlightVR-Unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Moonlight/
│   │   │   ├── SunshineClient.cs         # Host discovery, pairing, and session launcher
│   │   │   ├── SunshineCrypto.cs         # Cryptographic pairing and challenge-response
│   │   │   ├── MoonlightPacketProtocol.cs# UDP packet serializers for input and streaming
│   │   │   ├── MoonlightInputManager.cs  # Low-latency UDP control input channel
│   │   │   └── MoonlightVideoReceiver.cs # Video streaming and MediaCodec hardware bridge
│   │   ├── VR/
│   │   │   ├── CurvedScreenGenerator.cs  # Procedural cylindrical display mesh & collider
│   │   │   ├── XRScreenRaycaster.cs      # VR laser pointer to desktop mouse mapping
│   │   │   ├── SpatialScreenInteraction.cs # 6DoF screen manipulation & recentering
│   │   │   └── MetaPassthroughManager.cs # Meta Quest 3 Mixed Reality passthrough
│   │   └── UI/
│   │       ├── VRMenuHUD.cs              # Spatial floating settings dashboard
│   │       └── SettingsData.cs           # Persistent user settings
│   ├── Shaders/
│   │   ├── CurvedVideoScreen.shader      # High-performance unlit screen shader with border glow
│   │   └── LaserPointer.shader           # Glowing holographic VR laser pointer
│   ├── Plugins/Android/
│   │   ├── AndroidManifest.xml           # Quest 3 OpenXR VR manifest
│   │   └── MoonlightDecoderBridge.java   # Android MediaCodec zero-copy decoder bridge
│   └── Scenes/
│       └── MainVRDesktopScene.unity      # Main VR scene
├── android/                              # Standalone Android Gradle build system
├── Packages/manifest.json                # Unity package dependencies
├── ProjectSettings/                      # OpenXR & Quest 3 player configuration
├── MoonlightVR-Unity.apk                 # Compiled release APK ready for sideloading
└── README.md                             # Project documentation & download QR table
```

---

## 📄 License
MIT License. Built for the OpenXR, Moonlight, and Meta Quest open-source communities.
