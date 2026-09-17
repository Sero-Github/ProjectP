using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Board : MonoBehaviour
{
    public static Board Instance { get; private set; }

    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private float cellSize = 50f;
    [SerializeField] private Color cellOutlineColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Color cellFillColor = new Color(1f, 1f, 1f, 0.15f);

    private RectTransform rectTransform;
    private bool[,] occupied;
    private readonly List<GameObject> cellVisuals = new List<GameObject>();

    public RectTransform RectTransform => rectTransform;
    public Vector2Int GridSize => new Vector2Int(columns, rows);
    public float CellSize => cellSize;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        BuildGrid();
    }

    private void BuildGrid()
    {
        occupied = new bool[columns, rows];

        foreach (var visual in cellVisuals)
        {
            Destroy(visual);
        }
        cellVisuals.Clear();

        rectTransform.sizeDelta = new Vector2(columns * cellSize, rows * cellSize);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                CreateCellVisual(new Vector2Int(x, y));
            }
        }
    }

    private void CreateCellVisual(Vector2Int cell)
    {
        Vector2 anchoredPosition = GetCellAnchoredPosition(cell);

        var outline = CreateCellImage($"Cell {cell.x},{cell.y} outline", cellOutlineColor, cellSize, anchoredPosition);
        var fill = CreateCellImage($"Cell {cell.x},{cell.y} fill", cellFillColor, cellSize - 2f, anchoredPosition);

        cellVisuals.Add(outline.gameObject);
        cellVisuals.Add(fill.gameObject);
    }

    private Image CreateCellImage(string name, Color color, float size, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(rectTransform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = anchoredPosition;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    public bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    public bool IsCellFree(Vector2Int cell)
    {
        return IsInBounds(cell) && !occupied[cell.x, cell.y];
    }

    public Vector2 GetCellAnchoredPosition(Vector2Int cell)
    {
        float width = columns * cellSize;
        float height = rows * cellSize;

        float x = -width / 2f + cellSize / 2f + cell.x * cellSize;
        float y = height / 2f - cellSize / 2f - cell.y * cellSize;

        return new Vector2(x, y);
    }

    public bool TryGetCellAtScreenPoint(Vector2 screenPoint, Camera camera, out Vector2Int cell)
    {
        cell = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, out Vector2 localPoint))
        {
            return false;
        }

        float width = columns * cellSize;
        float height = rows * cellSize;

        int x = Mathf.RoundToInt((localPoint.x - (-width / 2f + cellSize / 2f)) / cellSize);
        int y = Mathf.RoundToInt(((height / 2f - cellSize / 2f) - localPoint.y) / cellSize);

        cell = new Vector2Int(x, y);
        return true;
    }

    public bool CanPlaceBlock(Block block, Vector2Int originCell)
    {
        foreach (var offset in block.GetPieceOffsets(cellSize))
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
        foreach (var offset in block.GetPieceOffsets(cellSize))
        {
            Vector2Int cell = originCell + offset;
            occupied[cell.x, cell.y] = true;
        }

        block.RectTransform.SetParent(rectTransform, false);
        block.RectTransform.anchoredPosition = GetCellAnchoredPosition(originCell);
    }

    public void RemoveBlock(Block block, Vector2Int originCell)
    {
        foreach (var offset in block.GetPieceOffsets(cellSize))
        {
            Vector2Int cell = originCell + offset;
            if (IsInBounds(cell))
            {
                occupied[cell.x, cell.y] = false;
            }
        }
    }
}