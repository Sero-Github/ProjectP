using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Player : Unit
{
    public static Player Instance { get; private set; }

    // 체력이 0이 되면 발생합니다. RoundManager가 받아 이전 라운드로 되돌립니다.
    public static event Action Died;

    [Header("피격")]
    [Tooltip("피격 후 깜빡이는 시간(초). 깜빡이는 동안에도 대미지는 받습니다.")]
    [Min(0f)]
    [SerializeField] private float blinkDuration = 2f;

    [Tooltip("깜빡일 때 가장 투명해지는 알파 값")]
    [Range(0f, 1f)]
    [SerializeField] private float blinkMinAlpha = 0.2f;

    [Tooltip("알파가 1 -> 최소 -> 1로 한 번 왕복하는 시간(초)")]
    [Min(0.01f)]
    [SerializeField] private float blinkPeriod = 0.2f;

    [Tooltip("플레이어와 부딪혀 대미지를 준 몬스터가 밀려나는 거리")]
    [Min(0f)]
    [SerializeField] private float contactKnockbackDistance = 0.8f;

    [Tooltip("몬스터가 밀려나는 데 걸리는 시간(초)")]
    [Min(0.01f)]
    [SerializeField] private float contactKnockbackDuration = 0.15f;

    // 인스펙터의 Stats는 블록을 적용하기 전 기본 스탯이며 수정하지 않습니다. (HP = 100 + X)
    // 실제 스탯은 기본 스탯에 SkillManager의 블록 스탯을 더해 새로 만듭니다.
    private UnitStats baseStats;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private float blinkTimer;

    public float ContactKnockbackDistance => contactKnockbackDistance;
    public float ContactKnockbackDuration => contactKnockbackDuration;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        baseStats = stats;
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
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

    private void Update()
    {
        if (blinkTimer <= 0f)
        {
            return;
        }

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            EndBlink();
            return;
        }

        // 깜빡이는 동안 알파가 1과 blinkMinAlpha 사이를 빠르게 왕복한다.
        float halfPeriod = blinkPeriod * 0.5f;
        float t = Mathf.PingPong(Time.time, halfPeriod) / halfPeriod;
        SetAlpha(Mathf.Lerp(1f, blinkMinAlpha, t));
    }

    private void ApplySkills(SkillManager skillManager)
    {
        BlockStatTotals blockStats = skillManager.Stats;

        ReplaceStats(new UnitStats(
            baseStats.MaxHp + blockStats.Get(StatType.Hp),
            baseStats.Defense + blockStats.Get(StatType.Defense),
            baseStats.HealIncrease + blockStats.Get(StatType.HealIncrease)));
    }

    // 라운드를 시작할 때마다 체력을 가득 채운다. 사망해 사라진 상태라면 다시 나타난다.
    public void Revive()
    {
        EndBlink();
        SetVisible(true);
        RestoreFullHp();
    }

    protected override void OnDamaged(int applied)
    {
        Debug.Log($"Player hit: -{applied} HP {CurrentHp} / {MaxHp}");

        // 피격할 때마다 깜빡임을 처음부터 다시 시작한다. 이 피격으로 죽었다면 깜빡이지 않고 바로 사망 처리한다.
        if (!IsDead && blinkDuration > 0f)
        {
            blinkTimer = blinkDuration;
        }
    }

    protected override void OnDied()
    {
        Debug.Log("Player died.");
        EndBlink();

        // 몬스터와 같은 사망 파티클을 내고 사라진다. 다음 라운드 시작(Revive) 때 다시 나타난다.
        EnemyDeathParticles.Play(spriteRenderer);
        SetVisible(false);

        Died?.Invoke();
    }

    private void EndBlink()
    {
        blinkTimer = 0f;
        SetAlpha(1f);
    }

    // 사라진 동안에는 몬스터 탄환, 몬스터와 부딪히지 않도록 콜라이더도 끈다.
    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = visible;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}
