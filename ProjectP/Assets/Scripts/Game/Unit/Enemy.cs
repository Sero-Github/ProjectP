using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 유형별 외형에 맞는 콜라이더(Circle, Polygon, Box 등)를 쓸 수 있도록 Collider2D는 강제하지 않는다.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : Unit
{
    private static readonly List<Enemy> actives = new List<Enemy>();
    public static IReadOnlyList<Enemy> Actives => actives;

    // 폭발 대상을 고를 때 쓰는 actives 복사본. 폭발로 죽은 몬스터가 actives에서 빠져도 순회가 깨지지 않게 한다.
    private static readonly List<Enemy> explosionTargets = new List<Enemy>();

    // 체력이 0이 되어 죽었을 때 발생합니다.
    public static event Action<Enemy> Killed;
    // 처치가 아닌 이유(플레이어 사망 등)로 사라졌을 때 발생합니다. 몬스터별 기록을 지울 때 Killed와 함께 구독합니다.
    public static event Action<Enemy> Despawned;

    // 체력, 방어력, 공격력, 속도는 모두 이 데이터와 레벨로 계산합니다. 인스펙터의 Stats 값은 덮어씌워집니다.
    [SerializeField] private EnemyData data;
    // 속성 주입 효과의 수치. 없으면 속성 효과 없이 대미지만 받습니다.
    [SerializeField] private ElementSettings elementSettings;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    // 속성이 주입되지 않았을 때의 색. 풀에서 다시 꺼내질 때 이 색으로 돌아간다.
    private Color baseColor;
    private EnemyElementStatus elementStatus;
    private Player contactTarget;
    private float contactTimer;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    // 스포너가 만든 몬스터는 사망하면 파괴하지 않고 이 풀로 돌아간다. 씬에 직접 배치한 몬스터는 null이다.
    private IObjectPool<Enemy> pool;

    public EnemyData Data => data;
    public int Level { get; private set; }
    public int Attack { get; private set; }
    public int Gold { get; private set; }
    // 속성 주입 상태. ElementSettings가 연결되지 않았으면 null이다.
    public EnemyElementStatus ElementStatus => elementStatus;
    // 노랑(감전) 속성으로 공격 속도가 느려진 배율. 공격 간격을 이 값으로 나눈다.
    public float AttackSpeedMultiplier => elementStatus != null ? elementStatus.AttackSpeedMultiplier : 1f;
    // 몸체 콜라이더를 감싸는 원의 반지름. 공격 지대에 닿았는지, 지원 크리쳐가 얼마나 지나갈지 계산할 때 쓴다.
    public float BodyRadius
    {
        get
        {
            Vector3 extents = bodyCollider.bounds.extents;
            return Mathf.Max(extents.x, extents.y);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        bodyCollider = GetComponent<Collider2D>();
        bodyCollider.isTrigger = true;

        if (elementSettings != null)
        {
            elementStatus = new EnemyElementStatus(elementSettings);
        }
        else
        {
            Debug.LogWarning($"{name}: ElementSettings가 연결되지 않아 속성 효과가 적용되지 않습니다.", this);
        }

        // 씬에 직접 배치된 경우를 위해 1레벨로 초기화해 두고, 스포너가 생성 직후 다시 초기화합니다.
        Initialize(1);
    }

    private void OnEnable()
    {
        actives.Add(this);
    }

    // 풀로 돌아갈 때 지난 생애의 접촉 상태가 다음 생성으로 이어지지 않게 비운다.
    private void OnDisable()
    {
        actives.Remove(this);
        contactTarget = null;
        contactTimer = 0f;
        knockbackTimer = 0f;
    }

    public void SetPool(IObjectPool<Enemy> enemyPool)
    {
        pool = enemyPool;
    }

    // 생성되거나 풀에서 다시 꺼내질 때마다 호출되어 체력을 가득 채운다.
    public void Initialize(int level)
    {
        if (data == null)
        {
            Debug.LogError($"{name}: EnemyData가 연결되지 않았습니다.", this);
            return;
        }

        Level = Mathf.Max(1, level);
        SetStats(data.CreateStats(Level));
        Attack = data.GetAttack(Level);
        Gold = data.GetGold(Level);

        elementStatus?.Reset();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    // 공격 스킬의 타격. 속성을 주입(색 변경)한 뒤 대미지를 주므로, 이 타격으로 죽으면 주입된 속성 색으로 사망 파티클이 나온다.
    public void TakeHit(ElementHit hit)
    {
        if (IsDead)
        {
            return;
        }

        if (elementStatus == null)
        {
            TakeDamage(hit.Damage);
            return;
        }

        EnemyElementStatus.HitResult result = elementStatus.Apply(hit);
        if (result.Injected && spriteRenderer != null)
        {
            spriteRenderer.color = GetElementColor(elementStatus.Element);
        }

        // 이 타격으로 죽어 풀로 돌아가도 폭발은 맞은 자리에서 일어난다.
        Vector2 position = transform.position;
        TakeDamage(hit.Damage);

        if (result.ExplosionDamage > 0)
        {
            Explode(position, result.ExplosionDamage);
        }

        if (result.KnockbackMultiplier > 0f && !IsDead)
        {
            StartKnockback(result.KnockbackMultiplier);
        }
    }

    // 파랑(방어력 감소) 속성은 스탯 방어력을 먼저 계산한 뒤 비율로 깎는다.
    protected override int ApplyDefense(int damage)
    {
        float reduction = elementStatus != null ? elementStatus.DefenseReduction : 0f;
        int defense = Mathf.FloorToInt(stats.Defense * (1f - reduction) + 0.0001f);
        return UnitStats.ApplyDefense(damage, defense);
    }

    private void Update()
    {
        if (elementStatus == null || IsDead)
        {
            return;
        }

        // 감전, 독 대미지
        int damage = elementStatus.Tick(Time.deltaTime);
        if (damage > 0)
        {
            TakeDamage(damage);
        }
    }

    private void FixedUpdate()
    {
        if (data == null)
        {
            return;
        }

        // 넉백되는 동안에는 추적과 근접 공격을 멈춘다. 화면 밖으로 밀려나도 사라지지 않고 다시 걸어온다.
        if (knockbackTimer > 0f)
        {
            float step = Mathf.Min(knockbackTimer, Time.fixedDeltaTime);
            knockbackTimer -= Time.fixedDeltaTime;
            rb.MovePosition(rb.position + knockbackVelocity * step);
            return;
        }

        // 플레이어와 닿아 있는 동안에는 멈춰서 일정 간격으로 대미지를 줍니다.
        if (contactTarget != null)
        {
            contactTimer -= Time.fixedDeltaTime;
            if (contactTimer <= 0f)
            {
                Player target = contactTarget;
                contactTimer = data.ContactInterval / AttackSpeedMultiplier;

                // 대미지를 주면 살짝 밀려난다. 방어력에 모두 막혀 대미지가 0이면 밀려나지 않는다.
                // 이 대미지로 플레이어가 죽으면 모든 몬스터가 풀로 돌아가므로(비활성) 넉백하지 않는다.
                if (target.TakeDamage(Attack) > 0 && gameObject.activeSelf)
                {
                    Knockback(target.transform.position, target.ContactKnockbackDistance, target.ContactKnockbackDuration);
                }
            }
            return;
        }

        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        float moveSpeed = data.MoveSpeed * (elementStatus != null ? elementStatus.MoveSpeedMultiplier : 1f);
        Vector2 next = Vector2.MoveTowards(rb.position, player.transform.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    // 빨강 폭발. 반경 안의 다른 몬스터에게 속성 주입 없이 대미지를 준다.
    private void Explode(Vector2 center, int damage)
    {
        float radius = elementSettings.ExplosionRadius;
        ElementExplosionEffect.Play(center, radius, GetElementColor(Element.Red));

        explosionTargets.Clear();
        explosionTargets.AddRange(actives);

        float sqrRadius = radius * radius;
        foreach (Enemy enemy in explosionTargets)
        {
            if (enemy != this && !enemy.IsDead && ((Vector2)enemy.transform.position - center).sqrMagnitude <= sqrRadius)
            {
                enemy.TakeDamage(damage);
            }
        }

        explosionTargets.Clear();
    }

    // 회색 넉백. 플레이어 반대 방향으로 (이동 속도 * 배율 * multiplier)만큼 밀려난다.
    private void StartKnockback(float multiplier)
    {
        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        float distance = data.MoveSpeed * elementSettings.KnockbackDistanceMultiplier * multiplier;
        Knockback(player.transform.position, distance, elementSettings.KnockbackDuration);
    }

    // origin 반대 방향으로 duration초 동안 distance만큼 밀려난다. 밀려나는 동안은 추적과 근접 공격을 멈춘다.
    public void Knockback(Vector2 origin, float distance, float duration)
    {
        if (distance <= 0f || duration <= 0f)
        {
            return;
        }

        Vector2 direction = rb.position - origin;
        direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.up;

        knockbackTimer = duration;
        knockbackVelocity = direction * (distance / duration);
    }

    // 속성 색은 탄환, 블록과 같은 core 색을 쓴다. 속성이 없으면 원래 색이다.
    private Color GetElementColor(Element element)
    {
        if (element == Element.None || BlockManager.Instance == null)
        {
            return baseColor;
        }

        return BlockManager.Instance.GetColorForCore((int)element);
    }

    protected override void OnDied()
    {
        Killed?.Invoke(this);
        // 몬스터 모양과 현재 색(주입된 속성의 색)으로 조각나며 사라진다.
        EnemyDeathParticles.Play(spriteRenderer);
        ReturnToPool();
    }

    // 처치가 아닌 이유(플레이어 사망 등)로 몬스터를 없앤다. Killed를 발생시키지 않으므로 블록, 골드를 드랍하지 않는다.
    public void Despawn(bool playParticles)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (playParticles)
        {
            EnemyDeathParticles.Play(spriteRenderer);
        }

        Despawned?.Invoke(this);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (pool != null)
        {
            gameObject.SetActive(false);
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player player))
        {
            return;
        }

        // 닿는 즉시 한 번 대미지를 주고, 이후 ContactInterval마다 줍니다.
        contactTarget = player;
        contactTimer = 0f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (contactTarget != null && other.gameObject == contactTarget.gameObject)
        {
            contactTarget = null;
        }
    }
}