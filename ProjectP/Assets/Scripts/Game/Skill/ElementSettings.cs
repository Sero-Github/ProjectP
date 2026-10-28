using System.Collections.Generic;
using UnityEngine;

// 속성 주입 효과의 수치. ⚡ 속성 문서 3장을 따른다.
// 효과의 세기는 모두 속성 공격력(속성 공격력 스탯 + 스킬 점수로 얻은 속성 공격력)으로 계산한다.
// "기본 N% + 속성 공격력 100당 M%"는 100 단위로 버림하며, 스택이 여럿이면 스택마다 계산해 더한 뒤 최대치로 제한한다.
[CreateAssetMenu(fileName = "ElementSettings", menuName = "ProjectP/Skill/Element Settings")]
public class ElementSettings : ScriptableObject
{
    // 속성 공격력 이 값마다 증가량을 한 번 더한다.
    private const int AttackUnit = 100;

    [Tooltip("감전, 독 대미지를 주는 간격(초)")]
    [Min(0.05f)]
    [SerializeField] private float tickInterval = 1f;

    [Header("빨강(폭발)")]
    [Tooltip("폭발 반경(유닛). 폭발 대미지는 속성 공격력과 같습니다.")]
    [Min(0f)]
    [SerializeField] private float explosionRadius = 1.5f;

    [Header("주황(강화)")]
    [Tooltip("주황 타격으로 노랑, 초록, 파랑 효과를 추가로 중첩할 수 있는 횟수")]
    [Min(0)]
    [SerializeField] private int orangeExtraStacks = 1;

    [Tooltip("회색 속성을 가진 적을 주황으로 타격할 때 넉백 거리 배율")]
    [Min(0f)]
    [SerializeField] private float orangeKnockbackMultiplier = 1.5f;

    [Header("노랑(감전)")]
    [Tooltip("감전 대미지. 틱마다 속성 공격력의 N%")]
    [Min(0f)]
    [SerializeField] private float shockDamagePercent = 5f;

    [Tooltip("공격 속도 둔화 기본값(%)")]
    [Min(0f)]
    [SerializeField] private float attackSlowBase = 5f;

    [Tooltip("속성 공격력 100당 추가되는 공격 속도 둔화(%)")]
    [Min(0f)]
    [SerializeField] private float attackSlowPer100 = 5f;

    [Tooltip("공격 속도 둔화 최대치(%)")]
    [Range(0f, 100f)]
    [SerializeField] private float attackSlowMax = 40f;

    [Header("초록(둔화)")]
    [Tooltip("이동 속도 둔화 기본값(%)")]
    [Min(0f)]
    [SerializeField] private float moveSlowBase = 10f;

    [Tooltip("속성 공격력 100당 추가되는 이동 속도 둔화(%)")]
    [Min(0f)]
    [SerializeField] private float moveSlowPer100 = 5f;

    [Tooltip("이동 속도 둔화 최대치(%)")]
    [Range(0f, 100f)]
    [SerializeField] private float moveSlowMax = 40f;

    [Header("파랑(방어력 감소)")]
    [Tooltip("방어력 감소 기본값(%)")]
    [Min(0f)]
    [SerializeField] private float defenseReductionBase = 10f;

    [Tooltip("속성 공격력 100당 추가되는 방어력 감소(%)")]
    [Min(0f)]
    [SerializeField] private float defenseReductionPer100 = 10f;

    [Tooltip("방어력 감소 최대치(%)")]
    [Range(0f, 100f)]
    [SerializeField] private float defenseReductionMax = 100f;

    [Tooltip("파랑 속성을 잃은 뒤에도 방어력 감소가 유지되는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float defenseReductionLinger = 5f;

    [Header("보라(독)")]
    [Tooltip("독 대미지. 틱마다 스택별 속성 공격력의 N%")]
    [Min(0f)]
    [SerializeField] private float poisonDamagePercent = 10f;

    [Tooltip("독 최대 중첩 수")]
    [Min(1)]
    [SerializeField] private int poisonMaxStacks = 5;

    [Header("회색(넉백)")]
    [Tooltip("넉백 거리. 몬스터 이동 속도의 N배")]
    [Min(0f)]
    [SerializeField] private float knockbackDistanceMultiplier = 3f;

    [Tooltip("넉백되는 데 걸리는 시간(초). 이 동안에는 이동과 근접 공격을 멈춥니다.")]
    [Min(0.01f)]
    [SerializeField] private float knockbackDuration = 0.2f;

    public float TickInterval => tickInterval;
    public float ExplosionRadius => explosionRadius;
    public int OrangeExtraStacks => orangeExtraStacks;
    public float OrangeKnockbackMultiplier => orangeKnockbackMultiplier;
    public float DefenseReductionLinger => defenseReductionLinger;
    public int PoisonMaxStacks => poisonMaxStacks;
    public float KnockbackDistanceMultiplier => knockbackDistanceMultiplier;
    public float KnockbackDuration => knockbackDuration;

    // 스택별 감전 대미지의 합. 소수점 이하는 버린다.
    public int GetShockDamage(IReadOnlyList<int> stacks)
    {
        return SumPercentDamage(stacks, shockDamagePercent);
    }

    // 스택별 독 대미지의 합. 소수점 이하는 버린다.
    public int GetPoisonDamage(IReadOnlyList<int> stacks)
    {
        return SumPercentDamage(stacks, poisonDamagePercent);
    }

    // 공격 속도 둔화 비율(0~1). 속성 공격력 150이면 5% + 5% = 10%이다.
    public float GetAttackSlow(IReadOnlyList<int> stacks)
    {
        return SumScaledPercent(stacks, attackSlowBase, attackSlowPer100, attackSlowMax);
    }

    // 이동 속도 둔화 비율(0~1). 속성 공격력 150이면 10% + 5% = 15%이다.
    public float GetMoveSlow(IReadOnlyList<int> stacks)
    {
        return SumScaledPercent(stacks, moveSlowBase, moveSlowPer100, moveSlowMax);
    }

    // 방어력 감소 비율(0~1).
    public float GetDefenseReduction(IReadOnlyList<int> stacks)
    {
        return SumScaledPercent(stacks, defenseReductionBase, defenseReductionPer100, defenseReductionMax);
    }

    private static int SumPercentDamage(IReadOnlyList<int> stacks, float percent)
    {
        int total = 0;
        foreach (int elementAttack in stacks)
        {
            // 10 * 0.1이 0.999...로 계산되어 0이 되지 않도록 작은 오차를 더한다.
            total += Mathf.FloorToInt(Mathf.Max(0, elementAttack) * percent * 0.01f + 0.0001f);
        }

        return total;
    }

    private static float SumScaledPercent(IReadOnlyList<int> stacks, float basePercent, float per100Percent, float maxPercent)
    {
        float total = 0f;
        foreach (int elementAttack in stacks)
        {
            total += basePercent + per100Percent * (Mathf.Max(0, elementAttack) / AttackUnit);
        }

        return Mathf.Min(total, maxPercent) * 0.01f;
    }
}