using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Player : Unit
{
    public static Player Instance { get; private set; }

    // 게임오버 처리가 생기기 전까지는 이벤트만 발생시킵니다.
    public static event Action Died;

    // 인스펙터의 Stats는 블록을 적용하기 전 기본 스탯이며 수정하지 않습니다. (HP = 100 + X)
    // 실제 스탯은 기본 스탯에 SkillManager의 블록 스탯을 더해 새로 만듭니다.
    private UnitStats baseStats;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseStats = stats;
        base.Awake();
    }

    private void Start()
    {
        // 구독 전에 SkillManager가 이미 상태를 만든 경우를 반영합니다.
        if (SkillManager.Instance != null)
        {
            ApplySkills(SkillManager.Instance);
        }
    }

    private void OnEnable()
    {
        if (Instance == this)
        {
            SkillManager.Changed += ApplySkills;
        }
    }

    private void OnDisable()
    {
        SkillManager.Changed -= ApplySkills;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ApplySkills(SkillManager skillManager)
    {
        BlockStatTotals blockStats = skillManager.Stats;

        ReplaceStats(new UnitStats(
            baseStats.MaxHp + blockStats.Get(StatType.Hp),
            baseStats.Defense + blockStats.Get(StatType.Defense),
            baseStats.HealIncrease + blockStats.Get(StatType.HealIncrease)));
    }

    protected override void OnDied()
    {
        Debug.Log("Player died.");
        Died?.Invoke();
    }
}