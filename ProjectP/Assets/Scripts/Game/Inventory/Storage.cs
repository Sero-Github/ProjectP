using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 창고. ScrollRect의 Content에 붙인다.
// 블록마다 슬롯을 하나씩 만들어 가로 columns칸 격자로 배치하고, 행 수에 맞춰 Content 높이를 늘린다.
[RequireComponent(typeof(RectTransform))]
public class Storage : MonoBehaviour, IBlockDropTarget
{
    public static Storage Instance { get; private set; }

    [SerializeField] private int columns = 4;
    [SerializeField] private float slotLength = 135f;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private float padding = 20f;
    [SerializeField] private float pieceDisplaySize = 30f;
    [SerializeField] private float slotOutlineThickness = 3f;
    [SerializeField] private Color slotOutlineColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color slotFillColor = new Color(1f, 1f, 1f, 0.3f);
    private RectTransform dropArea;

    private RectTransform rectTransform;
    private readonly List<RectTransform> slots = new List<RectTransform>();
    private readonly Dictionary<Block, RectTransform> slotByBlock = new Dictionary<Block, RectTransform>();

    private Block detachedBlock;

    public RectTransform RectTransform => rectTransform;
    public int BlockCount => slots.Count;

    private void Awake()
    {
        Instance = this;
        EnsureInitialized();
        Relayout();
    }

    // 인벤토리 패널이 꺼져 있으면 Awake가 아직 불리지 않았을 수 있어,
    // 그 상태에서 AddBlock이 호출돼도 동작하도록 초기화를 따로 둔다.
    private void EnsureInitialized()
    {
        if (rectTransform != null)
        {
            return;
        }

        rectTransform = (RectTransform)transform;

        if (dropArea == null)
        {
            ScrollRect scrollRect = GetComponentInParent<ScrollRect>(true);
            dropArea = scrollRect != null ? (RectTransform)scrollRect.transform : rectTransform;
        }
    }

    private void OnEnable()
    {
        BlockDropTargets.Register(this);
    }

    private void OnDisable()
    {
        BlockDropTargets.Unregister(this);
    }

    private void Start()
    {
        // 씬에 미리 배치해 둔 블록도 슬롯으로 옮긴다.
        var initialBlocks = new List<Block>();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var block = rectTransform.GetChild(i).GetComponent<Block>();
            if (block != null)
            {
                initialBlocks.Add(block);
            }
        }

        foreach (var block in initialBlocks)
        {
            AddBlock(block);
        }
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera camera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(dropArea, screenPoint, camera);
    }

    public void AddBlock(Block block)
    {
        EnsureInitialized();

        RectTransform slot = CreateSlot();
        slots.Add(slot);
        slotByBlock[block] = slot;

        PutInSlot(block, slot);
        Relayout();
    }

    // 블록의 슬롯을 없애고 남은 슬롯을 앞으로 당긴다. 블록 자체의 처리(이동/파괴)는 호출자가 맡는다.
    public void RemoveBlock(Block block)
    {
        if (!slotByBlock.TryGetValue(block, out RectTransform slot))
        {
            return;
        }

        if (block.transform.parent == slot)
        {
            block.transform.SetParent(rectTransform, false);
        }

        slotByBlock.Remove(block);
        slots.Remove(slot);
        Destroy(slot.gameObject);

        if (detachedBlock == block)
        {
            detachedBlock = null;
        }

        Relayout();
    }

    private void PutInSlot(Block block, RectTransform slot)
    {
        block.transform.SetParent(slot, false);
        block.ResetLayoutToCenter();

        float scale = pieceDisplaySize / Block.PieceUnit;
        block.SetDisplayScale(scale);
        block.RectTransform.anchoredPosition = -block.GetVisualCenter() * scale;

        block.AttachTo(this);
    }

    private RectTransform CreateSlot()
    {
        var go = new GameObject("Slot", typeof(RectTransform), typeof(Image));
        var slot = (RectTransform)go.transform;
        slot.SetParent(rectTransform, false);
        slot.anchorMin = new Vector2(0f, 1f);
        slot.anchorMax = new Vector2(0f, 1f);
        slot.pivot = new Vector2(0f, 1f);
        slot.sizeDelta = new Vector2(slotLength, slotLength);

        var outline = go.GetComponent<Image>();
        outline.color = slotOutlineColor;
        outline.raycastTarget = false;

        var fillGo = new GameObject("fill", typeof(RectTransform), typeof(Image));
        var fillRect = (RectTransform)fillGo.transform;
        fillRect.SetParent(slot, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-slotOutlineThickness * 2f, -slotOutlineThickness * 2f);

        var fill = fillGo.GetComponent<Image>();
        fill.color = slotFillColor;
        fill.raycastTarget = false;

        return slot;
    }

    // 슬롯을 순서대로 가로 columns칸씩 배치하고, 행 수에 맞춰 Content 높이를 조정한다.
    private void Relayout()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;

            slots[i].anchoredPosition = new Vector2(
                padding + column * (slotLength + spacing),
                -(padding + row * (slotLength + spacing)));
        }

        int rowCount = Mathf.CeilToInt(slots.Count / (float)columns);
        float height = padding * 2f + rowCount * slotLength + Mathf.Max(0, rowCount - 1) * spacing;

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
    }

    public bool TryAccept(Block block, PointerEventData eventData)
    {
        if (!ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
        {
            return false;
        }

        // 창고에서 꺼낸 블록을 다시 창고에 놓으면 원래 슬롯으로 돌려보낸다.
        if (block == detachedBlock)
        {
            Restore(block);
            return true;
        }

        AddBlock(block);
        return true;
    }

    // 드래그 중에는 빈 슬롯을 남겨둬서, 드롭에 실패하면 같은 자리로 돌아올 수 있게 한다.
    public void Detach(Block block)
    {
        detachedBlock = block;
    }

    public void Restore(Block block)
    {
        if (detachedBlock != block)
        {
            return;
        }

        if (slotByBlock.TryGetValue(block, out RectTransform slot))
        {
            PutInSlot(block, slot);
        }

        detachedBlock = null;
    }

    public void Release(Block block)
    {
        RemoveBlock(block);
    }
}