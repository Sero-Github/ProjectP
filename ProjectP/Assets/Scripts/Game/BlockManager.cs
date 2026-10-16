using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    // 🧱 블록 문서의 블록 종류 순서대로 둔다. 인덱스 + 1이 블록 번호이다.
    [SerializeField] private List<Block> blockPrefabs;
    [SerializeField] private GameObject blockPiecePrefab;
    // 인덱스가 core 번호이다. 0은 스탯 블록 색상(흰색), 1~7은 Element 속성 색상이다.
    [SerializeField] private List<Color> coreColors = new List<Color>();
    [SerializeField] private BlockStatData statData;
    readonly private Color defaultCoreColor = Color.white;

    // 속성 블록에 무작위로 부여할 스킬 종류. None은 제외한다.
    private static readonly SkillType[] SkillTypes = Array.FindAll((SkillType[])Enum.GetValues(typeof(SkillType)), type => type != SkillType.None);

    private Dictionary<int, Color> coreColorLookup;

    private void Awake()
    {
        Instance = this;

        coreColorLookup = new Dictionary<int, Color>();
        int count = 0;
        foreach (var color in coreColors)
        {
            coreColorLookup[count] = color;
            count++;
        }
    }

    public Block CreateBlock()
    {
        if (blockPrefabs == null || blockPrefabs.Count == 0)
        {
            return null;
        }

        int prefabIndex = UnityEngine.Random.Range(0, blockPrefabs.Count);
        Block block = Instantiate(blockPrefabs[prefabIndex]);

        block.SetBlockId(prefabIndex + 1);
        block.SetCore(UnityEngine.Random.Range(0, coreColors.Count));
        if (block.HasStat && statData != null)
        {
            block.SetStat(statData.CreateRandomStat(block.BlockId));
        }
        if (block.HasSkill)
        {
            // 스킬 점수는 기획의 점수 표(등급, 칸 수)가 정해지기 전까지 칸 수로 둔다.
            SkillType skill = SkillTypes[UnityEngine.Random.Range(0, SkillTypes.Length)];
            block.SetSkill(skill, block.PieceCount);
        }
        block.ApplyColor(GetColorForCore(block.Core));

        return block;
    }

    // 병합 결과처럼 임의 모양의 블록을 만든다. cells는 어떤 좌표계여도 되며 좌상단 기준으로 정렬된다.
    public Block CreateMergedBlock(int coreId, int grade, BlockStat stat, SkillType skill, int skillScore,
        IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
    {
        if (blockPiecePrefab == null || cells == null || cells.Count == 0)
        {
            return null;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var cell in cells)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
        }

        var min = new Vector2Int(minX, minY);
        var normalizedCells = new List<Vector2Int>(cells.Count);
        foreach (var cell in cells)
        {
            normalizedCells.Add(cell - min);
        }

        var go = new GameObject("Block_Merged", typeof(RectTransform));
        Block block = go.AddComponent<Block>();
        block.Build(coreId, grade, stat, skill, skillScore, normalizedCells, coreCell - min, blockPiecePrefab);
        block.ApplyColor(GetColorForCore(coreId));

        return block;
    }

    public Color GetColorForCore(int coreId)
    {
        if (coreColorLookup != null && coreColorLookup.TryGetValue(coreId, out var color))
        {
            return color;
        }

        return defaultCoreColor;
    }

    public IReadOnlyList<Block> GetAllBlocks()
    {
        return UnityEngine.Object.FindObjectsByType<Block>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    public IReadOnlyList<Block> GetBlocksInBag()
    {
        return Backpack.Instance != null ? Backpack.Instance.RectTransform.GetComponentsInChildren<Block>() : Array.Empty<Block>();
    }

    public IReadOnlyList<Block> GetBlocksInStorage()
    {
        return Storage.Instance != null ? Storage.Instance.RectTransform.GetComponentsInChildren<Block>() : Array.Empty<Block>();
    }
}