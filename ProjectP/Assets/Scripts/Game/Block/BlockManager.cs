using System;
using System.Collections.Generic;
using UnityEngine;

// 블록을 만든다. 드롭, 병합, 불러오기 모두 빈 오브젝트에 블록 종류에 맞는 클래스(StatBlock, SkillBlock, ActiveSkillBlock)를 붙이고
// 조각 프리팹으로 모양을 조립하는 같은 방식을 쓴다. 블록 모양은 BlockSettings의 BlockShape 프리팹에서 읽는다.
public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [Tooltip("블록 모양 프리팹, 코어 색상, 스탯 계수 등 블록 생성 설정")]
    [SerializeField] private BlockSettings settings;

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

    // 드롭 블록을 만든다. 모양은 블록 종류 9개 중 하나, 종류는 코어 8개(스탯 1 + 속성 7) 중 하나를 같은 확률로 고른다.
    // 액티브 블록은 보스 처치 보상이라 여기서 만들지 않는다.
    public Block CreateBlock()
    {
        if (settings == null || settings.BlockShapes.Count == 0)
        {
            return null;
        }

        int shapeIndex = UnityEngine.Random.Range(0, settings.BlockShapes.Count);
        BlockShape shape = settings.BlockShapes[shapeIndex];
        if (shape == null)
        {
            return null;
        }

        List<Vector2Int> cells = shape.GetCells(out Vector2Int coreCell);
        int core = UnityEngine.Random.Range(0, settings.CoreCount);

        Block block;
        if (core == 0)
        {
            // 스탯 수치는 블록 점수 × BlockStatData 계수로 계산하므로 종류만 고른다.
            var statBlock = CreateBlockObject<StatBlock>(shape.name, cells, coreCell);
            statBlock?.SetStat(BlockStatData.GetRandomStatType());
            block = statBlock;
        }
        else
        {
            // 스킬 점수는 블록 점수를 쓰므로 스킬 종류, 속성, 타격 우선순위만 고른다.
            var skillBlock = CreateBlockObject<SkillBlock>(shape.name, cells, coreCell);
            skillBlock?.SetSkill((Element)core, SkillBlock.GetRandomSkillType(), SkillBlock.GetRandomTargetPriority());
            block = skillBlock;
        }

        if (block == null)
        {
            return null;
        }

        block.SetBlockId(shapeIndex + 1);
        SetScore(block, BlockScore.FromCells(block.PieceCount, settings.ScorePerCell), 0);
        ApplyColor(block);
        return block;
    }

    // 병합 결과 블록을 만든다. 병합할 블록(target)과 같은 클래스이며 종류별 정보도 target을 따른다.
    // cells는 어떤 좌표계여도 되며 좌상단 기준으로 정렬된다. baseScore는 강화하지 않은 원본 점수이며, 만들어진 블록은 0강이다.
    public Block CreateMergedBlock(Block target, int mergeCount, int baseScore, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
    {
        if (target == null)
        {
            return null;
        }

        Block block = CreateBlockObject(target.GetType(), "Block_Merged", cells, coreCell);
        if (block == null)
        {
            return null;
        }

        block.CopyKindDataFrom(target);
        block.SetMergeCount(mergeCount);
        SetScore(block, baseScore, 0);
        ApplyColor(block);
        return block;
    }

    // 저장된 블록을 다시 만든다. 블록 번호에 맞는 모양 프리팹이 있으면 그 모양으로, 없으면(병합 블록) 저장된 조각 오프셋으로 조립한다.
    public Block CreateBlock(BlockSaveData data)
    {
        if (data == null)
        {
            return null;
        }

        BlockShape shape = settings != null ? settings.GetBlockShape(data.blockId) : null;
        List<Vector2Int> cells;
        Vector2Int coreCell;
        if (shape != null)
        {
            cells = shape.GetCells(out coreCell);
        }
        else if (data.pieces != null && data.pieces.Count > 0)
        {
            // 저장된 조각 오프셋은 코어 칸이 (0,0)이다.
            cells = data.pieces;
            coreCell = Vector2Int.zero;
        }
        else
        {
            Debug.LogWarning($"저장된 블록(번호 {data.blockId})의 모양 정보가 없어 불러오지 못했습니다.");
            return null;
        }

        string objectName = shape != null ? shape.name : "Block_Merged";
        Block block = CreateBlockObject(GetBlockType(data.Kind), objectName, cells, coreCell);
        if (block == null)
        {
            return null;
        }

        block.SetBlockId(data.blockId);
        block.SetMergeCount(data.mergeCount);
        block.ReadKindData(data);
        SetScore(block, GetSavedBaseScore(data), data.enhanceLevel);
        ApplyColor(block);
        return block;
    }

    private static Type GetBlockType(BlockKind kind)
    {
        switch (kind)
        {
            case BlockKind.Skill:
                return typeof(SkillBlock);
            case BlockKind.ActiveSkill:
                return typeof(ActiveSkillBlock);
            default:
                return typeof(StatBlock);
        }
    }

    private T CreateBlockObject<T>(string objectName, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell) where T : Block
    {
        return (T)CreateBlockObject(typeof(T), objectName, cells, coreCell);
    }

    // 빈 UI 오브젝트에 blockType 클래스를 붙이고 조각을 조립한다. cells는 좌상단 기준으로 정렬한다.
    private Block CreateBlockObject(Type blockType, string objectName, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
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

        var go = new GameObject(objectName, typeof(RectTransform));
        go.layer = blockPiecePrefab.layer;
        var block = (Block)go.AddComponent(blockType);
        block.Build(normalizedCells, coreCell - min, blockPiecePrefab);
        return block;
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

    private void ApplyColor(Block block)
    {
        block.ApplyColor(GetColorForCore(block.ColorIndex));
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
