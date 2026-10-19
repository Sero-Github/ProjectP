using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 병합 결과 미리보기. 두 병합 슬롯의 블록을 칸 좌표 그대로 합친 모양을 보여준다.
// 드롭 대상으로 등록하지 않고, 미리보기 블록은 raycast를 꺼서 직접 조작할 수 없다.
public class MergeResultSlot : BlockGrid
{
    // 병합 불가 메시지. 추후 StringTable로 옮긴다.
    private const string SYSTEM_MERGE_NOT_SYNC_CORE = "두 블록의 코어의 종류가 같아야 합니다.";
    private const string SYSTEM_MERGE_NOT_SYNC_STAT = "두 블록의 스탯이 같아야 합니다.";
    private const string SYSTEM_MERGE_NOT_SYNC_SKILL = "두 블록의 스킬이 같아야 합니다.";
    private const string SYSTEM_MERGE_NOT_SYNC_GRADE = "두 블록의 등급이 같아야 합니다.";
    private const string SYSTEM_MERGE_MATERIAL_NOT_ORIGINAL = "소모 블록은 병합하지 않은 원본 블록이어야 합니다.";
    private const string SYSTEM_MERGE_NO_REMAINING_COUNT = "병합할 블록의 남은 병합 횟수가 없습니다.";
    private const string SYSTEM_MERGE_NO_ATTACH_BLOCK = "인접한 블록이 있어야 합니다.";

    // 병합할 블록(좌측). 결과 블록의 코어는 이 블록을 따른다.
    [SerializeField] private MergeSlot targetSlot;
    // 소모 블록(우측).
    [SerializeField] private MergeSlot materialSlot;
    // 병합이 불가능할 때 켤 경고 메시지(선택).
    [SerializeField] private GameObject invalidMessage;
    // 경고 메시지 텍스트(선택).
    [SerializeField] private TMP_Text invalidMessageText;
    // 병합 실행 버튼. 병합할 수 있을 때만 누를 수 있다.
    [SerializeField] private Button mergeButton;

    private static readonly Vector2Int[] NeighborDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private Block previewBlock;
    private bool canMerge;

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
        UpdatePreview();
        SetMergeButtonInteractable(canMerge);
    }

    // 미리보기 블록을 다시 만들고, 만들어졌으면 canMerge를 켠다.
    private void UpdatePreview()
    {
        ClearPreview();
        canMerge = false;

        Block target = targetSlot != null ? targetSlot.Block : null;
        Block material = materialSlot != null ? materialSlot.Block : null;

        if (target == null || material == null)
        {
            SetInvalidMessage(null);
            return;
        }

        HashSet<Vector2Int> cells = GetMergedCells(target, material);

        string invalidReason = GetMergeInvalidReason(target, material, cells);
        SetInvalidMessage(invalidReason);
        if (invalidReason != null)
        {
            return;
        }

        previewBlock = CreateResultBlock(target, material, cells);
        if (previewBlock == null)
        {
            return;
        }

        PlaceBlock(previewBlock, targetSlot.CoreCell);
        previewBlock.SetInteractable(false);
        canMerge = true;
    }

    // [병합] 버튼: 두 슬롯의 블록을 소모하고 병합된 블록을 창고로 보낸다.
    public void OnClickMerge()
    {
        if (!canMerge || Storage.Instance == null || BlockManager.Instance == null)
        {
            return;
        }

        Block target = targetSlot.Block;
        Block material = materialSlot.Block;
        HashSet<Vector2Int> cells = GetMergedCells(target, material);

        // 미리보기 블록은 raycast를 꺼 두었으므로 결과 블록은 새로 만든다.
        Block result = CreateResultBlock(target, material, cells);
        if (result == null)
        {
            return;
        }

        // 슬롯이 비면서 Changed로 미리보기와 버튼 상태가 갱신된다.
        targetSlot.ClearBlock();
        materialSlot.ClearBlock();

        Storage.Instance.AddBlock(result);
    }

    // 결과 블록은 병합할 블록의 코어와 등급을 따르고, 병합 횟수가 1 오르며,
    // 스탯 블록이면 두 스탯을, 속성 블록이면 두 스킬 점수를 합산한다.
    private Block CreateResultBlock(Block target, Block material, HashSet<Vector2Int> cells)
    {
        BlockManager blockManager = BlockManager.Instance;
        if (blockManager == null)
        {
            return null;
        }

        BlockStat stat = target.HasStat ? BlockStat.Merge(target.Stat, material.Stat) : BlockStat.None;
        int skillScore = target.HasSkill ? target.SkillScore + material.SkillScore : 0;
        return blockManager.CreateMergedBlock(target.Core, target.Grade, target.MergeCount + 1, stat, target.Skill, skillScore,
            new List<Vector2Int>(cells), targetSlot.CoreCell);
    }

    // 두 블록이 슬롯에서 차지한 칸을 같은 좌표계로 합친다.
    private HashSet<Vector2Int> GetMergedCells(Block target, Block material)
    {
        var cells = new HashSet<Vector2Int>(targetSlot.GetOccupiedCells(target));
        cells.UnionWith(materialSlot.GetOccupiedCells(material));
        return cells;
    }

    private void SetMergeButtonInteractable(bool interactable)
    {
        if (mergeButton != null)
        {
            mergeButton.interactable = interactable;
        }
    }

    // 코어가 같고(스탯 블록이면 스탯, 속성 블록이면 스킬도 같고), 등급이 같고, 소모 블록이 원본 블록이고,
    // 병합할 블록에 남은 병합 횟수가 있으며, 합친 칸이 하나로 이어져 있어야 병합할 수 있다.
    // 골드/잔여 병합 횟수 조건은 데이터가 생기면 추가한다.
    // 병합할 수 있으면 null, 없으면 경고 메시지를 반환한다.
    private string GetMergeInvalidReason(Block target, Block material, HashSet<Vector2Int> cells)
    {
        if (target.Core != material.Core)
        {
            return SYSTEM_MERGE_NOT_SYNC_CORE;
        }

        if (target.HasStat && target.Stat.Type != material.Stat.Type)
        {
            return SYSTEM_MERGE_NOT_SYNC_STAT;
        }

        if (target.HasSkill && target.Skill != material.Skill)
        {
            return SYSTEM_MERGE_NOT_SYNC_SKILL;
        }

        if (target.Grade != material.Grade)
        {
            return SYSTEM_MERGE_NOT_SYNC_GRADE;
        }

        if (material.IsMerged)
        {
            return SYSTEM_MERGE_MATERIAL_NOT_ORIGINAL;
        }

        if (target.RemainingMerges <= 0)
        {
            return SYSTEM_MERGE_NO_REMAINING_COUNT;
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