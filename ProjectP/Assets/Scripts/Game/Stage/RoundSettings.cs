using UnityEngine;

// RoundManager의 스테이지, 라운드 진행 설정. 씬을 고치지 않고 이 에셋만 수정해 라운드 진행을 바꾼다.
[CreateAssetMenu(fileName = "RoundSettings", menuName = "ProjectP/Stage/Round Settings")]
public class RoundSettings : ScriptableObject
{
    [Tooltip("스테이지, 라운드, 원형, 삼각형, 사각형, 오각형 열을 가진 CSV")]
    [SerializeField] private TextAsset stageCsv;

    [Tooltip("한 라운드에서 모든 몬스터를 소환하는 시간(초)")]
    [Min(1f)]
    [SerializeField] private float roundDuration = 100f;

    [Tooltip("라운드에서 승리하고 다음 라운드를 시작하기까지 기다리는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float transitionDelay = 2f;

    [Tooltip("플레이어가 사망하고 다시 나타나 이전 라운드를 시작하기까지 기다리는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float defeatDelay = 3f;

    public TextAsset StageCsv => stageCsv;
    public float RoundDuration => roundDuration;
    public float TransitionDelay => transitionDelay;
    public float DefeatDelay => defeatDelay;
}
