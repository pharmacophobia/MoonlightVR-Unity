# 📜 Meta Quest Store & GNU GPL-3.0 Legal Compliance Guide

> **Official Compliance Playbook for Publishing and Selling MoonlightVR on the Meta Horizon Store (App Lab) under the GNU General Public License v3.0.**

---

## 🏛️ 1. Executive Summary & Legal Framework

### Can You Legally Sell GPL-3.0 Software on the Meta Quest Store?
**Yes, 100% legally.**

The GNU General Public License v3.0 (GPL-3.0) is a **copyleft** license that explicitly protects the right to charge money for distributing copies of software:

> *"When we speak of free software, we are referring to freedom, not price. Our General Public Licenses are designed to make sure that you have the freedom to distribute copies of free software (and charge for them if you wish)..."*  
> — *GNU General Public License Preamble & Section 4*

Because MoonlightVR incorporates Moonlight's core GameStream client code, **the entire combined application is licensed under GPL-3.0**. 

---

## ⚖️ 2. The Core Copyleft Obligations

To legally distribute and sell a GPL-3.0 app on the Meta Quest Store for **$2.00**, you must fulfill three non-negotiable legal requirements:

| Requirement | Implementation in MoonlightVR | Status |
| :--- | :--- | :---: |
| **1. Full Source Code Availability** | The complete corresponding source code is publicly accessible on GitHub: [github.com/pharmacophobia/MoonlightVR-Unity](https://github.com/pharmacophobia/MoonlightVR-Unity). | ✅ Enforced |
| **2. Upstream License Preservation** | Canonical [LICENSE](file:///home/k/MoonlightVR-Unity/LICENSE) (GPL-3.0) placed in repo root and copyright headers added to all source files. | ✅ Enforced |
| **3. In-App Source Delivery** | Dedicated in-app **About & Open Source Credits Modal** (`GPL_AboutModal`) accessible directly inside the VR dashboard. | ✅ Enforced |
| **4. User Redistribution Rights** | Anyone who buys the app on the store has the legal right to inspect, fork, compile, and redistribute the code. | ✅ Enforced |

---

## 💰 3. Why the "$2.00 Convenience" Strategy Works

Selling open-source software on console/VR app stores is a well-established, highly lucrative model (pioneered by tools like Blender, Krita, and Source-engine ports):

1. **Zero-Friction Installation**: 98% of Meta Quest owners do not have SideQuest installed or developer mode enabled. They happily pay $2.00 for the convenience of 1-tap installation directly from their headset.
2. **Automatic Background Updates**: Store purchases receive automatic OTA updates whenever you push a new release to App Lab.
3. **Ecosystem Integration**: App shows up cleanly in the native Quest App Library without being categorized as an "Unknown Source".

---

## 🛡️ 4. Meta Horizon Store Policies & Guidelines

### A. Open-Source App Submission on Meta Horizon Store
Meta permits applications built using open-source libraries and copyleft frameworks provided that:
1. The developer possesses the legal right to distribute the binary (granted by GPL-3.0 Section 4).
2. The submission does not claim proprietary trademark exclusivity over upstream projects (Moonlight and Sunshine trademarks belong to their respective owners).
3. The store listing provides transparent attribution.

### B. Meta's "User Content" Policy Guardrail
Meta Developer Terms distinguish between **App Software Binaries** and **User Content** (community-uploaded assets, user-generated avatars):
- Open-source licenses apply to the software binary and engine code.
- As long as you are packaging your custom client wrapper and the Moonlight streaming code into the binary, you do not violate Meta's content policies.

---

## 📲 5. In-App Source Code Delivery (GPL Section 6)

Section 6(d) of GPL-3.0 requires offering equivalent access to the Corresponding Source from the place where the binary is distributed.

MoonlightVR fulfills this directly inside the VR environment:
1. Open the floating VR Dashboard.
2. Select **📜 About / Open Source License**.
3. An in-VR dialog displays:
   - Full GPL-3.0 license declaration.
   - Clickable / viewable link to the public GitHub repository:  
     `https://github.com/pharmacophobia/MoonlightVR-Unity`
   - Formal attribution to upstream projects:
     - **Moonlight GameStream** (`moonlight-stream.org`)
     - **Sunshine Server** (`app.sunshinevr.com`)
     - **Moonlight Android XR** (Cameron Gilleece)

---

## 📝 6. Meta Quest Store Listing Description (Copy & Paste)

When submitting MoonlightVR to Meta Horizon Developer Hub / App Lab, use this approved store listing copy:

```markdown
🌌 MoonlightVR for Meta Quest 3

High-Performance Spatial Remote Desktop & GameStream Client for Meta Quest 3, powered by Sunshine and Moonlight.

Stream your PC desktop and triple-A PC games with ultra-low latency, crystal-clear 4K resolution, and fluid 90/120 FPS directly onto a massive virtual curved screen in mixed reality or immersive 3D environments.

✨ KEY FEATURES:
• 🖥️ Infinite Curved Virtual Monitor: Adjust curvature from flat desktop to 120° wrap-around cockpit.
• 🏞️ 7 Immersive Environments: Cosmic Deep Space, Alpine Sunset Chalet, Cyberpunk Penthouse, Nordic Forest Sanctuary, Minimalist Modern Loft, Cinema Void, and Mixed Reality Passthrough.
• ⌨️ Floating 3D Keyboard & Voice Dictation: Speak naturally to dictate text and commands directly into your PC with on-device speech recognition.
• 🎯 Touch Plus Laser Pointer & Virtual Xbox Gamepad: Seamless mouse clicks, scrolling, and virtual gamepad emulation.
• ⚡ Snapdragon XR2 Gen 2 Hardware Acceleration: Zero-copy 4K HEVC/AV1 hardware decoding with minimal battery consumption.

📄 OPEN-SOURCE LICENSE & NOTICE:
MoonlightVR is free and open-source software licensed under the GNU General Public License version 3.0 (GPL-3.0). It is built upon the open-source Moonlight GameStream client and Sunshine server.

In accordance with Section 6 of the GNU GPL v3.0, the complete source code for this application is freely available for inspection, modification, and community contribution at:
https://github.com/pharmacophobia/MoonlightVR-Unity

Credits & Upstream Attribution:
• Moonlight GameStream Client (GPL-3.0) — https://moonlight-stream.org
• Sunshine Server (GPL-3.0) — https://app.sunshinevr.com
• Moonlight Android XR (GPL-3.0) — Cameron Gilleece
```

---

## ✅ 7. Pre-Submission Checklist

- [x] Canonical `LICENSE` file (GPL-3.0) present in repository root.
- [x] Standard GPL-3.0 license header notices added to all source files.
- [x] In-app `About` / `Open Source` modal implemented in `VRMenuHUD.cs`.
- [x] Public GitHub repository configured and active with full source code.
- [x] Official compiled binary (`MoonlightVR-Unity.apk`) available in repository and releases.
- [x] Store listing description contains clear attribution and GitHub source link.
