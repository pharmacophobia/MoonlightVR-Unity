using UnityEngine;

namespace MoonlightVR.VR
{
    /// <summary>
    /// Procedurally creates a cylindrical curved display mesh in Unity.
    /// Provides exact UV mapping and real-time curvature adjustment from flat plane (0°)
    /// to deep cockpit wrap-around (120° FOV). Updates the MeshCollider dynamically for raycasting.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CurvedScreenGenerator : MonoBehaviour
    {
        [Header("Screen Dimensions (Meters)")]
        [Range(0.5f, 6.0f)]
        [SerializeField] private float width = 2.4f;

        [Range(0.3f, 3.5f)]
        [SerializeField] private float height = 1.35f; // 16:9 ratio default

        [Header("Curvature Settings")]
        [Range(0f, 120f)]
        [SerializeField] private float curveAngleDegrees = 45f;

        [Header("Tessellation Detail")]
        [Range(8, 128)]
        [SerializeField] private int segmentsX = 64;

        [Range(2, 32)]
        [SerializeField] private int segmentsY = 4;

        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _screenMesh;

        public float Width => width;
        public float Height => height;
        public float CurveAngleDegrees => curveAngleDegrees;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            GenerateMesh();
        }

        private void OnValidate()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();
            GenerateMesh();
        }

        public void SetCurvature(float angleDegrees)
        {
            curveAngleDegrees = Mathf.Clamp(angleDegrees, 0f, 120f);
            GenerateMesh();
        }

        public void SetDimensions(float newWidth, float newHeight)
        {
            width = Mathf.Max(0.5f, newWidth);
            height = Mathf.Max(0.3f, newHeight);
            GenerateMesh();
        }

        public void GenerateMesh()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();

            if (_screenMesh == null)
            {
                _screenMesh = new Mesh { name = "ProceduralCurvedScreen" };
            }
            else
            {
                _screenMesh.Clear();
            }

            int numVertices = (segmentsX + 1) * (segmentsY + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[segmentsX * segmentsY * 6];

            float angleRad = curveAngleDegrees * Mathf.Deg2Rad;
            bool isFlat = curveAngleDegrees <= 0.01f;

            // Calculate cylinder radius: arcLength = radius * angleRad
            float radius = isFlat ? 1.0f : (width / angleRad);

            for (int y = 0; y <= segmentsY; y++)
            {
                float v = (float)y / segmentsY;
                float posY = (v - 0.5f) * height;

                for (int x = 0; x <= segmentsX; x++)
                {
                    float u = (float)x / segmentsX;
                    int index = y * (segmentsX + 1) + x;

                    float posX, posZ;
                    Vector3 normal;

                    if (isFlat)
                    {
                        posX = (u - 0.5f) * width;
                        posZ = 0f;
                        normal = -Vector3.forward;
                    }
                    else
                    {
                        // Subtend angle symmetrically from -angle/2 to +angle/2
                        float currentAngle = (u - 0.5f) * angleRad;
                        posX = radius * Mathf.Sin(currentAngle);
                        posZ = radius * (1.0f - Mathf.Cos(currentAngle));
                        normal = new Vector3(-Mathf.Sin(currentAngle), 0, -Mathf.Cos(currentAngle)).normalized;
                    }

                    vertices[index] = new Vector3(posX, posY, posZ);
                    normals[index] = normal;
                    uvs[index] = new Vector2(u, v);
                }
            }

            int triIndex = 0;
            for (int y = 0; y < segmentsY; y++)
            {
                for (int x = 0; x < segmentsX; x++)
                {
                    int bottomLeft = y * (segmentsX + 1) + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = (y + 1) * (segmentsX + 1) + x;
                    int topRight = topLeft + 1;

                    // First triangle (bottom-left, top-left, bottom-right)
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = bottomRight;

                    // Second triangle (bottom-right, top-left, top-right)
                    triangles[triIndex++] = bottomRight;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = topRight;
                }
            }

            _screenMesh.vertices = vertices;
            _screenMesh.normals = normals;
            _screenMesh.uv = uvs;
            _screenMesh.triangles = triangles;
            _screenMesh.RecalculateBounds();

            _meshFilter.sharedMesh = _screenMesh;

            if (_meshCollider != null)
            {
                _meshCollider.sharedMesh = null;
                _meshCollider.sharedMesh = _screenMesh;
            }
        }
    }
}
