using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Enemy : Unit
{
    private static readonly List<Enemy> actives = new List<Enemy>();
    public static IReadOnlyList<Enemy> Actives => actives;

    // 체력이 0이 되어 죽었을 때 발생합니다.
    public static event Action<Enemy> Killed;

    [SerializeField] private float moveSpeed = 1.2f;

    // 몬스터 유형별 공격력/공격 속도가 생기기 전까지 쓰는 임시 접촉 피해입니다.
    [Tooltip("플레이어와 닿아 있을 때 주는 대미지 (임시)")]
    [Min(0)]
    [SerializeField] private int attack = 10;

    [Tooltip("플레이어와 닿아 있을 때 대미지를 주는 간격(초)")]
    [Min(0.05f)]
    [SerializeField] private float contactInterval = 1f;

    private Rigidbody2D rb;
    private Player contactTarget;
    private float contactTimer;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        actives.Add(this);
    }

    private void OnDisable()
    {
        actives.Remove(this);
    }

    private void FixedUpdate()
    {
        // 플레이어와 닿아 있는 동안에는 멈춰서 일정 간격으로 대미지를 줍니다.
        if (contactTarget != null)
        {
            contactTimer -= Time.fixedDeltaTime;
            if (contactTimer <= 0f)
            {
                contactTarget.TakeDamage(attack);
                contactTimer = contactInterval;
            }
            return;
        }

        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, player.transform.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    protected override void OnDied()
    {
        Killed?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player player))
        {
            return;
        }

        // 닿는 즉시 한 번 대미지를 주고, 이후 contactInterval마다 줍니다.
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