using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    private RectTransform rectTransform;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        Instance = this;
        rectTransform = (RectTransform)transform;
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera camera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
    }

    public void AddBlock(Block block)
    {
        block.transform.SetParent(rectTransform, false);
    }
}