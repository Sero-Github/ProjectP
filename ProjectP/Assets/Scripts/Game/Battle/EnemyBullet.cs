using UnityEngine;

// 원거리 몬스터가 플레이어를 향해 쏘는 탄환. 플레이어에게 닿으면 대미지를 주고 사라진다.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Tooltip("발사 후 이 시간(초)이 지나면 맞지 않아도 사라집니다.")]
    [Min(0.1f)]
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;
    private int damage;
    private bool spent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 플레이어에 Rigidbody2D가 없어도 트리거 판정을 받도록 탄환은 Dynamic으로 둡니다.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Launch(Vector2 direction, float speed, int attack)
    {
        damage = attack;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.rotation = angle;
        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || !other.TryGetComponent(out Player player) || player.IsDead)
        {
            return;
        }

        player.TakeDamage(damage);
        spent = true;
        Destroy(gameObject);
    }
}
