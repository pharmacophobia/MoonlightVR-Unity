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
using System.Collections.Generic;
using UnityEngine;

namespace MoonlightVR.VR
{
    public enum VREnvironmentType
    {
        Passthrough = 0,
        CinemaVoid = 1,
        CosmicNebula = 2,
        AlpineSunset = 3,
        CyberpunkPenthouse = 4,
        NordicSanctuary = 5,
        ModernLoft = 6
    }

    /// <summary>
    /// Manages 3D immersive VR environments for MoonlightVR on Meta Quest 3.
    /// Provides 5 distinctive atmospheric 3D virtual worlds, plus Cinema Void and Mixed Reality Passthrough.
    /// Procedurally constructs and caches visual geometry, lighting, platforms, and particle effects.
    /// </summary>
    public class VREnvironmentManager : MonoBehaviour
    {
        public static VREnvironmentManager Instance { get; private set; }

        [Header("Camera & Dependencies")]
        [SerializeField] private Camera xrCamera;
        [SerializeField] private MetaPassthroughManager passthroughManager;

        [Header("Active Environment")]
        [SerializeField] private VREnvironmentType currentEnvironment = VREnvironmentType.Passthrough;

        public VREnvironmentType CurrentEnvironment => currentEnvironment;
        public event Action<VREnvironmentType> OnEnvironmentChanged;

        // Cached Environment Root GameObjects
        private readonly Dictionary<VREnvironmentType, GameObject> _environmentRoots = new Dictionary<VREnvironmentType, GameObject>();
        private GameObject _environmentMasterParent;

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

            if (xrCamera == null) xrCamera = Camera.main;
            if (passthroughManager == null) passthroughManager = GetComponent<MetaPassthroughManager>();

            _environmentMasterParent = new GameObject("VREnvironments_Root");
            _environmentMasterParent.transform.SetParent(transform);
        }

        private void Start()
        {
            // Initialize and apply starting environment
            SetEnvironment(currentEnvironment, true);
        }

        /// <summary>
        /// Switches to the specified environment type.
        /// </summary>
        public void SetEnvironment(VREnvironmentType type, bool forceRefresh = false)
        {
            if (!forceRefresh && currentEnvironment == type && _environmentRoots.ContainsKey(type))
            {
                return;
            }

            currentEnvironment = type;

            // Handle Passthrough vs Virtual 3D Environment
            if (currentEnvironment == VREnvironmentType.Passthrough)
            {
                HideAllVirtualEnvironments();
                if (passthroughManager != null) passthroughManager.SetPassthrough(true);
                if (xrCamera != null)
                {
                    xrCamera.clearFlags = CameraClearFlags.SolidColor;
                    xrCamera.backgroundColor = new Color(0, 0, 0, 0);
                }
                RenderSettings.ambientLight = Color.white;
                OnEnvironmentChanged?.Invoke(currentEnvironment);
                return;
            }

            // Virtual Environment: Disable passthrough
            if (passthroughManager != null) passthroughManager.SetPassthrough(false);
            HideAllVirtualEnvironments();

            // Ensure the requested environment root is created
            if (!_environmentRoots.ContainsKey(type) || _environmentRoots[type] == null)
            {
                _environmentRoots[type] = BuildEnvironment(type);
            }

            GameObject activeRoot = _environmentRoots[type];
            if (activeRoot != null) activeRoot.SetActive(true);

            ApplyLightingAndSky(type);
            OnEnvironmentChanged?.Invoke(currentEnvironment);
            Debug.Log($"[VREnvironmentManager] Switched to environment: {type}");
        }

        public void NextEnvironment()
        {
            int total = Enum.GetValues(typeof(VREnvironmentType)).Length;
            int next = ((int)currentEnvironment + 1) % total;
            SetEnvironment((VREnvironmentType)next);
        }

        public void PreviousEnvironment()
        {
            int total = Enum.GetValues(typeof(VREnvironmentType)).Length;
            int prev = ((int)currentEnvironment - 1 + total) % total;
            SetEnvironment((VREnvironmentType)prev);
        }

        private void HideAllVirtualEnvironments()
        {
            foreach (var kvp in _environmentRoots)
            {
                if (kvp.Value != null) kvp.Value.SetActive(false);
            }
        }

        private void ApplyLightingAndSky(VREnvironmentType type)
        {
            if (xrCamera == null) xrCamera = Camera.main;
            if (xrCamera == null) return;

            xrCamera.clearFlags = CameraClearFlags.SolidColor;

            switch (type)
            {
                case VREnvironmentType.CinemaVoid:
                    xrCamera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.15f);
                    break;

                case VREnvironmentType.CosmicNebula:
                    xrCamera.backgroundColor = new Color(0.015f, 0.01f, 0.04f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.2f, 0.15f, 0.35f);
                    break;

                case VREnvironmentType.AlpineSunset:
                    xrCamera.backgroundColor = new Color(0.45f, 0.22f, 0.18f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.6f, 0.45f, 0.35f);
                    break;

                case VREnvironmentType.CyberpunkPenthouse:
                    xrCamera.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.1f, 0.25f, 0.4f);
                    break;

                case VREnvironmentType.NordicSanctuary:
                    xrCamera.backgroundColor = new Color(0.01f, 0.05f, 0.04f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.15f, 0.25f, 0.2f);
                    break;

                case VREnvironmentType.ModernLoft:
                    xrCamera.backgroundColor = new Color(0.35f, 0.45f, 0.55f, 1.0f);
                    RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
                    break;
            }
        }

        #region Procedural Environment Builders

        private GameObject BuildEnvironment(VREnvironmentType type)
        {
            GameObject root = new GameObject($"Env_{type}");
            root.transform.SetParent(_environmentMasterParent.transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            switch (type)
            {
                case VREnvironmentType.CinemaVoid:
                    BuildCinemaVoid(root);
                    break;
                case VREnvironmentType.CosmicNebula:
                    BuildCosmicNebula(root);
                    break;
                case VREnvironmentType.AlpineSunset:
                    BuildAlpineSunset(root);
                    break;
                case VREnvironmentType.CyberpunkPenthouse:
                    BuildCyberpunkPenthouse(root);
                    break;
                case VREnvironmentType.NordicSanctuary:
                    BuildNordicSanctuary(root);
                    break;
                case VREnvironmentType.ModernLoft:
                    BuildModernLoft(root);
                    break;
            }

            return root;
        }

        // 1. Cinema Void: Sleek dark reflective floor circle
        private void BuildCinemaVoid(GameObject root)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "CinemaFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -1.0f, 0);
            floor.transform.localScale = new Vector3(12f, 0.05f, 12f);
            
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.03f, 0.03f, 0.04f);
            mat.SetFloat("_Glossiness", 0.75f);
            floor.GetComponent<Renderer>().material = mat;
            Destroy(floor.GetComponent<Collider>());
        }

        // 2. Cosmic Nebula: Deep space platform, planetary celestial body, particle starfield
        private void BuildCosmicNebula(GameObject root)
        {
            // Sci-fi floating observation ring
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "CosmicPlatform";
            platform.transform.SetParent(root.transform);
            platform.transform.localPosition = new Vector3(0, -1.0f, 0);
            platform.transform.localScale = new Vector3(6f, 0.1f, 6f);
            
            Material platMat = new Material(Shader.Find("Standard"));
            platMat.color = new Color(0.08f, 0.08f, 0.15f);
            platMat.SetFloat("_Glossiness", 0.85f);
            platform.GetComponent<Renderer>().material = platMat;
            Destroy(platform.GetComponent<Collider>());

            // Outer glowing accent ring
            GameObject accentRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            accentRing.name = "AccentRing";
            accentRing.transform.SetParent(root.transform);
            accentRing.transform.localPosition = new Vector3(0, -0.98f, 0);
            accentRing.transform.localScale = new Vector3(6.3f, 0.02f, 6.3f);
            Material ringMat = new Material(Shader.Find("Standard"));
            ringMat.color = new Color(0.3f, 0.1f, 0.8f);
            ringMat.EnableKeyword("_EMISSION");
            ringMat.SetColor("_EmissionColor", new Color(0.5f, 0.2f, 1.0f) * 1.5f);
            accentRing.GetComponent<Renderer>().material = ringMat;
            Destroy(accentRing.GetComponent<Collider>());

            // Distant celestial planet / crescent moon
            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "CosmicPlanet";
            planet.transform.SetParent(root.transform);
            planet.transform.localPosition = new Vector3(-25f, 15f, 45f);
            planet.transform.localScale = Vector3.one * 18f;
            Material planetMat = new Material(Shader.Find("Standard"));
            planetMat.color = new Color(0.15f, 0.2f, 0.45f);
            planetMat.EnableKeyword("_EMISSION");
            planetMat.SetColor("_EmissionColor", new Color(0.1f, 0.25f, 0.6f) * 0.4f);
            planet.GetComponent<Renderer>().material = planetMat;
            Destroy(planet.GetComponent<Collider>());

            // Particle Starfield & Nebula Dust
            GameObject starsObj = new GameObject("NebulaStars");
            starsObj.transform.SetParent(root.transform);
            starsObj.transform.localPosition = Vector3.zero;
            ParticleSystem ps = starsObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.05f;
            main.startSize = 0.04f;
            main.maxParticles = 600;
            main.startColor = new Color(0.7f, 0.8f, 1.0f, 0.8f);
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 20f;

            var emission = ps.emission;
            emission.rateOverTime = 40;
        }

        // 3. Alpine Sunset: Wooden chalet deck, warm sunset lighting, distant mountain silhouettes
        private void BuildAlpineSunset(GameObject root)
        {
            // Warm timber deck
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "ChaletDeck";
            deck.transform.SetParent(root.transform);
            deck.transform.localPosition = new Vector3(0, -1.0f, 0);
            deck.transform.localScale = new Vector3(8f, 0.15f, 8f);

            Material deckMat = new Material(Shader.Find("Standard"));
            deckMat.color = new Color(0.35f, 0.22f, 0.14f); // Warm cedar wood
            deckMat.SetFloat("_Glossiness", 0.3f);
            deck.GetComponent<Renderer>().material = deckMat;
            Destroy(deck.GetComponent<Collider>());

            // Deck Railing
            for (int i = -3; i <= 3; i += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"RailingPost_{i}";
                post.transform.SetParent(root.transform);
                post.transform.localPosition = new Vector3(i, -0.5f, 3.8f);
                post.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
                post.GetComponent<Renderer>().material = deckMat;
                Destroy(post.GetComponent<Collider>());
            }
            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "TopRail";
            topRail.transform.SetParent(root.transform);
            topRail.transform.localPosition = new Vector3(0, 0.0f, 3.8f);
            topRail.transform.localScale = new Vector3(7.8f, 0.06f, 0.12f);
            topRail.GetComponent<Renderer>().material = deckMat;
            Destroy(topRail.GetComponent<Collider>());

            // Distant mountain silhouettes (stylized angular peaks)
            float[] peakOffsets = { -30f, -10f, 12f, 35f };
            float[] peakHeights = { 14f, 22f, 18f, 12f };
            for (int p = 0; p < peakOffsets.Length; p++)
            {
                GameObject peak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                peak.name = $"MountainPeak_{p}";
                peak.transform.SetParent(root.transform);
                peak.transform.localPosition = new Vector3(peakOffsets[p], peakHeights[p] * 0.4f - 2f, 50f + p * 5f);
                peak.transform.localScale = new Vector3(18f, peakHeights[p], 18f);
                
                Material peakMat = new Material(Shader.Find("Standard"));
                peakMat.color = new Color(0.25f, 0.18f, 0.22f); // Twilight mountain purple
                peak.GetComponent<Renderer>().material = peakMat;
                Destroy(peak.GetComponent<Collider>());
            }

            // Warm golden sunset light
            GameObject sunLight = new GameObject("SunsetSunLight");
            sunLight.transform.SetParent(root.transform);
            Light light = sunLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.72f, 0.45f);
            light.intensity = 1.2f;
            sunLight.transform.rotation = Quaternion.Euler(15f, 25f, 0);
        }

        // 4. Cyberpunk Penthouse: Neon grid terrace, reflective dark surface, neon skyscraper columns
        private void BuildCyberpunkPenthouse(GameObject root)
        {
            // Reflective dark terrace
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "PenthouseFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -1.0f, 0);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);

            Material floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.05f, 0.06f, 0.08f);
            floorMat.SetFloat("_Glossiness", 0.9f);
            floor.GetComponent<Renderer>().material = floorMat;
            Destroy(floor.GetComponent<Collider>());

            // Neon cyan border trim
            GameObject neonTrim1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonTrim1.transform.SetParent(root.transform);
            neonTrim1.transform.localPosition = new Vector3(0, -0.94f, 4.9f);
            neonTrim1.transform.localScale = new Vector3(9.8f, 0.04f, 0.08f);
            Material neonCyan = new Material(Shader.Find("Standard"));
            neonCyan.color = new Color(0.0f, 0.8f, 1.0f);
            neonCyan.EnableKeyword("_EMISSION");
            neonCyan.SetColor("_EmissionColor", new Color(0.0f, 0.9f, 1.0f) * 2.0f);
            neonTrim1.GetComponent<Renderer>().material = neonCyan;
            Destroy(neonTrim1.GetComponent<Collider>());

            // Neon magenta border trim
            GameObject neonTrim2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonTrim2.transform.SetParent(root.transform);
            neonTrim2.transform.localPosition = new Vector3(-4.9f, -0.94f, 0);
            neonTrim2.transform.localScale = new Vector3(0.08f, 0.04f, 9.8f);
            Material neonPink = new Material(Shader.Find("Standard"));
            neonPink.color = new Color(1.0f, 0.1f, 0.6f);
            neonPink.EnableKeyword("_EMISSION");
            neonPink.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.6f) * 2.0f);
            neonTrim2.GetComponent<Renderer>().material = neonPink;
            Destroy(neonTrim2.GetComponent<Collider>());

            // Distant neon skyscrapers
            Vector3[] towers = {
                new Vector3(-20, 10, 35),
                new Vector3(-8, 16, 45),
                new Vector3(10, 14, 40),
                new Vector3(22, 22, 50),
                new Vector3(32, 8, 30)
            };

            for (int t = 0; t < towers.Length; t++)
            {
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tower.name = $"Skyscraper_{t}";
                tower.transform.SetParent(root.transform);
                tower.transform.localPosition = towers[t];
                tower.transform.localScale = new Vector3(6f, 35f, 6f);
                
                Material towerMat = new Material(Shader.Find("Standard"));
                towerMat.color = new Color(0.02f, 0.03f, 0.06f);
                towerMat.EnableKeyword("_EMISSION");
                towerMat.SetColor("_EmissionColor", (t % 2 == 0 ? new Color(0f, 0.3f, 0.6f) : new Color(0.5f, 0.05f, 0.3f)) * 0.3f);
                tower.GetComponent<Renderer>().material = towerMat;
                Destroy(tower.GetComponent<Collider>());
            }
        }

        // 5. Nordic Sanctuary: Moonlit pine clearing, stone circle, crackling campfire
        private void BuildNordicSanctuary(GameObject root)
        {
            // Natural stone circular clearing
            GameObject patio = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            patio.name = "SanctuaryPatio";
            patio.transform.SetParent(root.transform);
            patio.transform.localPosition = new Vector3(0, -1.0f, 0);
            patio.transform.localScale = new Vector3(9f, 0.1f, 9f);

            Material patioMat = new Material(Shader.Find("Standard"));
            patioMat.color = new Color(0.18f, 0.22f, 0.20f); // Forest mossy slate
            patioMat.SetFloat("_Glossiness", 0.2f);
            patio.GetComponent<Renderer>().material = patioMat;
            Destroy(patio.GetComponent<Collider>());

            // Campfire stone ring
            GameObject campfire = new GameObject("CampfireHearth");
            campfire.transform.SetParent(root.transform);
            campfire.transform.localPosition = new Vector3(0, -0.9f, 2.5f);

            for (int s = 0; s < 8; s++)
            {
                float angle = s * Mathf.PI * 2f / 8f;
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"Stone_{s}";
                stone.transform.SetParent(campfire.transform);
                stone.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, 0, Mathf.Sin(angle) * 0.45f);
                stone.transform.localScale = Vector3.one * 0.22f;
                Material stoneMat = new Material(Shader.Find("Standard"));
                stoneMat.color = new Color(0.28f, 0.26f, 0.24f);
                stone.GetComponent<Renderer>().material = stoneMat;
                Destroy(stone.GetComponent<Collider>());
            }

            // Glowing campfire ember heart
            GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ember.name = "EmberCore";
            ember.transform.SetParent(campfire.transform);
            ember.transform.localPosition = new Vector3(0, 0.05f, 0);
            ember.transform.localScale = Vector3.one * 0.3f;
            Material emberMat = new Material(Shader.Find("Standard"));
            emberMat.color = new Color(1.0f, 0.4f, 0.05f);
            emberMat.EnableKeyword("_EMISSION");
            emberMat.SetColor("_EmissionColor", new Color(1.0f, 0.45f, 0.05f) * 3.0f);
            ember.GetComponent<Renderer>().material = emberMat;
            Destroy(ember.GetComponent<Collider>());

            // Campfire Warm Point Light
            GameObject fireLight = new GameObject("FireLight");
            fireLight.transform.SetParent(campfire.transform);
            fireLight.transform.localPosition = new Vector3(0, 0.3f, 0);
            Light fl = fireLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(1.0f, 0.6f, 0.25f);
            fl.intensity = 2.5f;
            fl.range = 7f;

            // Encompassing Pine Tree Silhouettes
            for (int t = 0; t < 10; t++)
            {
                float tAngle = t * Mathf.PI * 2f / 10f;
                float dist = 14f + (t % 3) * 2f;
                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = $"PineTree_{t}";
                tree.transform.SetParent(root.transform);
                tree.transform.localPosition = new Vector3(Mathf.Cos(tAngle) * dist, 2.5f, Mathf.Sin(tAngle) * dist);
                tree.transform.localScale = new Vector3(1.8f, 5f, 1.8f);
                
                Material treeMat = new Material(Shader.Find("Standard"));
                treeMat.color = new Color(0.04f, 0.12f, 0.08f); // Deep forest pine
                tree.GetComponent<Renderer>().material = treeMat;
                Destroy(tree.GetComponent<Collider>());
            }
        }

        // 6. Modern Loft: Scandinavian architectural studio, warm parquet flooring, glass view
        private void BuildModernLoft(GameObject root)
        {
            // Polished hardwood floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "LoftHardwoodFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -1.0f, 0);
            floor.transform.localScale = new Vector3(11f, 0.1f, 11f);

            Material woodMat = new Material(Shader.Find("Standard"));
            woodMat.color = new Color(0.6f, 0.45f, 0.32f); // Scandinavian white oak
            woodMat.SetFloat("_Glossiness", 0.65f);
            floor.GetComponent<Renderer>().material = woodMat;
            Destroy(floor.GetComponent<Collider>());

            // Architectural Ceiling Frame with recessed lights
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "LoftCeiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localPosition = new Vector3(0, 3.2f, 0);
            ceiling.transform.localScale = new Vector3(11f, 0.1f, 11f);
            Material ceilMat = new Material(Shader.Find("Standard"));
            ceilMat.color = new Color(0.92f, 0.92f, 0.94f);
            ceiling.GetComponent<Renderer>().material = ceilMat;
            Destroy(ceiling.GetComponent<Collider>());

            // Floor-to-ceiling panoramic glass frame posts
            for (int p = -4; p <= 4; p += 4)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"WindowPost_{p}";
                post.transform.SetParent(root.transform);
                post.transform.localPosition = new Vector3(p, 1.1f, 5.4f);
                post.transform.localScale = new Vector3(0.18f, 4.2f, 0.18f);
                Material postMat = new Material(Shader.Find("Standard"));
                postMat.color = new Color(0.12f, 0.14f, 0.16f); // Matte architectural black
                post.GetComponent<Renderer>().material = postMat;
                Destroy(post.GetComponent<Collider>());
            }

            // Soft warm recessed interior lighting
            GameObject loftLight = new GameObject("InteriorWarmLight");
            loftLight.transform.SetParent(root.transform);
            loftLight.transform.localPosition = new Vector3(0, 2.8f, -1f);
            Light l = loftLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.95f, 0.88f);
            l.intensity = 1.4f;
            l.range = 10f;
        }

        #endregion
    }
}
