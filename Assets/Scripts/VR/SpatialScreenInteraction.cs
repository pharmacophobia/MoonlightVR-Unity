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
    /// Controls 6DoF spatial positioning, distance, scaling, and recentering
    /// of the virtual desktop in your physical/virtual room.
    /// </summary>
    public class SpatialScreenInteraction : MonoBehaviour
    {
        [Header("Target Screen Root")]
        [SerializeField] private Transform screenRoot;

        [Header("Placement Defaults")]
        [SerializeField] private float defaultDistance = 1.8f;
        [SerializeField] private float defaultHeightOffset = 0.0f;
        [SerializeField] private float currentScale = 1.0f;

        [Header("Constraints")]
        [SerializeField] private float minDistance = 0.8f;
        [SerializeField] private float maxDistance = 5.0f;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 3.5f;
        [SerializeField] private bool lockPosition = false;

        private float _currentDistance;

        public float CurrentDistance => _currentDistance;
        public float CurrentScale => currentScale;
        public bool IsLocked => lockPosition;

        private void Start()
        {
            if (screenRoot == null) screenRoot = transform;
            _currentDistance = defaultDistance;
            RecenterScreen();
        }

        /// <summary>
        /// Smoothly repositions the virtual screen directly in front of the user's headset.
        /// </summary>
        public void RecenterScreen()
        {
            if (lockPosition) return;

            Transform hmd = Camera.main != null ? Camera.main.transform : null;
            if (hmd == null) return;

            Vector3 forward = hmd.forward;
            forward.y = 0;
            forward.Normalize();

            if (forward == Vector3.zero) forward = Vector3.forward;

            Vector3 targetPosition = hmd.position + forward * _currentDistance + Vector3.up * defaultHeightOffset;
            screenRoot.position = targetPosition;
            screenRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>
        /// Adjusts screen distance from the viewer.
        /// </summary>
        public void SetDistance(float distance)
        {
            if (lockPosition) return;
            _currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

            Transform hmd = Camera.main != null ? Camera.main.transform : null;
            if (hmd != null)
            {
                Vector3 direction = (screenRoot.position - hmd.position);
                direction.y = 0;
                direction.Normalize();
                screenRoot.position = hmd.position + direction * _currentDistance;
            }
        }

        /// <summary>
        /// Scales the virtual display size uniformly.
        /// </summary>
        public void SetScale(float scale)
        {
            if (lockPosition) return;
            currentScale = Mathf.Clamp(scale, minScale, maxScale);
            screenRoot.localScale = Vector3.one * currentScale;
        }

        /// <summary>
        /// Toggles locking the screen position in space.
        /// </summary>
        public void ToggleLock()
        {
            lockPosition = !lockPosition;
        }

        public void SetLocked(bool locked)
        {
            lockPosition = locked;
        }
    }
}
