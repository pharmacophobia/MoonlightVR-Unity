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

using UnityEngine;

namespace MoonlightVR.VR
{
    /// <summary>
    /// Manages Meta Quest 3 Mixed Reality Passthrough.
    /// Allows the user to toggle between seeing their physical room (for using a physical keyboard/mouse)
    /// and an immersive dark virtual cinema environment.
    /// </summary>
    public class MetaPassthroughManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera xrCamera;
        [SerializeField] private Color vrSkyColor = new Color(0.04f, 0.05f, 0.08f, 1.0f);

        [Header("State")]
        [SerializeField] private bool passthroughEnabled = true;

        public bool IsPassthroughEnabled => passthroughEnabled;

        private void Start()
        {
            if (xrCamera == null) xrCamera = Camera.main;
            ApplyPassthroughState();
        }

        public void TogglePassthrough()
        {
            SetPassthrough(!passthroughEnabled);
        }

        public void SetPassthrough(bool enable)
        {
            passthroughEnabled = enable;
            ApplyPassthroughState();
        }

        private void ApplyPassthroughState()
        {
            if (xrCamera == null) xrCamera = Camera.main;
            if (xrCamera == null) return;

            if (passthroughEnabled)
            {
                // In OpenXR / Meta XR, passthrough underlay requires clear color with alpha 0
                xrCamera.clearFlags = CameraClearFlags.SolidColor;
                xrCamera.backgroundColor = new Color(0, 0, 0, 0);
            }
            else
            {
                // Default virtual sky color when not controlled by VREnvironmentManager
                if (VREnvironmentManager.Instance == null || VREnvironmentManager.Instance.CurrentEnvironment == VREnvironmentType.Passthrough)
                {
                    xrCamera.clearFlags = CameraClearFlags.SolidColor;
                    xrCamera.backgroundColor = vrSkyColor;
                }
            }

            Debug.Log($"[MetaPassthroughManager] Passthrough active: {passthroughEnabled}");
        }
    }
}
