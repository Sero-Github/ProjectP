using System.Collections.Generic;
using UnityEngine;

public class TestInventory : MonoBehaviour
{
    [SerializeField] private List<Block> blockPrefabs;
    [SerializeField] private int totalBlockCount;

    private void Start()
    {
        SpawnRandomBlocks();
    }

    private void SpawnRandomBlocks()
    {
        if (Inventory.Instance == null || blockPrefabs == null || blockPrefabs.Count == 0)
        {
            return;
        }

        for (int i = 0; i < totalBlockCount; i++)
        {
            Block prefab = blockPrefabs[Random.Range(0, blockPrefabs.Count)];
            Block block = Instantiate(prefab);
            Inventory.Instance.AddBlock(block);
        }
    }
}
