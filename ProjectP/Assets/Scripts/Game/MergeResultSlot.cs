using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 병합 결과 미리보기. 두 병합 슬롯의 블록을 칸 좌표 그대로 합친 모양을 보여준다.
// 드롭 대상으로 등록하지 않고, 미리보기 블록은 raycast를 꺼서 직접 조작할 수 없다.
public class MergeResultSlot : BlockGrid
{
    // 병합 불가 메시지. 추후 StringTable로 옮긴다.
    private const string SYSTEM_MERGE_NOT_SYNC_CORE = "두 블록의 코어의 종류가 같아야 합니다.";
    private const string SYSTEM_MERGE_DUPLICATION_ATTACH_BLOCK = "두 블록이 병합된 블록일 수 없습니다.";
    private const string SYSTEM_MERGE_NO_ATTACH_BLOCK = "인접한 블록이 있어야 합니다.";

    // 병합할 블록(좌측). 결과 블록의 코어는 이 블록을 따른다.
    [SerializeField] private MergeSlot targetSlot;
    // 소모 블록(우측).
    [SerializeField] private MergeSlot materialSlot;
    // 병합이 불가능할 때 켤 경고 메시지(선택).
    [SerializeField] private GameObject invalidMessage;
    // 경고 메시지 텍스트(선택).
    [SerializeField] private TMP_Text invalidMessageText;

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

    // message가 null이면 경고 메시지를 숨긴다.
    private void SetInvalidMessage(string message)
    {
        if (invalidMessageText != null && message != null)
        {
            invalidMessageText.text = message;
        }

        if (invalidMessage != null)
        {
            invalidMessage.SetActive(message != null);
        }
    }

    public void RefreshPreview()
    {
        ClearPreview();

        Block target = targetSlot != null ? targetSlot.Block : null;
        Block material = materialSlot != null ? materialSlot.Block : null;

        if (target == null || material == null)
        {
            SetInvalidMessage(null);
            return;
        }

        var cells = new HashSet<Vector2Int>(targetSlot.GetOccupiedCells(target));
        cells.UnionWith(materialSlot.GetOccupiedCells(material));

        string invalidReason = GetMergeInvalidReason(target, material, cells);
        SetInvalidMessage(invalidReason);
        if (invalidReason != null)
        {
            return;
        }

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

    // 코어가 같고, 두 블록 모두 원본 블록이며, 합친 칸이 하나로 이어져 있어야 병합할 수 있다.
    // 등급/골드/잔여 병합 횟수 조건은 데이터가 생기면 추가한다.
    // 병합할 수 있으면 null, 없으면 경고 메시지를 반환한다.
    private string GetMergeInvalidReason(Block target, Block material, HashSet<Vector2Int> cells)
    {
        if (target.Core != material.Core)
        {
            return SYSTEM_MERGE_NOT_SYNC_CORE;
        }

        if (target.IsMerged || material.IsMerged)
        {
            return SYSTEM_MERGE_DUPLICATION_ATTACH_BLOCK;
        }

        if (!IsConnected(cells))
        {
            return SYSTEM_MERGE_NO_ATTACH_BLOCK;
        }

        return null;
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