using UnityEngine;
using MoonlightVR.Moonlight;

namespace MoonlightVR.VR
{
    /// <summary>
    /// Projects a VR laser pointer from the controller or camera onto the curved screen.
    /// Accurately maps 3D raycast hit points to 2D desktop pixel coordinates and sends
    /// click, drag, and scroll events to Sunshine via MoonlightInputManager.
    /// </summary>
    public class XRScreenRaycaster : MonoBehaviour
    {
        [Header("Controller Ray Origin")]
        [SerializeField] private Transform rayOriginTransform;
        [SerializeField] private float maxRayDistance = 10f;
        [SerializeField] private LayerMask screenLayerMask = ~0;

        [Header("Visual Feedback")]
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private GameObject reticleIndicator;
        [SerializeField] private float laserWidth = 0.004f;

        [Header("Input Configuration")]
        [SerializeField] private bool enableMouseDrag = true;
        [SerializeField] private float scrollSensitivity = 120f;

        private bool _isLeftMouseDown = false;
        private bool _isRightMouseDown = false;
        private Vector2 _lastUv = Vector2.zero;

        private void Start()
        {
            if (laserLine == null)
            {
                laserLine = gameObject.AddComponent<LineRenderer>();
                laserLine.startWidth = laserWidth;
                laserLine.endWidth = laserWidth * 0.5f;
                laserLine.material = new Material(Shader.Find("Sprites/Default"));
                laserLine.startColor = new Color(0.2f, 0.7f, 1f, 0.8f);
                laserLine.endColor = new Color(0.1f, 0.4f, 1f, 0.2f);
            }

            if (reticleIndicator == null)
            {
                reticleIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                reticleIndicator.name = "LaserReticle";
                reticleIndicator.transform.localScale = Vector3.one * 0.02f;
                Destroy(reticleIndicator.GetComponent<Collider>());
                var reticleMat = new Material(Shader.Find("Sprites/Default"));
                reticleMat.color = new Color(0.2f, 0.8f, 1f, 0.9f);
                reticleIndicator.GetComponent<Renderer>().material = reticleMat;
            }

            if (rayOriginTransform == null)
            {
                rayOriginTransform = transform;
            }
        }

        private void Update()
        {
            Ray ray = GetActiveRay();
            laserLine.SetPosition(0, ray.origin);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, screenLayerMask))
            {
                var screenGen = hit.collider.GetComponent<CurvedScreenGenerator>();
                if (screenGen != null)
                {
                    laserLine.SetPosition(1, hit.point);
                    reticleIndicator.SetActive(true);
                    reticleIndicator.transform.position = hit.point + hit.normal * 0.005f;
                    reticleIndicator.transform.forward = -hit.normal;

                    Vector2 uv = hit.textureCoord;
                    ProcessDesktopInput(uv);
                    return;
                }
            }

            // Fallback when not pointing at screen
            laserLine.SetPosition(1, ray.origin + ray.direction * 3f);
            reticleIndicator.SetActive(false);

            if (_isLeftMouseDown)
            {
                _isLeftMouseDown = false;
                MoonlightInputManager.Instance?.SendMouseButton(MoonlightPacketProtocol.BUTTON_LEFT, false);
            }
        }

        private Ray GetActiveRay()
        {
            // In Unity Editor or standalone mode without active XR controller, raycast from camera center or mouse
            if (Application.isEditor || !UnityEngine.XR.XRSettings.isDeviceActive)
            {
                if (Camera.main != null)
                {
                    return Camera.main.ScreenPointToRay(Input.mousePosition);
                }
            }

            return new Ray(rayOriginTransform.position, rayOriginTransform.forward);
        }

        private void ProcessDesktopInput(Vector2 uv)
        {
            if (MoonlightInputManager.Instance == null) return;

            // Send mouse movement when cursor position updates
            if (Vector2.Distance(uv, _lastUv) > 0.0005f)
            {
                MoonlightInputManager.Instance.SendNormalizedMouseMove(uv.x, uv.y);
                _lastUv = uv;
            }

            // Left Click (Trigger or Left Mouse Button)
            bool triggerPressed = Input.GetMouseButton(0) || Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger") > 0.5f;
            if (triggerPressed && !_isLeftMouseDown)
            {
                _isLeftMouseDown = true;
                MoonlightInputManager.Instance.SendMouseButton(MoonlightPacketProtocol.BUTTON_LEFT, true);
            }
            else if (!triggerPressed && _isLeftMouseDown)
            {
                _isLeftMouseDown = false;
                MoonlightInputManager.Instance.SendMouseButton(MoonlightPacketProtocol.BUTTON_LEFT, false);
            }

            // Right Click (Grip or Right Mouse Button)
            bool gripPressed = Input.GetMouseButton(1) || Input.GetAxis("Oculus_CrossPlatform_PrimaryHandTrigger") > 0.5f;
            if (gripPressed && !_isRightMouseDown)
            {
                _isRightMouseDown = true;
                MoonlightInputManager.Instance.SendMouseButton(MoonlightPacketProtocol.BUTTON_RIGHT, true);
            }
            else if (!gripPressed && _isRightMouseDown)
            {
                _isRightMouseDown = false;
                MoonlightInputManager.Instance.SendMouseButton(MoonlightPacketProtocol.BUTTON_RIGHT, false);
            }

            // Scroll Wheel (Mouse Wheel or Thumbstick Y)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                scroll = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            }

            if (Mathf.Abs(scroll) > 0.05f)
            {
                short delta = (short)Mathf.RoundToInt(scroll * scrollSensitivity);
                MoonlightInputManager.Instance.SendMouseScroll(delta);
            }
        }
    }
}
