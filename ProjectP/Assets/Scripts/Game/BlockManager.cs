using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [SerializeField] private List<Block> blockPrefabs;
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
        return Bag.Instance != null ? Bag.Instance.RectTransform.GetComponentsInChildren<Block>() : Array.Empty<Block>();
    }

    public IReadOnlyList<Block> GetBlocksInStorage()
    {
        return Storage.Instance != null ? Storage.Instance.RectTransform.GetComponentsInChildren<Block>() : Array.Empty<Block>();
    }
}