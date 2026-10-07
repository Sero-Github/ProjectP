using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 병합 화면의 블록 슬롯. 블록을 하나만 받으며, 이미 블록이 있으면 새 블록으로 교체하고 기존 블록은 창고로 돌려보낸다.
public class MergeSlot : BlockGrid
{
    // 슬롯의 블록이 들어오거나 빠질 때 호출된다. 병합 결과 미리보기 갱신에 쓴다.
    public event Action Changed;

    public Block Block
    {
        get
        {
            foreach (var pair in PlacedBlocks)
            {
                return pair.Key;
            }

            return null;
        }
    }

    // 블록의 코어 조각이 놓인 칸.
    public Vector2Int CoreCell => Block != null ? PlacedBlocks[Block] : default;

    public override bool TryAccept(Block block, PointerEventData eventData)
    {
        Vector2Int cell = GetCellAtWorldPoint(block.CoreWorldPosition);

        Block existing = Block;
        if (existing == null || existing == block)
        {
            if (!base.TryAccept(block, eventData))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        if (Storage.Instance == null)
        {
            return false;
        }

        // 기존 블록을 뺀 상태로 들어갈 수 있는지 확인하고, 안 되면 기존 블록을 그대로 둔다.
        Vector2Int existingCell = PlacedBlocks[existing];
        RemoveBlock(existing);

        if (!CanPlaceBlock(block, cell))
        {
            PlaceBlock(existing, existingCell);
            return false;
        }

        PlaceBlock(block, cell);
        Storage.Instance.AddBlock(existing);

        Changed?.Invoke();
        return true;
    }

    public override void Detach(Block block)
    {
        base.Detach(block);
        Changed?.Invoke();
    }

    public override void Restore(Block block)
    {
        base.Restore(block);
        Changed?.Invoke();
    }
}