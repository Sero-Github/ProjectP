using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [SerializeField] private float slotSize = 50f;

    private RectTransform rectTransform;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        Instance = this;
        rectTransform = (RectTransform)transform;
    }

    private void Start()
    {
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var block = rectTransform.GetChild(i).GetComponent<Block>();
            if (block != null)
            {
                ApplyDisplayScale(block);
            }
        }
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera camera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
    }

    public void AddBlock(Block block)
    {
        block.transform.SetParent(rectTransform, false);
        ApplyDisplayScale(block);
    }

    public void ApplyDisplayScale(Block block)
    {
        Board board = Board.Instance;
        if (board == null)
        {
            block.SetDisplayScale(1f);
            return;
        }

        block.NormalizeVisualBounds(board.CellSize);

        Vector2 footprint = block.GetFootprintSize(board.CellSize);
        float largestSide = Mathf.Max(footprint.x, footprint.y);
        float scale = largestSide > 0f ? Mathf.Min(1f, slotSize / largestSide) : 1f;

        block.SetDisplayScale(scale);
    }
}