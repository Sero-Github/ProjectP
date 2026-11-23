using System.Collections.Generic;
using UnityEngine;

// BlockManager가 블록을 만들 때 쓰는 설정. 씬을 고치지 않고 이 에셋만 수정해 블록 구성을 바꾼다.
[CreateAssetMenu(fileName = "BlockSettings", menuName = "ProjectP/Block/Block Settings")]
public class BlockSettings : ScriptableObject
{
    private static readonly Color DefaultCoreColor = Color.white;

    // 필드 이름은 에셋에 저장된 목록을 그대로 읽기 위해 바꾸지 않는다.
    [Tooltip("블록 모양 프리팹(BlockShape). 🧱 블록 문서의 블록 종류 순서대로 둡니다. 인덱스 + 1이 블록 번호입니다.")]
    [SerializeField] private List<BlockShape> blockPrefabs = new List<BlockShape>();

    [Tooltip("병합 블록처럼 임의 모양의 블록을 조립할 때 쓰는 조각 프리팹")]
    [SerializeField] private GameObject blockPiecePrefab;

    [Tooltip("인덱스가 core 번호입니다. 0은 스탯 블록 색상(흰색), 1~7은 Element 속성 색상입니다.")]
    [SerializeField] private List<Color> coreColors = new List<Color>();

    [Tooltip("스탯별 점수 1당 수치. 스탯 수치 = 블록 점수 × 이 값. 비어 있으면 기본 계수(속성 공격력 3, 나머지 1)를 씁니다.")]
    [SerializeField] private BlockStatData statData;

    [Header("블록 점수")]
    [Tooltip("블록 한 칸당 점수. 원본 블록 점수 = 칸 수 × 이 값")]
    [Min(0)]
    [SerializeField] private int scorePerCell = 5;

    [Tooltip("블록 하나를 병합할 수 있는 최대 횟수(합). 기획은 모든 블록 3회입니다.")]
    [Min(0)]
    [SerializeField] private int maxMergeCount = 3;

    [Tooltip("1강마다 원본 점수(3합 0강)에 더해지는 비율(%)")]
    [Min(0)]
    [SerializeField] private int enhanceRatePercent = 20;

    public IReadOnlyList<BlockShape> BlockShapes => blockPrefabs;
    public GameObject BlockPiecePrefab => blockPiecePrefab;
    public int CoreCount => coreColors.Count;
    public BlockStatData StatData => statData;
    public int ScorePerCell => scorePerCell;
    public int MaxMergeCount => maxMergeCount;
    public int EnhanceRatePercent => enhanceRatePercent;

    // 블록 번호(1부터)에 맞는 모양 프리팹. 없으면(병합 블록) null이다.
    public BlockShape GetBlockShape(int blockId)
    {
        int index = blockId - 1;
        return index >= 0 && index < blockPrefabs.Count ? blockPrefabs[index] : null;
    }

    public Color GetCoreColor(int coreId)
    {
        return coreId >= 0 && coreId < coreColors.Count ? coreColors[coreId] : DefaultCoreColor;
    }
}
