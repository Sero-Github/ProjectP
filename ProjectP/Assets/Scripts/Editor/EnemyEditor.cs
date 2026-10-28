using UnityEditor;
using UnityEngine;

// Enemy의 스탯은 생성 시 EnemyData와 레벨로 덮어씌워지므로, 편집용 Stats 대신
// 실행 중인 몬스터의 현재 스탯(현재 체력 포함)을 접지 않고 읽기 전용으로 보여준다.
[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, Unit.StatsField);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 스탯", EditorStyles.boldLabel);

        Enemy enemy = (Enemy)target;
        // 프리팹 에셋은 Awake가 돌지 않아 값이 비어 있으므로 씬의 인스턴스일 때만 보여준다.
        if (!Application.isPlaying || EditorUtility.IsPersistent(enemy))
        {
            EditorGUILayout.HelpBox("플레이 중 생성된 몬스터를 선택하면 현재 스탯이 표시됩니다. 스탯은 EnemyData와 레벨로 계산됩니다.", MessageType.Info);
            return;
        }

        DrawHpBar(enemy);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("레벨", enemy.Level);
            EditorGUILayout.IntField("방어력", enemy.Stats.Defense);
            EditorGUILayout.IntField("회복 증가(%)", enemy.Stats.HealIncrease);
            EditorGUILayout.IntField("공격력", enemy.Attack);
            EditorGUILayout.IntField("골드", enemy.Gold);
            if (enemy.Data != null)
            {
                EditorGUILayout.FloatField("이동 속도", enemy.Data.MoveSpeed);
            }
        }

        DrawElementStatus(enemy.ElementStatus);
    }

    // 주입된 속성과 스택, 속성 효과로 바뀐 배율을 보여준다.
    private static void DrawElementStatus(EnemyElementStatus status)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("속성", EditorStyles.boldLabel);

        if (status == null)
        {
            EditorGUILayout.HelpBox("ElementSettings가 연결되지 않아 속성 효과가 적용되지 않습니다.", MessageType.Warning);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.EnumPopup("주입된 속성", status.Element);
            EditorGUILayout.TextField("스택별 속성 공격력", status.Stacks.Count > 0 ? string.Join(", ", status.Stacks) : "-");
            EditorGUILayout.FloatField("이동 속도 배율", status.MoveSpeedMultiplier);
            EditorGUILayout.FloatField("공격 속도 배율", status.AttackSpeedMultiplier);
            EditorGUILayout.FloatField("방어력 감소", status.DefenseReduction);
            EditorGUILayout.FloatField("파랑 잔여 시간(초)", Mathf.Max(0f, status.LingeringTime));
        }
    }

    // 피격 시 체력이 바로 갱신되도록 플레이 중에는 계속 다시 그린다.
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    private static void DrawHpBar(Enemy enemy)
    {
        Rect rect = EditorGUILayout.GetControlRect();
        rect = EditorGUI.PrefixLabel(rect, new GUIContent("체력"));
        float ratio = enemy.MaxHp > 0 ? (float)enemy.CurrentHp / enemy.MaxHp : 0f;
        EditorGUI.ProgressBar(rect, ratio, $"{enemy.CurrentHp} / {enemy.MaxHp}");
    }
}