using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NumberMerge.Save;

namespace NumberMerge.UI
{
    public sealed class DebugScreen : MonoBehaviour
    {
        private const string GameplaySceneName = "Gameplay";

        [SerializeField] private GameObject _overlay;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _clearDataAndRestartButton;

        public static bool IsOpen { get; private set; }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Hide);
            _restartButton.onClick.AddListener(RestartMatch);
            _clearDataAndRestartButton.onClick.AddListener(ClearDataAndRestartMatch);
            SetOpen(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f12Key.wasPressedThisFrame)
            {
                SetOpen(!IsOpen);
            }
        }

        private void OnDestroy()
        {
            IsOpen = false;
        }

        public void Show()
        {
            SetOpen(true);
        }

        public void Hide()
        {
            SetOpen(false);
        }

        private void RestartMatch()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }

        private void ClearDataAndRestartMatch()
        {
            SaveSystem.DeleteAllPlayerDataForTesting();
            SceneManager.LoadScene(GameplaySceneName);
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            _overlay.SetActive(open);
        }
    }
}
