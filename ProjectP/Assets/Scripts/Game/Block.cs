using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private List<GameObject> pieces;
    [SerializeField] private GameObject core;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera eventCamera;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    private Board placedBoard;
    private Vector2Int placedCell;

    private Board dragOriginBoard;
    private Vector2Int dragOriginCell;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            rootCanvas = parentCanvas.rootCanvas;
        }
    }

    public IReadOnlyList<Vector2Int> GetPieceOffsets(float cellSize)
    {
        var offsets = new List<Vector2Int>(pieces.Count);
        Vector2 corePosition = ((RectTransform)core.transform).anchoredPosition;

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
            NormalizeAnchorToCenter();
        }
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

        rectTransform.position = worldPosition;
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
        Board board = Board.Instance;

        if (board != null &&
            board.TryGetCellAtScreenPoint(eventData.position, eventData.pressEventCamera, out Vector2Int cell) &&
            board.CanPlaceBlock(this, cell))
        {
            board.PlaceBlock(this, cell);
            placedBoard = board;
            placedCell = cell;
            return;
        }

        Inventory inventory = Inventory.Instance;
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
    }
}