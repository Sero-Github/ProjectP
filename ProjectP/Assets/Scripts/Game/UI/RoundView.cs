using TMPro;
using UnityEngine;

// 현재 스테이지와 라운드를 "스테이지 - 라운드" 형식으로 보여준다. (Ex. 1 - 1)
public class RoundView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void OnEnable()
    {
        RoundManager.RoundStarted += Refresh;

        RoundManager manager = RoundManager.Instance;
        if (manager != null && manager.CurrentStage > 0)
        {
            Refresh(manager.CurrentStage, manager.CurrentRound);
        }
    }

    private void OnDisable()
    {
        RoundManager.RoundStarted -= Refresh;
    }

    private void Refresh(int stage, int round)
    {
        if (text != null)
        {
            text.text = $"{stage} - {round}";
        }
    }
}