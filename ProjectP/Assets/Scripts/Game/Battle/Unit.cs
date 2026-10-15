using System;
using UnityEngine;

// 체력을 가지고 피해와 회복을 받는 전투 개체(플레이어, 몬스터)의 공통 부분.
public abstract class Unit : MonoBehaviour
{
    // EnemyEditor가 SerializedProperty로 찾을 필드 이름.
    public const string StatsField = nameof(stats);

    [SerializeField] protected UnitStats stats = new UnitStats();

    public event Action<Unit> HpChanged;

    public UnitStats Stats => stats;
    public int MaxHp => stats.MaxHp;
    public int CurrentHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    protected virtual void Awake()
    {
        CurrentHp = stats.MaxHp;
    }

    // 방어력을 적용해 실제로 깎인 체력을 돌려준다. 이미 죽었다면 0이다.
    public int TakeDamage(int damage)
    {
        if (IsDead)
        {
            return 0;
        }

        int applied = Mathf.Min(stats.ApplyDefense(damage), CurrentHp);
        CurrentHp -= applied;
        HpChanged?.Invoke(this);

        if (IsDead)
        {
            OnDied();
        }

        return applied;
    }

    // 회복 증가를 적용해 실제로 회복된 체력을 돌려준다. 최대 체력을 넘지 않는다.
    public int Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return 0;
        }

        int healed = Mathf.Min(stats.ApplyHealIncrease(amount), stats.MaxHp - CurrentHp);
        if (healed <= 0)
        {
            return 0;
        }

        CurrentHp += healed;
        HpChanged?.Invoke(this);
        return healed;
    }

    // 스탯을 교체하고 체력을 새 최대 체력으로 채운다.
    protected void SetStats(UnitStats newStats)
    {
        stats = newStats;
        CurrentHp = stats.MaxHp;
        HpChanged?.Invoke(this);
    }

    protected abstract void OnDied();
}