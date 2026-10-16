using UnityEngine;

// 원거리 몬스터가 플레이어를 향해 쏘는 탄환. 플레이어에게 닿으면 대미지를 주고 사라진다.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Tooltip("화면 경계에서 이 거리(유닛)만큼 더 벗어나면 사라집니다.")]
    [Min(0f)]
    [SerializeField] private float offscreenMargin = 0.5f;

    private Rigidbody2D rb;
    private Camera targetCamera;
    private int damage;
    private bool spent;
    // 몬스터는 화면 밖에서도 쏠 수 있으므로, 한 번 화면 안에 들어온 뒤에 벗어날 때만 사라집니다.
    private bool enteredScreen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 플레이어에 Rigidbody2D가 없어도 트리거 판정을 받도록 탄환은 Dynamic으로 둡니다.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GetComponent<CircleCollider2D>().isTrigger = true;

        targetCamera = Camera.main;
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        bool offscreen = IsOffscreen();
        if (!offscreen)
        {
            enteredScreen = true;
        }
        else if (enteredScreen)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 direction, float speed, int attack)
    {
        damage = attack;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.rotation = angle;
        rb.linearVelocity = direction * speed;
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

    private bool IsOffscreen()
    {
        float halfHeight = targetCamera.orthographicSize + offscreenMargin;
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect + offscreenMargin;
        Vector2 offset = transform.position - targetCamera.transform.position;
        return Mathf.Abs(offset.x) > halfWidth || Mathf.Abs(offset.y) > halfHeight;
    }
}