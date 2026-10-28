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

    // 방어력을 적용해 실제로 깎인 체력을 돌려준다. 이미 죽었거나 방어력에 모두 막히면 0이다.
    public int TakeDamage(int damage)
    {
        if (IsDead)
        {
            return 0;
        }

        int applied = Mathf.Min(ApplyDefense(damage), CurrentHp);
        if (applied <= 0)
        {
            return 0;
        }

        CurrentHp -= applied;
        HpChanged?.Invoke(this);

        if (IsDead)
        {
            OnDied();
        }

        return applied;
    }

    // 받는 대미지에 방어력을 적용한다. 방어력이 일시적으로 바뀌는 개체는 재정의한다.
    protected virtual int ApplyDefense(int damage)
    {
        return stats.ApplyDefense(damage);
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

    // 스탯을 교체하되 현재 체력 비율을 유지한다. 최대 체력이 바뀌는 블록을 넣고 빼는 것만으로 체력을 회복하지 못하게 한다.
    // 살아 있으면 체력이 0이 되지 않으며, 이미 죽었다면 체력은 0 그대로이다.
    protected void ReplaceStats(UnitStats newStats)
    {
        float ratio = stats.MaxHp > 0 ? (float)CurrentHp / stats.MaxHp : 1f;
        bool wasDead = IsDead;

        stats = newStats;
        if (!wasDead)
        {
            CurrentHp = Mathf.Clamp(Mathf.RoundToInt(stats.MaxHp * ratio), 1, stats.MaxHp);
        }

        HpChanged?.Invoke(this);
    }

    protected abstract void OnDied();
}