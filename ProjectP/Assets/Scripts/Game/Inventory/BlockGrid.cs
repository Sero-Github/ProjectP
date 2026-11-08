using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 칸 단위로 블록을 배치하는 그리드 공통 로직. 가방과 병합 슬롯이 상속한다.
// 칸 좌표 계산은 RectTransform의 rect 중심을 기준으로 하므로 pivot/anchor 설정과 무관하게 동작한다.
[RequireComponent(typeof(RectTransform))]
public abstract class BlockGrid : MonoBehaviour, IBlockDropTarget
{
    [SerializeField] protected int columns = 3;
    [SerializeField] protected int rows = 3;
    // 켜면 칸 크기를 RectTransform 크기에 맞춰 자동 계산한다. 끄면 cellSize로 RectTransform 크기를 정한다.
    [SerializeField] protected bool fitToRect = true;
    [SerializeField] protected float padding = 0f;
    [SerializeField] protected float cellSize = 50f;
    [SerializeField] protected float cellOutlineThickness = 1f;
    [SerializeField] protected Color cellOutlineColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] protected Color cellFillColor = new Color(1f, 1f, 1f, 0.15f);

    protected RectTransform rectTransform;
    private bool[,] occupied;
    private readonly List<RectTransform> cellOutlines = new List<RectTransform>();
    private readonly List<RectTransform> cellFills = new List<RectTransform>();
    private readonly Dictionary<Block, Vector2Int> placedBlocks = new Dictionary<Block, Vector2Int>();
    private bool isRefreshingLayout;

    private Block detachedBlock;
    private Vector2Int detachedCell;

    // 블록이 배치되거나 빠질 때 호출된다. 드래그를 시작해 블록을 들어 올린 경우도 빠진 것으로 본다.
    public event Action Changed;

    public RectTransform RectTransform => rectTransform;
    public Vector2Int GridSize => new Vector2Int(columns, rows);
    public float CellSize => cellSize;
    public IReadOnlyDictionary<Block, Vector2Int> PlacedBlocks => placedBlocks;

    protected virtual void Awake()
    {
        EnsureInitialized();
    }

    // 인벤토리 패널이 꺼져 있으면 Awake가 아직 불리지 않았을 수 있어,
    // 그 상태에서 저장된 블록을 배치해도 동작하도록 초기화를 따로 둔다.
    public void EnsureInitialized()
    {
        if (rectTransform != null)
        {
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        BuildGrid();
        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void OnEnable()
    {
        BlockDropTargets.Register(this);

        // 꺼진 상태에서 배치된 블록은 그때의 크기로 계산되었으므로 켜질 때 다시 맞춘다.
        if (occupied != null)
        {
            RefreshLayout();
        }
    }

    protected virtual void OnDisable()
    {
        BlockDropTargets.Unregister(this);
    }

    // 해상도나 부모 레이아웃이 바뀌어 크기가 변하면 칸과 블록을 다시 맞춘다.
    private void OnRectTransformDimensionsChange()
    {
        if (occupied != null)
        {
            RefreshLayout();
        }
    }

    private void BuildGrid()
    {
        occupied = new bool[columns, rows];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                cellOutlines.Add(CreateCellImage($"Cell {x},{y} outline", cellOutlineColor));
                cellFills.Add(CreateCellImage($"Cell {x},{y} fill", cellFillColor));
            }
        }

        RefreshLayout();
    }

    private RectTransform CreateCellImage(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(rectTransform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    // 칸 크기를 다시 계산하고 칸 이미지와 배치된 블록의 위치/배율을 갱신한다.
    private void RefreshLayout()
    {
        if (isRefreshingLayout)
        {
            return;
        }

        isRefreshingLayout = true;

        if (fitToRect)
        {
            Rect rect = rectTransform.rect;
            float fitWidth = (rect.width - padding * 2f) / columns;
            float fitHeight = (rect.height - padding * 2f) / rows;
            cellSize = Mathf.Max(0f, Mathf.Min(fitWidth, fitHeight));
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(columns * cellSize + padding * 2f, rows * cellSize + padding * 2f);
        }

        float fillSize = Mathf.Max(0f, cellSize - cellOutlineThickness * 2f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = y * columns + x;
                Vector2 position = GetCellAnchoredPosition(new Vector2Int(x, y));

                cellOutlines[index].sizeDelta = new Vector2(cellSize, cellSize);
                cellOutlines[index].anchoredPosition = position;
                cellFills[index].sizeDelta = new Vector2(fillSize, fillSize);
                cellFills[index].anchoredPosition = position;
            }
        }

        foreach (var pair in placedBlocks)
        {
            ApplyBlockTransform(pair.Key, pair.Value);
        }

        isRefreshingLayout = false;
    }

    public bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    public bool IsCellFree(Vector2Int cell)
    {
        return IsInBounds(cell) && !occupied[cell.x, cell.y];
    }

    // anchor가 중앙(0.5, 0.5)인 자식 기준의 칸 중심 위치. 격자는 rect 중앙에 정렬된다.
    public Vector2 GetCellAnchoredPosition(Vector2Int cell)
    {
        float width = columns * cellSize;
        float height = rows * cellSize;

        float x = -width / 2f + cellSize / 2f + cell.x * cellSize;
        float y = height / 2f - cellSize / 2f - cell.y * cellSize;

        return new Vector2(x, y);
    }

    // RectTransform 로컬 좌표(pivot 기준)를 칸 좌표로 바꾼다. 범위 밖이어도 계산된 칸을 돌려준다.
    public Vector2Int GetCellAtLocalPoint(Vector2 localPoint)
    {
        Vector2 point = localPoint - rectTransform.rect.center;

        float width = columns * cellSize;
        float height = rows * cellSize;

        int x = Mathf.FloorToInt((point.x + width / 2f) / cellSize);
        int y = Mathf.FloorToInt((height / 2f - point.y) / cellSize);

        return new Vector2Int(x, y);
    }

    public Vector2Int GetCellAtWorldPoint(Vector3 worldPoint)
    {
        return GetCellAtLocalPoint(rectTransform.InverseTransformPoint(worldPoint));
    }

    public bool TryGetCellAtScreenPoint(Vector2 screenPoint, Camera camera, out Vector2Int cell)
    {
        cell = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, out Vector2 localPoint))
        {
            return false;
        }

        cell = GetCellAtLocalPoint(localPoint);
        return true;
    }

    public bool CanPlaceBlock(Block block, Vector2Int originCell)
    {
        EnsureInitialized();

        foreach (var offset in block.GetPieceOffsets())
        {
            if (!IsCellFree(originCell + offset))
            {
                return false;
            }
        }

        return true;
    }

    public void PlaceBlock(Block block, Vector2Int originCell)
    {
        EnsureInitialized();

        foreach (var offset in block.GetPieceOffsets())
        {
            Vector2Int cell = originCell + offset;
            occupied[cell.x, cell.y] = true;
        }

        placedBlocks[block] = originCell;

        block.RectTransform.SetParent(rectTransform, false);
        block.ResetLayoutToCenter();
        ApplyBlockTransform(block, originCell);
        block.AttachTo(this);

        Changed?.Invoke();
    }

    // 블록의 코어 조각이 originCell 중심에 오도록 위치와 배율을 맞춘다.
    private void ApplyBlockTransform(Block block, Vector2Int originCell)
    {
        float scale = cellSize / Block.PieceUnit;
        block.SetDisplayScale(scale);
        block.RectTransform.anchoredPosition = GetCellAnchoredPosition(originCell) - block.CoreLocalPosition * scale;
    }

    public void RemoveBlock(Block block)
    {
        if (!placedBlocks.TryGetValue(block, out Vector2Int originCell))
        {
            return;
        }

        foreach (var offset in block.GetPieceOffsets())
        {
            Vector2Int cell = originCell + offset;
            if (IsInBounds(cell))
            {
                occupied[cell.x, cell.y] = false;
            }
        }

        placedBlocks.Remove(block);

        Changed?.Invoke();
    }

    // 블록이 실제로 차지하고 있는 그리드 칸 목록.
    public List<Vector2Int> GetOccupiedCells(Block block)
    {
        var cells = new List<Vector2Int>();
        if (!placedBlocks.TryGetValue(block, out Vector2Int originCell))
        {
            return cells;
        }

        foreach (var offset in block.GetPieceOffsets())
        {
            cells.Add(originCell + offset);
        }

        return cells;
    }

    // 드래그로 들어 올려 잠시 빠져 있는 블록과 원래 칸. 드롭에 실패하면 이 칸으로 돌아온다.
    public bool TryGetDetachedBlock(out Block block, out Vector2Int cell)
    {
        block = detachedBlock;
        cell = detachedCell;
        return block != null;
    }

    // 드래그 중인 블록의 코어 조각이 놓인 칸을 기준으로 배치한다.
    // 포인터 위치가 아니라 실제 그려진 조각 위치를 쓰기 때문에, 보이는 모양 그대로 칸에 들어간다.
    public virtual bool TryAccept(Block block, PointerEventData eventData)
    {
        Vector2Int cell = GetCellAtWorldPoint(block.CoreWorldPosition);

        if (!CanPlaceBlock(block, cell))
        {
            return false;
        }

        PlaceBlock(block, cell);
        return true;
    }

    public virtual void Detach(Block block)
    {
        if (placedBlocks.TryGetValue(block, out Vector2Int cell))
        {
            detachedBlock = block;
            detachedCell = cell;
        }

        RemoveBlock(block);
    }

    public virtual void Restore(Block block)
    {
        if (detachedBlock == block)
        {
            PlaceBlock(block, detachedCell);
        }

        detachedBlock = null;
    }

    public virtual void Release(Block block)
    {
        if (detachedBlock == block)
        {
            detachedBlock = null;
        }
    }
}
