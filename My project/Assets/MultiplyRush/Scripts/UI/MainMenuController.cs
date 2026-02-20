using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplyRush
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public string gameSceneName = "Game";
        public Text bestLevelText;

        private void Start()
        {
            CanvasRootGuard.NormalizeAllRootCanvasScales();
            EnsureBackgroundBehindContent();

            if (bestLevelText != null)
            {
                bestLevelText.text = "Best Level: " + ProgressionStore.GetBestLevel();
            }
        }

        private void EnsureBackgroundBehindContent()
        {
            var background = GameObject.Find("Background");
            if (background == null)
            {
                return;
            }

            var backgroundRect = background.GetComponent<RectTransform>();
            if (backgroundRect == null)
            {
                return;
            }

            backgroundRect.SetAsFirstSibling();
        }

        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
