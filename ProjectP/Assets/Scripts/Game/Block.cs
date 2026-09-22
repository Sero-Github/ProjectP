using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private int core;
    [SerializeField] private GameObject coreBlock;
    [SerializeField] private List<GameObject> pieces;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera eventCamera;
    private Vector2 defaultSizeDelta;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    private Bag placedBoard;
    private Vector2Int placedCell;

    private Bag dragOriginBoard;
    private Vector2Int dragOriginCell;

    public RectTransform RectTransform => rectTransform;
    public int Core => core;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        defaultSizeDelta = rectTransform.sizeDelta;
        RefreshRootCanvas();
    }

    private void OnTransformParentChanged()
    {
        RefreshRootCanvas();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        dragOriginBoard = placedBoard;
        dragOriginCell = placedCell;

        if (placedBoard != null)
        {
            placedBoard.RemoveBlock(this, placedCell);
            placedBoard = null;
        }

        eventCamera = eventData.pressEventCamera;

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            SetDisplayScale(1f);
            NormalizeAnchorToCenter();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null)
        {
            return;
        }

        var canvasRect = (RectTransform)rootCanvas.transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Bag board = Bag.Instance;

        if (board != null &&
            board.TryGetCellAtScreenPoint(eventData.position, eventData.pressEventCamera, out Vector2Int cell) &&
            board.CanPlaceBlock(this, cell))
        {
            board.PlaceBlock(this, cell);
            placedBoard = board;
            placedCell = cell;
            return;
        }

        Storage inventory = Storage.Instance;
        if (inventory != null && inventory.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
        {
            inventory.AddBlock(this);
            placedBoard = null;
            return;
        }

        if (dragOriginBoard != null)
        {
            dragOriginBoard.PlaceBlock(this, dragOriginCell);
            placedBoard = dragOriginBoard;
            placedCell = dragOriginCell;
            return;
        }

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;

        if (Storage.Instance != null && originalParent == Storage.Instance.RectTransform)
        {
            Storage.Instance.ApplyDisplayScale(this);
        }
    }

    public void SetCore(int coreId)
    {
        core = coreId;
    }

    public void ApplyColor(Color color)
    {
        if (pieces == null)
        {
            return;
        }

        foreach (var piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            foreach (var image in piece.GetComponentsInChildren<Image>(true))
            {
                if (image.name.Contains("fill"))
                {
                    image.color = color;
                }
            }
        }
    }

    private void RefreshRootCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
    }

    public IReadOnlyList<Vector2Int> GetPieceOffsets(float cellSize)
    {
        var offsets = new List<Vector2Int>(pieces.Count);
        Vector2 corePosition = ((RectTransform)coreBlock.transform).anchoredPosition;

        foreach (var piece in pieces)
        {
            Vector2 piecePosition = ((RectTransform)piece.transform).anchoredPosition;
            Vector2 delta = piecePosition - corePosition;

            int dx = Mathf.RoundToInt(delta.x / cellSize);
            int dy = Mathf.RoundToInt(-delta.y / cellSize);

            offsets.Add(new Vector2Int(dx, dy));
        }

        return offsets;
    }

    public Vector2 GetFootprintSize(float cellSize)
    {
        var offsets = GetPieceOffsets(cellSize);

        if (offsets.Count == 0)
        {
            return new Vector2(cellSize, cellSize);
        }

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var offset in offsets)
        {
            minX = Mathf.Min(minX, offset.x);
            maxX = Mathf.Max(maxX, offset.x);
            minY = Mathf.Min(minY, offset.y);
            maxY = Mathf.Max(maxY, offset.y);
        }

        float width = (maxX - minX + 1) * cellSize;
        float height = (maxY - minY + 1) * cellSize;
        return new Vector2(width, height);
    }

    public void SetDisplayScale(float scale)
    {
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    // Inventory의 HorizontalLayoutGroup은 자식의 sizeDelta/pivot을 그대로 믿고 세로 중앙 정렬을 계산한다.
    // 모든 블록 프리팹의 루트 sizeDelta/pivot이 모양과 무관하게 고정되어 있어서 그대로 두면
    // core를 기준으로 비대칭인 모양일수록 정렬이 어긋나 보인다. 조각들의 실제 경계 박스에 맞춰
    // sizeDelta/pivot을 보정해 레이아웃 그룹이 "진짜 그림"을 기준으로 정렬하게 만든다.
    public void NormalizeVisualBounds(float cellSize)
    {
        if (pieces == null || pieces.Count == 0)
        {
            return;
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var piece in pieces)
        {
            Vector2 piecePosition = ((RectTransform)piece.transform).anchoredPosition;

            minX = Mathf.Min(minX, piecePosition.x - cellSize / 2f);
            maxX = Mathf.Max(maxX, piecePosition.x + cellSize / 2f);
            minY = Mathf.Min(minY, piecePosition.y - cellSize / 2f);
            maxY = Mathf.Max(maxY, piecePosition.y + cellSize / 2f);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        if (width <= 0f || height <= 0f)
        {
            return;
        }

        Vector3 worldPosition = rectTransform.position;

        rectTransform.pivot = new Vector2(-minX / width, -minY / height);
        rectTransform.sizeDelta = new Vector2(width, height);

        rectTransform.position = worldPosition;
    }

    // Horizontal/Vertical Layout Group이 자식의 anchor를 좌상단 등으로 바꿔놓기 때문에,
    // Board의 anchoredPosition 계산(anchorMin=anchorMax=pivot=0.5 가정)과 맞춰주기 위해
    // 드래그를 시작해 레이아웃 그룹의 영향에서 벗어나는 시점에 anchor를 중앙으로 되돌린다.
    private void NormalizeAnchorToCenter()
    {
        Vector3 worldPosition = rectTransform.position;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = defaultSizeDelta;

        rectTransform.position = worldPosition;
    }
}