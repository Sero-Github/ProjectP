using UnityEngine;

public class TestStorage : MonoBehaviour
{
    [SerializeField] private int totalBlockCount;

    private void Start()
    {
        SpawnRandomBlocks();
    }

    private void SpawnRandomBlocks()
    {
        if (Storage.Instance == null || BlockManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < totalBlockCount; i++)
        {
            Block block = BlockManager.Instance.CreateBlock();
            if (block != null)
            {
                Storage.Instance.AddBlock(block);
            }
        }
    }
}