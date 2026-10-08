using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Enemy : MonoBehaviour
{
    private static readonly List<Enemy> actives = new List<Enemy>();
    public static IReadOnlyList<Enemy> Actives => actives;

    // 탄환으로 처치됐을 때만 발생합니다. 플레이어와 부딪혀 사라질 때는 발생하지 않습니다.
    public static event Action<Enemy> Killed;

    [SerializeField] private float moveSpeed = 1.2f;

    private Rigidbody2D rb;
    private bool dead;

    private void Awake()
    {
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
        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, player.transform.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    // 대미지/체력 시스템이 생기기 전까지는 피격 시 바로 제거합니다.
    // 관통 탄은 같은 프레임에 여러 번 맞힐 수 있으므로 처치 처리는 한 번만 합니다.
    public void Hit()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        Killed?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player _))
        {
            return;
        }

        Destroy(gameObject);
    }
}