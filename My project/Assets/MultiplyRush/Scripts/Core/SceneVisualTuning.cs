using UnityEngine;
using UnityEngine.Rendering;

namespace MultiplyRush
{
    public static class SceneVisualTuning
    {
        private static readonly Color MenuBackground = new Color(0.06f, 0.08f, 0.14f, 1f);
        private static readonly Color GameBackground = new Color(0.5f, 0.67f, 0.88f, 1f);
        private static Material _runtimeSkybox;
        private static bool _qualityConfigured;
        private static VisualTier _visualTier = VisualTier.Medium;

        private enum VisualTier
        {
            Low = 0,
            Medium = 1,
            High = 2
        }

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
                mainCamera.farClipPlane = 520f;
                mainCamera.allowHDR = _visualTier != VisualTier.Low;
                mainCamera.allowMSAA = true;
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
                light.intensity = _visualTier == VisualTier.High ? 1.46f : (_visualTier == VisualTier.Medium ? 1.36f : 1.24f);
                light.transform.rotation = Quaternion.Euler(39f, 31f, 0f);
                light.shadows = _visualTier == VisualTier.Low ? LightShadows.None : LightShadows.Soft;
                light.shadowStrength = _visualTier == VisualTier.High ? 0.84f : 0.72f;
                light.shadowBias = _visualTier == VisualTier.High ? 0.035f : 0.05f;
                light.shadowNormalBias = _visualTier == VisualTier.High ? 0.28f : 0.36f;
                primaryDirectional = light;
            }

            EnsureFillLight();
            EnsureRimLight();
            if (primaryDirectional != null)
            {
                RenderSettings.sun = primaryDirectional;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.78f, 0.94f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.48f, 0.62f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.33f, 1f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.66f, 0.76f, 0.9f, 1f);
            RenderSettings.fogDensity = _visualTier == VisualTier.High ? 0.0048f : (_visualTier == VisualTier.Medium ? 0.0051f : 0.0056f);
            RenderSettings.reflectionIntensity = _visualTier == VisualTier.High ? 0.72f : (_visualTier == VisualTier.Medium ? 0.66f : 0.58f);
            RenderSettings.flareStrength = _visualTier == VisualTier.Low ? 0.84f : 1f;
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
            var exposureBoost = _visualTier == VisualTier.High ? 0.1f : (_visualTier == VisualTier.Medium ? 0.05f : 0f);
            var atmosphereBoost = _visualTier == VisualTier.Low ? -0.04f : 0.06f;
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
                    _runtimeSkybox.SetFloat("_Exposure", Mathf.Clamp(skyExposure + exposureBoost, 0.5f, 1.65f));
                }

                if (_runtimeSkybox.HasProperty("_AtmosphereThickness"))
                {
                    _runtimeSkybox.SetFloat("_AtmosphereThickness", Mathf.Clamp(atmosphereThickness + atmosphereBoost, 0.35f, 1.5f));
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            var fogScale = _visualTier == VisualTier.High ? 0.92f : (_visualTier == VisualTier.Medium ? 0.98f : 1.08f);
            RenderSettings.fogDensity = Mathf.Clamp(fogDensity * fogScale, 0.0024f, 0.0105f);

            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = Mathf.Clamp(sunIntensity + (_visualTier == VisualTier.High ? 0.06f : 0f), 0.7f, 1.8f);
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

            _visualTier = ResolveVisualTier(systemMemory, graphicsMemory);
            switch (_visualTier)
            {
                case VisualTier.Low:
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.lodBias = 0.76f;
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 18f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.pixelLightCount = 1;
                    break;
                case VisualTier.Medium:
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.lodBias = 0.98f;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowDistance = 42f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.pixelLightCount = 2;
                    break;
                default:
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.lodBias = 1.14f;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 62f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.pixelLightCount = 3;
                    break;
            }

            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            _qualityConfigured = true;
        }

        private static VisualTier ResolveVisualTier(int systemMemory, int graphicsMemory)
        {
            if (graphicsMemory < 1700 || systemMemory < 3500)
            {
                return VisualTier.Low;
            }

            if (graphicsMemory < 3200 || systemMemory < 5600)
            {
                return VisualTier.Medium;
            }

            return VisualTier.High;
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
                _runtimeSkybox.SetFloat("_AtmosphereThickness", _visualTier == VisualTier.High ? 0.94f : 0.82f);
            }

            if (_runtimeSkybox.HasProperty("_Exposure"))
            {
                _runtimeSkybox.SetFloat("_Exposure", _visualTier == VisualTier.High ? 1.24f : 1.16f);
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
            fillLight.intensity = _visualTier == VisualTier.High ? 0.44f : 0.36f;
            fillLight.color = new Color(0.62f, 0.72f, 0.9f, 1f);
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(58f, -146f, 0f);
        }

        private static void EnsureRimLight()
        {
            const string rimLightName = "MR_RimLight";
            var existing = GameObject.Find(rimLightName);
            Light rimLight;
            if (existing != null)
            {
                rimLight = existing.GetComponent<Light>();
            }
            else
            {
                var rimObject = new GameObject(rimLightName);
                rimLight = rimObject.AddComponent<Light>();
            }

            if (rimLight == null)
            {
                return;
            }

            rimLight.type = LightType.Directional;
            rimLight.intensity = _visualTier == VisualTier.High ? 0.3f : 0.2f;
            rimLight.color = new Color(0.56f, 0.76f, 0.98f, 1f);
            rimLight.shadows = LightShadows.None;
            rimLight.transform.rotation = Quaternion.Euler(24f, 170f, 0f);
        }
    }
}
