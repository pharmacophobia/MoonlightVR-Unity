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
            passthroughEnabled = !passthroughEnabled;
            ApplyPassthroughState();
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
                // Cinema Void
                xrCamera.clearFlags = CameraClearFlags.SolidColor;
                xrCamera.backgroundColor = vrSkyColor;
            }

            Debug.Log($"[MetaPassthroughManager] Passthrough active: {passthroughEnabled}");
        }
    }
}
