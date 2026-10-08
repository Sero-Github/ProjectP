using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Block : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 블록 프리팹에서 조각 한 칸의 크기. 그리드마다 다른 칸 크기는 표시 배율로 맞춘다.
    public const float PieceUnit = 50f;

    [SerializeField] private int core;
    [SerializeField] private bool isMerged;
    [SerializeField] private GameObject coreBlock;
    [SerializeField] private List<GameObject> pieces;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera eventCamera;
    private Vector2 defaultSizeDelta;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    private IBlockDropTarget currentTarget;
    private IBlockDropTarget dragOrigin;

    public RectTransform RectTransform => rectTransform;
    public int Core => core;
    public bool IsMerged => isMerged;
    public IBlockDropTarget CurrentTarget => currentTarget;

    // 코어 조각의 위치(블록 로컬 좌표, 배율 적용 전)와 월드 위치.
    public Vector2 CoreLocalPosition => ((RectTransform)coreBlock.transform).anchoredPosition;
    public Vector3 CoreWorldPosition => coreBlock.transform.position;

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
        // 비활성 계층으로 옮겨진 동안에는 OnTransformParentChanged를 받지 못했을 수 있다.
        RefreshRootCanvas();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        dragOrigin = currentTarget;
        currentTarget = null;
        dragOrigin?.Detach(this);

        eventCamera = eventData.pressEventCamera;

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            ResetLayoutToCenter();
            SetDisplayScale(GetDragScale());
            MoveVisualCenterTo(eventData.position);
        }
    }

    // 드래그 중에는 지금 화면의 그리드(가방 또는 병합 슬롯) 칸과 같은 크기로 보여서,
    // 놓기 전에 들어갈 모양을 그대로 확인할 수 있게 한다.
    private float GetDragScale()
    {
        foreach (var target in BlockDropTargets.All)
        {
            if (target is BlockGrid grid && grid.isActiveAndEnabled && grid.CellSize > 0f)
            {
                return grid.CellSize / PieceUnit;
            }
        }

        return 1f;
    }

    // 블록 모양의 중심이 포인터 위치에 오도록 옮긴다.
    private void MoveVisualCenterTo(Vector2 screenPoint)
    {
        var canvasRect = (RectTransform)rootCanvas.transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint - GetVisualCenter() * rectTransform.localScale.x;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null)
        {
            return;
        }

        MoveVisualCenterTo(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // TryAccept 안에서 대상 목록이 바뀔 수 있어 복사본으로 순회한다.
        var targets = new List<IBlockDropTarget>(BlockDropTargets.All);
        foreach (var target in targets)
        {
            if (target.TryAccept(this, eventData))
            {
                if (dragOrigin != null && dragOrigin != target)
                {
                    dragOrigin.Release(this);
                }
                return;
            }
        }

        if (dragOrigin != null)
        {
            dragOrigin.Restore(this);
            return;
        }

        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    public void SetCore(int coreId)
    {
        core = coreId;
    }

    public void AttachTo(IBlockDropTarget target)
    {
        currentTarget = target;
    }

    // 병합 결과처럼 프리팹이 없는 모양의 블록을 조각 프리팹으로 조립한다.
    // cells는 좌상단이 (0,0)인 칸 좌표이며, coreCell은 그 중 코어 칸이다.
    public void Build(int coreId, IReadOnlyList<Vector2Int> cells, Vector2Int coreCell, GameObject piecePrefab, bool merged)
    {
        core = coreId;
        isMerged = merged;
        pieces = new List<GameObject>(cells.Count);

        defaultSizeDelta = new Vector2(PieceUnit, PieceUnit);
        rectTransform.sizeDelta = defaultSizeDelta;

        foreach (var cell in cells)
        {
            GameObject piece = Instantiate(piecePrefab, rectTransform);
            var pieceRect = (RectTransform)piece.transform;
            pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
            pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
            pieceRect.pivot = new Vector2(0.5f, 0.5f);
            pieceRect.sizeDelta = new Vector2(PieceUnit, PieceUnit);

            Vector2Int delta = cell - coreCell;
            pieceRect.anchoredPosition = new Vector2(delta.x * PieceUnit, -delta.y * PieceUnit);

            pieces.Add(piece);
            if (cell == coreCell)
            {
                coreBlock = piece;
            }
        }
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

    // 코어 칸을 (0,0)으로 하는 각 조각의 칸 오프셋. y는 아래쪽이 +이다.
    public IReadOnlyList<Vector2Int> GetPieceOffsets()
    {
        var offsets = new List<Vector2Int>(pieces.Count);
        Vector2 corePosition = ((RectTransform)coreBlock.transform).anchoredPosition;

        foreach (var piece in pieces)
        {
            Vector2 piecePosition = ((RectTransform)piece.transform).anchoredPosition;
            Vector2 delta = piecePosition - corePosition;

            int dx = Mathf.RoundToInt(delta.x / PieceUnit);
            int dy = Mathf.RoundToInt(-delta.y / PieceUnit);

            offsets.Add(new Vector2Int(dx, dy));
        }

        return offsets;
    }

    public Vector2 GetFootprintSize()
    {
        var offsets = GetPieceOffsets();

        if (offsets.Count == 0)
        {
            return new Vector2(PieceUnit, PieceUnit);
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

        float width = (maxX - minX + 1) * PieceUnit;
        float height = (maxY - minY + 1) * PieceUnit;
        return new Vector2(width, height);
    }

    public void SetDisplayScale(float scale)
    {
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    // 조각 이미지의 raycast를 끄면 드래그할 수 없는 표시 전용 블록이 된다.
    public void SetInteractable(bool interactable)
    {
        foreach (var image in GetComponentsInChildren<Image>(true))
        {
            image.raycastTarget = interactable;
        }
    }

    // 조각들이 그리는 경계 박스의 중심(블록 로컬 좌표, 배율 적용 전).
    // 코어가 모양의 중앙에 있지 않은 블록을 슬롯 가운데에 맞출 때 쓴다.
    public Vector2 GetVisualCenter()
    {
        if (pieces == null || pieces.Count == 0)
        {
            return Vector2.zero;
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var piece in pieces)
        {
            Vector2 piecePosition = ((RectTransform)piece.transform).anchoredPosition;

            minX = Mathf.Min(minX, piecePosition.x);
            maxX = Mathf.Max(maxX, piecePosition.x);
            minY = Mathf.Min(minY, piecePosition.y);
            maxY = Mathf.Max(maxY, piecePosition.y);
        }

        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }

    // BlockGrid와 창고 슬롯의 anchoredPosition 계산(anchorMin=anchorMax=pivot=0.5 가정)과 맞추기 위해
    // anchor/pivot/sizeDelta를 중앙 기준 기본값으로 되돌린다.
    public void ResetLayoutToCenter()
    {
        Vector3 worldPosition = rectTransform.position;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = defaultSizeDelta;

        rectTransform.position = worldPosition;
    }
}