using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectP.Game
{
    // ESC 키로 일시정지 메뉴(반투명 검은 화면 + 버튼)를 열고 닫는다.
    public class PauseMenu : MonoBehaviour
    {
        private const string MainSceneName = "Main";

        [SerializeField] private GameObject pausePanel;

        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        private void Start()
        {
            SetPaused(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetPaused(!IsPaused);
            }
        }

        // [돌아가기] 검은 화면 해제
        public void OnResume()
        {
            SetPaused(false);
        }

        // [메인화면으로] Main 씬으로 이동
        public void OnMainMenu()
        {
            SceneManager.LoadScene(MainSceneName);
        }

        // [게임종료]
        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPaused(bool paused)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(paused);
            }
        }
    }
}
