using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [SerializeField] private List<Block> blockPrefabs;
    [SerializeField] private GameObject blockPiecePrefab;
    [SerializeField] private List<Color> coreColors = new List<Color>();
    readonly private Color defaultCoreColor = Color.white;

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

        Block prefab = blockPrefabs[UnityEngine.Random.Range(0, blockPrefabs.Count)];
        Block block = Instantiate(prefab);

        block.SetCore(UnityEngine.Random.Range(0, coreColors.Count));
        block.ApplyColor(GetColorForCore(block.Core));

        return block;
    }

    // 병합 결과처럼 임의 모양의 블록을 만든다. cells는 어떤 좌표계여도 되며 좌상단 기준으로 정렬된다.
    public Block CreateMergedBlock(int coreId, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell)
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
        block.Build(coreId, normalizedCells, coreCell - min, blockPiecePrefab, true);
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