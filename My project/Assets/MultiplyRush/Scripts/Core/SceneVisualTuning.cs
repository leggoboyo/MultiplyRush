using UnityEngine;
using UnityEngine.Rendering;

namespace MultiplyRush
{
    public static class SceneVisualTuning
    {
        private static readonly Color MenuBackground = new Color(0.06f, 0.08f, 0.14f, 1f);
        private static readonly Color GameBackground = new Color(0.59f, 0.74f, 0.92f, 1f);
        private static Material _runtimeSkybox;
        private static bool _qualityConfigured;

        public static void ApplyMenuLook()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = MenuBackground;
            }
        }

        public static void ApplyGameLook()
        {
            ConfigureRuntimeQuality();

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                mainCamera.backgroundColor = GameBackground;
                mainCamera.nearClipPlane = 0.05f;
                mainCamera.farClipPlane = 450f;
                mainCamera.allowHDR = true;
            }

            EnsureStylizedSkybox();

            Light primaryDirectional = null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type != LightType.Directional)
                {
                    continue;
                }

                light.color = new Color(1f, 0.95f, 0.86f, 1f);
                light.intensity = 1.34f;
                light.transform.rotation = Quaternion.Euler(40f, 28f, 0f);
                light.shadows = LightShadows.None;
                primaryDirectional = light;
            }

            EnsureFillLight();
            if (primaryDirectional != null)
            {
                RenderSettings.sun = primaryDirectional;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.92f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.45f, 0.58f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.19f, 0.24f, 0.31f, 1f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.69f, 0.79f, 0.9f, 1f);
            RenderSettings.fogDensity = 0.0058f;
            RenderSettings.reflectionIntensity = 0.58f;
        }

        public static void ApplyLevelTheme(
            Color skyTint,
            Color groundTint,
            Color fogColor,
            float fogDensity,
            float skyExposure,
            float atmosphereThickness,
            Color ambientSky,
            Color ambientEquator,
            Color ambientGround,
            Color sunColor,
            float sunIntensity)
        {
            EnsureStylizedSkybox();
            if (_runtimeSkybox != null)
            {
                if (_runtimeSkybox.HasProperty("_SkyTint"))
                {
                    _runtimeSkybox.SetColor("_SkyTint", skyTint);
                }

                if (_runtimeSkybox.HasProperty("_GroundColor"))
                {
                    _runtimeSkybox.SetColor("_GroundColor", groundTint);
                }

                if (_runtimeSkybox.HasProperty("_Exposure"))
                {
                    _runtimeSkybox.SetFloat("_Exposure", Mathf.Clamp(skyExposure, 0.5f, 1.6f));
                }

                if (_runtimeSkybox.HasProperty("_AtmosphereThickness"))
                {
                    _runtimeSkybox.SetFloat("_AtmosphereThickness", Mathf.Clamp(atmosphereThickness, 0.35f, 1.4f));
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = Mathf.Clamp(fogDensity, 0.0024f, 0.01f);

            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = Mathf.Clamp(sunIntensity, 0.7f, 1.7f);
            }
        }

        private static void ConfigureRuntimeQuality()
        {
            if (_qualityConfigured)
            {
                return;
            }

            var systemMemory = Mathf.Max(1, SystemInfo.systemMemorySize);
            var graphicsMemory = SystemInfo.graphicsMemorySize > 0
                ? SystemInfo.graphicsMemorySize
                : systemMemory / 2;

            var lowTier = graphicsMemory < 1600 || systemMemory < 3500;
            QualitySettings.antiAliasing = lowTier ? 2 : 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            _qualityConfigured = true;
        }

        private static void EnsureStylizedSkybox()
        {
            if (_runtimeSkybox == null)
            {
                var skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader != null)
                {
                    _runtimeSkybox = new Material(skyShader)
                    {
                        name = "MultiplyRush_RuntimeSky"
                    };
                }
            }

            if (_runtimeSkybox == null)
            {
                return;
            }

            if (_runtimeSkybox.HasProperty("_SkyTint"))
            {
                _runtimeSkybox.SetColor("_SkyTint", new Color(0.45f, 0.64f, 0.88f, 1f));
            }

            if (_runtimeSkybox.HasProperty("_GroundColor"))
            {
                _runtimeSkybox.SetColor("_GroundColor", new Color(0.78f, 0.86f, 0.92f, 1f));
            }

            if (_runtimeSkybox.HasProperty("_SunSize"))
            {
                _runtimeSkybox.SetFloat("_SunSize", 0.024f);
            }

            if (_runtimeSkybox.HasProperty("_SunSizeConvergence"))
            {
                _runtimeSkybox.SetFloat("_SunSizeConvergence", 5.4f);
            }

            if (_runtimeSkybox.HasProperty("_AtmosphereThickness"))
            {
                _runtimeSkybox.SetFloat("_AtmosphereThickness", 0.82f);
            }

            if (_runtimeSkybox.HasProperty("_Exposure"))
            {
                _runtimeSkybox.SetFloat("_Exposure", 1.18f);
            }

            RenderSettings.skybox = _runtimeSkybox;
        }

        private static void EnsureFillLight()
        {
            const string fillLightName = "MR_FillLight";
            var existing = GameObject.Find(fillLightName);
            Light fillLight;
            if (existing != null)
            {
                fillLight = existing.GetComponent<Light>();
            }
            else
            {
                var fillObject = new GameObject(fillLightName);
                fillLight = fillObject.AddComponent<Light>();
            }

            if (fillLight == null)
            {
                return;
            }

            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.34f;
            fillLight.color = new Color(0.62f, 0.72f, 0.9f, 1f);
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(58f, -146f, 0f);
        }
    }
}
