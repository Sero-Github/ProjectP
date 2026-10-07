using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 병합 결과 미리보기. 두 병합 슬롯의 블록을 칸 좌표 그대로 합친 모양을 보여준다.
// 드롭 대상으로 등록하지 않고, 미리보기 블록은 raycast를 꺼서 직접 조작할 수 없다.
public class MergeResultSlot : BlockGrid
{
    // 병합할 블록(좌측). 결과 블록의 코어는 이 블록을 따른다.
    [SerializeField] private MergeSlot targetSlot;
    // 소모 블록(우측). 원본 블록만 사용할 수 있다.
    [SerializeField] private MergeSlot materialSlot;
    // 병합이 불가능할 때 켤 표시(선택).
    [SerializeField] private GameObject invalidMark;

    private static readonly Vector2Int[] NeighborDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private Block previewBlock;

    // base를 호출하지 않아 BlockDropTargets에 등록되지 않는다.
    protected override void OnEnable()
    {
        if (targetSlot != null)
        {
            targetSlot.Changed += RefreshPreview;
        }

        if (materialSlot != null)
        {
            materialSlot.Changed += RefreshPreview;
        }

        RefreshPreview();
    }

    protected override void OnDisable()
    {
        if (targetSlot != null)
        {
            targetSlot.Changed -= RefreshPreview;
        }

        if (materialSlot != null)
        {
            materialSlot.Changed -= RefreshPreview;
        }
    }

    public override bool TryAccept(Block block, PointerEventData eventData)
    {
        return false;
    }

    private void SetInvalidMark(bool active)
    {
        if (invalidMark != null)
        {
            invalidMark.SetActive(active);
        }
    }

    public void RefreshPreview()
    {
        ClearPreview();

        Block target = targetSlot != null ? targetSlot.Block : null;
        Block material = materialSlot != null ? materialSlot.Block : null;

        if (target == null || material == null)
        {
            SetInvalidMark(false);
            return;
        }

        var cells = new HashSet<Vector2Int>(targetSlot.GetOccupiedCells(target));
        cells.UnionWith(materialSlot.GetOccupiedCells(material));

        if (!CanMerge(target, material, cells))
        {
            SetInvalidMark(true);
            return;
        }

        SetInvalidMark(false);

        BlockManager blockManager = BlockManager.Instance;
        if (blockManager == null)
        {
            return;
        }

        Vector2Int coreCell = targetSlot.CoreCell;
        previewBlock = blockManager.CreateMergedBlock(target.Core, new List<Vector2Int>(cells), coreCell);
        if (previewBlock == null)
        {
            return;
        }

        PlaceBlock(previewBlock, coreCell);
        previewBlock.SetInteractable(false);
    }

    // 코어가 같고, 소모 블록이 원본 블록이며, 합친 칸이 하나로 이어져 있어야 병합할 수 있다.
    // 등급/골드/잔여 병합 횟수 조건은 데이터가 생기면 추가한다.
    private bool CanMerge(Block target, Block material, HashSet<Vector2Int> cells)
    {
        if (target.Core != material.Core)
        {
            return false;
        }

        if (material.IsMerged)
        {
            return false;
        }

        return IsConnected(cells);
    }

    private static bool IsConnected(HashSet<Vector2Int> cells)
    {
        if (cells.Count == 0)
        {
            return false;
        }

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        foreach (var cell in cells)
        {
            queue.Enqueue(cell);
            visited.Add(cell);
            break;
        }

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            foreach (var direction in NeighborDirections)
            {
                Vector2Int next = cell + direction;
                if (cells.Contains(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == cells.Count;
    }

    private void ClearPreview()
    {
        if (previewBlock == null)
        {
            return;
        }

        RemoveBlock(previewBlock);
        Destroy(previewBlock.gameObject);
        previewBlock = null;
    }
}