using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [Tooltip("블록 프리팹, 코어 색상, 스탯 계수 등 블록 생성 설정")]
    [SerializeField] private BlockSettings settings;

    // 속성 블록에 무작위로 부여할 스킬 종류. None은 제외한다.
    private static readonly SkillType[] SkillTypes = Array.FindAll((SkillType[])Enum.GetValues(typeof(SkillType)), type => type != SkillType.None);

    // 연결되지 않았으면 null이다.
    public BlockSettings Settings => settings;

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
        block.SetCore(UnityEngine.Random.Range(0, settings.CoreCount));
        if (block.HasStat)
        {
            // 스탯 수치는 블록 점수 × BlockStatData 계수로 계산하므로 종류만 고른다.
            block.SetStat(new BlockStat(BlockStatData.GetRandomStatType()));
        }
        if (block.HasSkill)
        {
            // 스킬 점수는 블록 점수를 쓰므로 종류만 고른다.
            block.SetSkill(SkillTypes[UnityEngine.Random.Range(0, SkillTypes.Length)]);
        }
        SetScore(block, BlockScore.FromCells(block.PieceCount, settings.ScorePerCell), 0);
        block.ApplyColor(GetColorForCore(block.Core));

        return block;
    }

    // 병합 결과처럼 임의 모양의 블록을 만든다. cells는 어떤 좌표계여도 되며 좌상단 기준으로 정렬된다.
    // baseScore는 강화하지 않은 원본 점수이며, 만들어진 블록은 0강이다.
    public Block CreateMergedBlock(int coreId, int mergeCount, BlockStat stat, SkillType skill,
        int baseScore, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
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
        block.Build(coreId, mergeCount, stat, skill, normalizedCells, coreCell - min, blockPiecePrefab);
        SetScore(block, baseScore, 0);
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

        var stat = new BlockStat(data.StatType);
        int baseScore = GetSavedBaseScore(data);
        Block prefab = settings != null ? settings.GetBlockPrefab(data.blockId) : null;

        if (prefab != null)
        {
            Block block = Instantiate(prefab);
            block.SetBlockId(data.blockId);
            block.SetMergeCount(data.mergeCount);
            // SetStat, SetSkill은 core에 따라 값을 거르므로 core를 먼저 정한다.
            block.SetCore(data.core);
            block.SetStat(stat);
            block.SetSkill(data.SkillType);
            SetScore(block, baseScore, data.enhanceLevel);
            block.ApplyColor(GetColorForCore(block.Core));
            return block;
        }

        if (data.pieces == null || data.pieces.Count == 0)
        {
            Debug.LogWarning($"저장된 블록(번호 {data.blockId})의 모양 정보가 없어 불러오지 못했습니다.");
            return null;
        }

        // 저장된 조각 오프셋은 코어 칸이 (0,0)이다.
        Block merged = CreateMergedBlock(data.core, data.mergeCount, stat, data.SkillType,
            baseScore, data.pieces, Vector2Int.zero);
        if (merged != null)
        {
            merged.SetBlockId(data.blockId);
            SetScore(merged, baseScore, data.enhanceLevel);
        }

        return merged;
    }

    // 저장된 원본 점수. 점수 필드가 없던 옛 저장 파일은 0으로 읽히므로 옛 값으로 바꿔 읽는다.
    // 스탯 블록은 옛 스탯 값이 이미 점수와 같은 단위이고, 스킬 블록의 옛 스킬 점수는 칸 수의 합이다.
    // 저장 형식 버전(M0-2)이 생기면 버전 0인 파일에만 적용하도록 옮긴다.
    private int GetSavedBaseScore(BlockSaveData data)
    {
        if (data.baseScore > 0)
        {
            return data.baseScore;
        }

        int scorePerCell = settings != null ? settings.ScorePerCell : 0;
        return data.core == 0 ? data.statValue : data.skillScore * scorePerCell;
    }

    // 최종 점수의 강화 계산은 BlockSettings의 1강당 증가율을 쓴다.
    private void SetScore(Block block, int baseScore, int enhanceLevel)
    {
        int enhanceRatePercent = settings != null ? settings.EnhanceRatePercent : 0;
        block.SetScore(baseScore, enhanceLevel, enhanceRatePercent);
    }

    // 남은 병합 횟수 = 최대 합 − 합. 블록은 설정 에셋을 모르므로 최대 합은 BlockSettings에서 가져온다.
    // 블록 설정이 없으면 0이며, 이때는 병합 블록을 조립할 조각 프리팹도 없어 병합할 수 없다.
    public int GetRemainingMerges(Block block)
    {
        if (block == null || settings == null)
        {
            return 0;
        }

        return BlockScore.GetRemainingMerges(block.MergeCount, settings.MaxMergeCount);
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