using UnityEngine;

// 몬스터 유형(원형, 삼각형, 사각형, 오각형)별 스탯. 수식은 👿 몬스터 문서를 따른다.
// 스탯 = 고정 값 + 배율 * 레벨 (소수점 이하 버림)
// 레벨 = (스테이지 - 1) * 스테이지당 라운드 수 + 라운드
[CreateAssetMenu(fileName = "EnemyData", menuName = "ProjectP/Battle/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("체력")]
    [Tooltip("고정 HP")]
    [Min(0f)]
    [SerializeField] private float baseHp = 1f;

    [Tooltip("HP 배율. 레벨마다 이 값만큼 체력이 늘어납니다.")]
    [Min(0f)]
    [SerializeField] private float hpMultiplier = 1f;

    [Header("공격력")]
    [Tooltip("고정 공격력")]
    [Min(0f)]
    [SerializeField] private float baseAttack = 1f;

    [Tooltip("공격력 배율. 레벨마다 이 값만큼 공격력이 늘어납니다.")]
    [Min(0f)]
    [SerializeField] private float attackMultiplier = 1f;

    [Header("방어력")]
    [Tooltip("고정 방어력")]
    [Min(0f)]
    [SerializeField] private float baseDefense = 0f;

    [Tooltip("방어력 배율. 레벨마다 이 값만큼 방어력이 늘어납니다.")]
    [Min(0f)]
    [SerializeField] private float defenseMultiplier = 0f;

    [Header("속도")]
    [Tooltip("이동 속도(유닛/초)")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 1f;

    [Tooltip("근접 공격 쿨타임(초). 플레이어와 닿아 있는 동안 이 간격으로 대미지를 줍니다.")]
    [Min(0.05f)]
    [SerializeField] private float contactInterval = 1f;

    [Header("골드")]
    [Tooltip("고정 골드")]
    [Min(0f)]
    [SerializeField] private float baseGold = 0f;

    [Tooltip("골드 배율. 레벨마다 이 값만큼 골드가 늘어납니다.")]
    [Min(0f)]
    [SerializeField] private float goldMultiplier = 0f;

    public float MoveSpeed => moveSpeed;
    public float ContactInterval => contactInterval;

    public UnitStats CreateStats(int level)
    {
        return new UnitStats(Evaluate(baseHp, hpMultiplier, level), Evaluate(baseDefense, defenseMultiplier, level));
    }

    public int GetAttack(int level)
    {
        return Evaluate(baseAttack, attackMultiplier, level);
    }

    public int GetGold(int level)
    {
        return Evaluate(baseGold, goldMultiplier, level);
    }

    // 소수점 이하는 버린다. 2.1 * 10이 20.999...로 계산되어 20이 되지 않도록 작은 오차를 더한다.
    private static int Evaluate(float baseValue, float multiplier, int level)
    {
        return Mathf.FloorToInt(baseValue + multiplier * level + 0.0001f);
    }
}
