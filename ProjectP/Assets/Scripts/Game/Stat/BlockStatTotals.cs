using System.Collections.Generic;
using UnityEngine;

// 블록들의 스탯을 종류별로 합산한 값. SkillManager가 가방에 배치된 블록으로 만들어 가지며,
// 플레이어와 공격 스킬이 참조한다.
// 수식은 📊 스탯 문서를 따른다.
public class BlockStatTotals
{
    // 쿨타임 감소는 가장 높은 값 20개만 적용한다.
    public const int MaxCooldownReductionCount = 20;

    public static readonly BlockStatTotals Empty = new BlockStatTotals(null);

    private readonly Dictionary<StatType, int> totals = new Dictionary<StatType, int>();
    // 쿨타임 감소는 곱 적용이므로 합산 값과 별개로 블록별 값을 높은 순으로 둔다. 병합 블록은 이미 합산된 하나의 값이다.
    private readonly List<int> cooldownReductions = new List<int>();

    public BlockStatTotals(IEnumerable<Block> blocks)
    {
        if (blocks == null)
        {
            return;
        }

        foreach (var block in blocks)
        {
            if (block == null || !block.HasStat || block.Stat.Type == StatType.None)
            {
                continue;
            }

            BlockStat stat = block.Stat;
            totals[stat.Type] = Get(stat.Type) + stat.Value;

            if (stat.Type == StatType.CooldownReduction)
            {
                cooldownReductions.Add(stat.Value);
            }
        }

        cooldownReductions.Sort((a, b) => b.CompareTo(a));
    }

    public int Get(StatType type)
    {
        return totals.TryGetValue(type, out int value) ? value : 0;
    }

    // 속성 공격력 스탯. 속성이 없으면 0이다.
    public int GetElementAttack(Element element)
    {
        if (element < Element.Red || element > Element.Gray)
        {
            return 0;
        }

        return Get(StatType.ElementAttack_1 + (element - Element.Red));
    }

    // 실제 적용 대미지 = (공격력 + 속성 공격력) * (1 + 대미지 증가%). 공격력은 스킬 기본 공격력에 공격력 스탯을 더한 값이다.
    // elementAttack은 속성 공격력 스탯과 스킬 점수로 얻은 속성 공격력의 합이며, 무속성 공격은 0이다. 소수점 이하는 버린다.
    public int ApplyDamage(int baseAttack, int elementAttack = 0)
    {
        int attack = baseAttack + Get(StatType.Attack) + elementAttack;
        // 10 * 1.1이 10.999...로 계산되어 10이 되지 않도록 작은 오차를 더한다.
        return Mathf.FloorToInt(attack * (1f + Get(StatType.DamageIncrease) * 0.01f) + 0.0001f);
    }

    // 실제 스킬 쿨타임 = 스킬 쿨타임 * ([1 - 쿨타임 감소1] * [1 - 쿨타임 감소2] ...)
    public float ApplyCooldown(float cooldown)
    {
        float multiplier = 1f;
        int count = Mathf.Min(cooldownReductions.Count, MaxCooldownReductionCount);
        for (int i = 0; i < count; i++)
        {
            multiplier *= 1f - Mathf.Clamp01(cooldownReductions[i] * 0.01f);
        }

        return cooldown * multiplier;
    }
}