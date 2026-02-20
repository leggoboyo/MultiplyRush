using UnityEngine;
using UnityEngine.UI;

namespace MultiplyRush
{
    public static class CanvasRootGuard
    {
        public static void NormalizeAllRootCanvasScales()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas)
                {
                    continue;
                }

                var rect = canvas.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                if (rect.localScale != Vector3.one)
                {
                    rect.localScale = Vector3.one;
                }
            }
        }
    }
}
