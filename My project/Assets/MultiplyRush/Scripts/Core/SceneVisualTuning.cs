using UnityEngine;
using UnityEngine.Rendering;

namespace MultiplyRush
{
    public static class SceneVisualTuning
    {
        private static readonly Color MenuBackground = new Color(0.06f, 0.08f, 0.14f, 1f);
        private static readonly Color GameBackground = new Color(0.72f, 0.84f, 0.95f, 1f);

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
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = GameBackground;
                mainCamera.nearClipPlane = 0.05f;
                mainCamera.farClipPlane = 450f;
            }

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type != LightType.Directional)
                {
                    continue;
                }

                light.color = new Color(1f, 0.95f, 0.86f, 1f);
                light.intensity = 1.22f;
                light.transform.rotation = Quaternion.Euler(43f, 33f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.54f, 0.67f, 0.79f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.38f, 0.48f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.2f, 0.26f, 1f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.62f, 0.74f, 0.86f, 1f);
            RenderSettings.fogDensity = 0.0115f;
        }
    }
}
