using UnityEditor;
using UnityEngine;

// BlockStatData를 행 = 스탯, 열 = 점수 1당 수치인 표로 보여주고 편집한다.
[CustomEditor(typeof(BlockStatData))]
public class BlockStatDataEditor : Editor
{
    private const float LabelWidth = 150f;
    private const float ValueWidth = 80f;

    private SerializedProperty rows;

    private void OnEnable()
    {
        rows = serializedObject.FindProperty(BlockStatData.RowsField);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("스탯 수치 = 블록 점수 × 점수 1당 수치", MessageType.None);

        DrawColumnHeader();
        for (int i = 0; i < rows.arraySize; i++)
        {
            DrawRow(rows.GetArrayElementAtIndex(i));
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawColumnHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(LabelRect(rect), "스탯", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(ValueRect(rect), "점수 1당 수치", EditorStyles.centeredGreyMiniLabel);
    }

    private static void DrawRow(SerializedProperty row)
    {
        SerializedProperty stat = row.FindPropertyRelative(BlockStatData.RowStatField);
        SerializedProperty valuePerScore = row.FindPropertyRelative(BlockStatData.RowValuePerScoreField);

        Rect rect = EditorGUILayout.GetControlRect();
        string statName = ObjectNames.NicifyVariableName(((StatType)stat.intValue).ToString());
        EditorGUI.LabelField(LabelRect(rect), statName);

        EditorGUI.BeginChangeCheck();
        int value = EditorGUI.IntField(ValueRect(rect), valuePerScore.intValue);
        if (EditorGUI.EndChangeCheck())
        {
            valuePerScore.intValue = Mathf.Max(0, value);
        }
    }

    private static Rect LabelRect(Rect rect)
    {
        return new Rect(rect.x, rect.y, LabelWidth, rect.height);
    }

    private static Rect ValueRect(Rect rect)
    {
        return new Rect(rect.x + LabelWidth, rect.y, ValueWidth, rect.height);
    }
}
