using System.Collections.Generic;
using UnityEngine;

// BlockManager가 블록을 만들 때 쓰는 설정. 씬을 고치지 않고 이 에셋만 수정해 블록 구성을 바꾼다.
[CreateAssetMenu(fileName = "BlockSettings", menuName = "ProjectP/Block/Block Settings")]
public class BlockSettings : ScriptableObject
{
    private static readonly Color DefaultCoreColor = Color.white;

    [Tooltip("🧱 블록 문서의 블록 종류 순서대로 둡니다. 인덱스 + 1이 블록 번호입니다.")]
    [SerializeField] private List<Block> blockPrefabs = new List<Block>();

    [Tooltip("병합 블록처럼 임의 모양의 블록을 조립할 때 쓰는 조각 프리팹")]
    [SerializeField] private GameObject blockPiecePrefab;

    [Tooltip("인덱스가 core 번호입니다. 0은 스탯 블록 색상(흰색), 1~7은 Element 속성 색상입니다.")]
    [SerializeField] private List<Color> coreColors = new List<Color>();

    [Tooltip("스탯 블록의 블록 종류별 스탯 점수")]
    [SerializeField] private BlockStatData statData;

    [Tooltip("드랍되는 블록의 등급. 등급별 드랍이 정해지기 전까지는 1등급만 드랍합니다.")]
    [Min(1)]
    [SerializeField] private int dropGrade = 1;

    public IReadOnlyList<Block> BlockPrefabs => blockPrefabs;
    public GameObject BlockPiecePrefab => blockPiecePrefab;
    public int CoreCount => coreColors.Count;
    public BlockStatData StatData => statData;
    public int DropGrade => dropGrade;

    // 블록 번호(1부터)에 맞는 프리팹. 없으면 null이다.
    public Block GetBlockPrefab(int blockId)
    {
        int index = blockId - 1;
        return index >= 0 && index < blockPrefabs.Count ? blockPrefabs[index] : null;
    }

    public Color GetCoreColor(int coreId)
    {
        return coreId >= 0 && coreId < coreColors.Count ? coreColors[coreId] : DefaultCoreColor;
    }
}
