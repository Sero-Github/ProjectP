using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [Tooltip("블록 프리팹, 코어 색상, 스탯 점수 등 블록 생성 설정")]
    [SerializeField] private BlockSettings settings;

    // 속성 블록에 무작위로 부여할 스킬 종류. None은 제외한다.
    private static readonly SkillType[] SkillTypes = Array.FindAll((SkillType[])Enum.GetValues(typeof(SkillType)), type => type != SkillType.None);

    private void Awake()
    {
        Instance = this;

        if (settings == null)
        {
            Debug.LogError($"{name}: BlockSettings가 연결되지 않았습니다.", this);
        }
    }

    public Block CreateBlock()
    {
        if (settings == null || settings.BlockPrefabs.Count == 0)
        {
            return null;
        }

        int prefabIndex = UnityEngine.Random.Range(0, settings.BlockPrefabs.Count);
        Block block = Instantiate(settings.BlockPrefabs[prefabIndex]);

        block.SetBlockId(prefabIndex + 1);
        block.SetGrade(settings.DropGrade);
        block.SetCore(UnityEngine.Random.Range(0, settings.CoreCount));
        if (block.HasStat && settings.StatData != null)
        {
            block.SetStat(settings.StatData.CreateRandomStat(block.BlockId));
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
    public Block CreateMergedBlock(int coreId, int grade, int mergeCount, BlockStat stat, SkillType skill, int skillScore,
        IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
    {
        GameObject blockPiecePrefab = settings != null ? settings.BlockPiecePrefab : null;
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
        block.Build(coreId, grade, mergeCount, stat, skill, skillScore, normalizedCells, coreCell - min, blockPiecePrefab);
        block.ApplyColor(GetColorForCore(coreId));

        return block;
    }

    // 저장된 블록을 다시 만든다. 블록 번호에 맞는 프리팹이 있으면 프리팹으로, 없으면(병합 블록) 저장된 모양으로 조립한다.
    public Block CreateBlock(BlockSaveData data)
    {
        if (data == null)
        {
            return null;
        }

        var stat = new BlockStat(data.StatType, data.statValue);
        Block prefab = settings != null ? settings.GetBlockPrefab(data.blockId) : null;

        if (prefab != null)
        {
            Block block = Instantiate(prefab);
            block.SetBlockId(data.blockId);
            block.SetGrade(data.grade);
            block.SetMergeCount(data.mergeCount);
            // SetStat, SetSkill은 core에 따라 값을 거르므로 core를 먼저 정한다.
            block.SetCore(data.core);
            block.SetStat(stat);
            block.SetSkill(data.SkillType, data.skillScore);
            block.ApplyColor(GetColorForCore(block.Core));
            return block;
        }

        if (data.pieces == null || data.pieces.Count == 0)
        {
            Debug.LogWarning($"저장된 블록(번호 {data.blockId})의 모양 정보가 없어 불러오지 못했습니다.");
            return null;
        }

        // 저장된 조각 오프셋은 코어 칸이 (0,0)이다.
        Block merged = CreateMergedBlock(data.core, data.grade, data.mergeCount, stat, data.SkillType, data.skillScore,
            data.pieces, Vector2Int.zero);
        if (merged != null)
        {
            merged.SetBlockId(data.blockId);
        }

        return merged;
    }

    public Color GetColorForCore(int coreId)
    {
        return settings != null ? settings.GetCoreColor(coreId) : Color.white;
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