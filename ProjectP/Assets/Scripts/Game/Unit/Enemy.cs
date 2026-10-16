using System;
using System.Collections.Generic;
using UnityEngine;

// 유형별 외형에 맞는 콜라이더(Circle, Polygon, Box 등)를 쓸 수 있도록 Collider2D는 강제하지 않는다.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : Unit
{
    private static readonly List<Enemy> actives = new List<Enemy>();
    public static IReadOnlyList<Enemy> Actives => actives;

    // 체력이 0이 되어 죽었을 때 발생합니다.
    public static event Action<Enemy> Killed;

    // 체력, 방어력, 공격력, 속도는 모두 이 데이터와 레벨로 계산합니다. 인스펙터의 Stats 값은 덮어씌워집니다.
    [SerializeField] private EnemyData data;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Player contactTarget;
    private float contactTimer;

    public EnemyData Data => data;
    public int Level { get; private set; }
    public int Attack { get; private set; }
    public int Gold { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;

        // 씬에 직접 배치된 경우를 위해 1레벨로 초기화해 두고, 스포너가 생성 직후 다시 초기화합니다.
        Initialize(1);
    }

    private void OnEnable()
    {
        actives.Add(this);
    }

    private void OnDisable()
    {
        actives.Remove(this);
    }

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
    }

    private void FixedUpdate()
    {
        if (data == null)
        {
            return;
        }

        // 플레이어와 닿아 있는 동안에는 멈춰서 일정 간격으로 대미지를 줍니다.
        if (contactTarget != null)
        {
            contactTimer -= Time.fixedDeltaTime;
            if (contactTimer <= 0f)
            {
                contactTarget.TakeDamage(Attack);
                contactTimer = data.ContactInterval;
            }
            return;
        }

        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, player.transform.position, data.MoveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    protected override void OnDied()
    {
        Killed?.Invoke(this);
        // 몬스터 모양과 현재 색(주입된 속성의 색)으로 조각나며 사라진다.
        EnemyDeathParticles.Play(spriteRenderer);
        Destroy(gameObject);
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
