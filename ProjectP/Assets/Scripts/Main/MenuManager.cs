using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private string gameplaySceneName = "Game";
    [SerializeField] private GameObject settingsPanel;

    // 게임 시작
    public void OnStart()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
 
    // [설정] 패널 열기
    public void OnOpenSettings()
    {
        UIManager.Instance.Show(settingsPanel);
    }
 
    // [설정] 패널 닫기
    public void OnCloseSettings()
    {
        UIManager.Instance.Hide(settingsPanel);
    }
 
    // 게임 종료
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}