using UnityEngine;
using UnityEngine.EventSystems;

// 병합 화면의 블록 슬롯. 블록을 하나만 받으며, 이미 블록이 있으면 새 블록으로 교체하고 기존 블록은 창고로 돌려보낸다.
// 블록이 들어오거나 빠지면 BlockGrid.Changed가 호출되어 병합 결과 미리보기가 갱신된다.
public class MergeSlot : BlockGrid
{
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
            return base.TryAccept(block, eventData);
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
        return true;
    }

    // 슬롯의 블록을 없앤다. 병합 재료로 소모될 때 쓴다.
    public void ClearBlock()
    {
        Block block = Block;
        if (block == null)
        {
            return;
        }

        RemoveBlock(block);
        Destroy(block.gameObject);
    }
}