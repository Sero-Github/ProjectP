using System.Collections.Generic;
using UnityEngine;

// 가지고 있는 블록(창고, 가방, 병합 슬롯)을 SaveData에 담고, 저장된 블록을 다시 만들어 배치한다.
// 인벤토리 패널이 꺼져 있으면 가방과 창고의 Instance가 아직 없으므로 비활성 오브젝트까지 찾는다.
public static class InventorySaver
{
    public static void Capture(SaveData save)
    {
        if (save == null)
        {
            return;
        }

        save.blocks.Clear();
        var added = new HashSet<Block>();

        void Add(Block block, bool inBackpack, Vector2Int cell)
        {
            if (block != null && added.Add(block))
            {
                save.blocks.Add(BlockSaveData.From(block, inBackpack, cell));
            }
        }

        // 드래그로 들어 올린 블록은 원래 칸에 있는 것으로 저장한다.
        Backpack backpack = FindBackpack();
        if (backpack != null)
        {
            foreach (var pair in backpack.PlacedBlocks)
            {
                Add(pair.Key, true, pair.Value);
            }

            if (backpack.TryGetDetachedBlock(out Block detached, out Vector2Int detachedCell))
            {
                Add(detached, true, detachedCell);
            }
        }

        Storage storage = FindStorage();
        if (storage != null)
        {
            foreach (var block in storage.GetBlocks())
            {
                Add(block, false, default);
            }
        }

        // 병합 슬롯에 올려 둔 블록은 불러올 때 창고로 돌려보낸다.
        foreach (var slot in Object.FindObjectsByType<MergeSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var pair in slot.PlacedBlocks)
            {
                Add(pair.Key, false, default);
            }

            if (slot.TryGetDetachedBlock(out Block detached, out _))
            {
                Add(detached, false, default);
            }
        }
    }

    // 저장된 블록을 만들어 가방과 창고에 넣는다. 가방 칸에 들어가지 못하는 블록은 창고로 보낸다.
    public static void Restore(SaveData save)
    {
        if (save == null || save.blocks.Count == 0)
        {
            return;
        }

        BlockManager blockManager = BlockManager.Instance;
        if (blockManager == null)
        {
            Debug.LogError("BlockManager가 없어 저장된 블록을 불러오지 못했습니다.");
            return;
        }

        Backpack backpack = FindBackpack();
        Storage storage = FindStorage();

        foreach (var data in save.blocks)
        {
            Block block = blockManager.CreateBlock(data);
            if (block == null)
            {
                continue;
            }

            if (data.inBackpack)
            {
                if (backpack != null && backpack.CanPlaceBlock(block, data.cell))
                {
                    backpack.PlaceBlock(block, data.cell);
                    continue;
                }

                Debug.LogWarning($"저장된 블록을 가방 {data.cell} 칸에 놓을 수 없어 창고로 옮깁니다.");
            }

            if (storage != null)
            {
                storage.AddBlock(block);
            }
            else
            {
                Debug.LogError("창고가 없어 저장된 블록을 불러오지 못했습니다.");
                Object.Destroy(block.gameObject);
            }
        }
    }

    private static Backpack FindBackpack()
    {
        return Backpack.Instance != null ? Backpack.Instance : Object.FindFirstObjectByType<Backpack>(FindObjectsInactive.Include);
    }

    private static Storage FindStorage()
    {
        return Storage.Instance != null ? Storage.Instance : Object.FindFirstObjectByType<Storage>(FindObjectsInactive.Include);
    }
}
