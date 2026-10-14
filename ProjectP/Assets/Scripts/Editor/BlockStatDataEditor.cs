using UnityEditor;
using UnityEngine;

// BlockStatData를 행 = 스탯, 열 = 블록 번호인 표로 보여주고 편집한다.
[CustomEditor(typeof(BlockStatData))]
public class BlockStatDataEditor : Editor
{
    private const float LabelWidth = 150f;
    private const float CellSpacing = 2f;

    private SerializedProperty blockCount;
    private SerializedProperty rows;

    private void OnEnable()
    {
        blockCount = serializedObject.FindProperty(BlockStatData.BlockCountField);
        rows = serializedObject.FindProperty(BlockStatData.RowsField);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(blockCount);
        EditorGUILayout.Space();

        int columns = blockCount.intValue;
        DrawColumnHeader(columns);
        for (int i = 0; i < rows.arraySize; i++)
        {
            DrawRow(rows.GetArrayElementAtIndex(i), columns);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawColumnHeader(int columns)
    {
        Rect rect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(CellRect(rect, -1, columns), "스탯 \\ 블록", EditorStyles.miniBoldLabel);
        for (int column = 0; column < columns; column++)
        {
            EditorGUI.LabelField(CellRect(rect, column, columns), (column + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void DrawRow(SerializedProperty row, int columns)
    {
        SerializedProperty stat = row.FindPropertyRelative(BlockStatData.RowStatField);
        SerializedProperty scores = row.FindPropertyRelative(BlockStatData.RowScoresField);

        Rect rect = EditorGUILayout.GetControlRect();
        string statName = ObjectNames.NicifyVariableName(((StatType)stat.intValue).ToString());
        EditorGUI.LabelField(CellRect(rect, -1, columns), statName);

        // OnValidate가 길이를 맞추기 전일 수 있으므로 실제 배열 길이까지만 그린다.
        int count = Mathf.Min(columns, scores.arraySize);
        for (int column = 0; column < count; column++)
        {
            SerializedProperty score = scores.GetArrayElementAtIndex(column);
            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntField(CellRect(rect, column, columns), score.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                score.intValue = Mathf.Max(0, value);
            }
        }
    }

    // column이 -1이면 스탯 라벨 칸, 0 이상이면 블록 번호 칸을 돌려준다.
    private static Rect CellRect(Rect rect, int column, int columns)
    {
        if (column < 0)
        {
            return new Rect(rect.x, rect.y, LabelWidth, rect.height);
        }

        float cellWidth = (rect.width - LabelWidth) / Mathf.Max(1, columns);
        return new Rect(rect.x + LabelWidth + cellWidth * column + CellSpacing * 0.5f, rect.y, cellWidth - CellSpacing, rect.height);
    }
}