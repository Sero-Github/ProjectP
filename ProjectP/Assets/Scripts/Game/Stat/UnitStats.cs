using System;
using UnityEngine;

// 플레이어와 몬스터가 공통으로 가지는 기본 스탯. 수식은 📊 스탯 문서를 따른다.
[Serializable]
public class UnitStats
{
    [Tooltip("최대 체력. 플레이어는 100 + X로 계산됩니다.")]
    [Min(1)]
    [SerializeField] private int maxHp = 100;

    [Tooltip("방어력. 받는 대미지를 고정 값으로 줄입니다. (적용 대미지 = 대미지 - 방어력, 최소 1)")]
    [Min(0)]
    [SerializeField] private int defense = 0;

    [Tooltip("회복 증가(%). 체력 회복량 = 회복량 * (1 + X * 0.01)")]
    [Min(0)]
    [SerializeField] private int healIncrease = 0;

    public UnitStats()
    {
    }

    public UnitStats(int maxHp, int defense, int healIncrease = 0)
    {
        this.maxHp = Mathf.Max(1, maxHp);
        this.defense = Mathf.Max(0, defense);
        this.healIncrease = Mathf.Max(0, healIncrease);
    }

    public int MaxHp => maxHp;
    public int Defense => defense;
    public int HealIncrease => healIncrease;

    public int ApplyDefense(int damage)
    {
        return Mathf.Max(1, damage - defense);
    }

    // 소수점 이하는 버린다.
    public int ApplyHealIncrease(int amount)
    {
        return Mathf.FloorToInt(amount * (1f + healIncrease * 0.01f));
    }
}