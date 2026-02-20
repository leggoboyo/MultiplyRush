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

            if (bestLevelText != null)
            {
                bestLevelText.text = "Best Level: " + ProgressionStore.GetBestLevel();
            }
        }

        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
