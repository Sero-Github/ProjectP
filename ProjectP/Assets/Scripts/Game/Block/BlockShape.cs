using System.Collections.Generic;
using UnityEngine;

// 블록 프리팹의 모양 정보. 🧱 블록 문서 3장의 블록 종류 하나가 프리팹 하나이다.
// 블록 종류(스탯, 스킬, 액티브)는 드롭할 때 정해지므로 프리팹에는 블록 클래스를 붙이지 않고 모양만 둔다.
// BlockManager는 이 프리팹을 복제하지 않고 조각 위치에서 칸 목록만 읽어, 정해진 블록 클래스로 조각을 조립한다.
public class BlockShape : MonoBehaviour
{
    [Tooltip("블록을 이루는 조각들. 조각 하나가 한 칸이며, 위치는 Block.PieceUnit 단위입니다.")]
    [SerializeField] private List<GameObject> pieces = new List<GameObject>();
    [Tooltip("코어 조각. pieces 중 하나여야 합니다.")]
    [SerializeField] private GameObject coreBlock;

    public int PieceCount => pieces != null ? pieces.Count : 0;

    // 좌상단이 (0,0)인 칸 목록과 그 중 코어 칸. y는 아래쪽이 +이다. 모양 정보가 없으면 빈 목록이다.
    public List<Vector2Int> GetCells(out Vector2Int coreCell)
    {
        coreCell = Vector2Int.zero;
        var cells = new List<Vector2Int>(PieceCount);
        if (PieceCount == 0)
        {
            return cells;
        }

        GameObject core = coreBlock != null ? coreBlock : pieces[0];
        Vector2 corePosition = ((RectTransform)core.transform).anchoredPosition;

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var piece in pieces)
        {
            if (piece == null)
            {
                continue;
            }

            Vector2 delta = ((RectTransform)piece.transform).anchoredPosition - corePosition;
            var cell = new Vector2Int(Mathf.RoundToInt(delta.x / Block.PieceUnit), Mathf.RoundToInt(-delta.y / Block.PieceUnit));
            cells.Add(cell);
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
        }

        var min = new Vector2Int(minX, minY);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i] -= min;
        }

        coreCell = -min;
        return cells;
    }
}
