using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Enemy : MonoBehaviour
{
    private static readonly List<Enemy> actives = new List<Enemy>();
    public static IReadOnlyList<Enemy> Actives => actives;

    [SerializeField] private float moveSpeed = 1.2f;

    private Rigidbody2D rb;

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
    public void Hit()
    {
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