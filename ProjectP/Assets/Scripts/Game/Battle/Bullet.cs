using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float offscreenMargin = 0.5f;

    private Rigidbody2D rb;
    private Camera targetCamera;
    private int remainingPierce;
    private int damage;
    private bool spent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Enemy가 Kinematic이므로 트리거 판정을 받으려면 탄환은 Dynamic이어야 합니다.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (targetCamera != null && IsOffscreen())
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 같은 프레임에 이미 죽은 적은 파괴되기 전까지 남아 있으므로 관통 횟수를 쓰지 않게 건너뜁니다.
        if (spent || !other.TryGetComponent(out Enemy enemy) || enemy.IsDead)
        {
            return;
        }

        enemy.TakeDamage(damage);

        if (remainingPierce > 0)
        {
            remainingPierce--;
            return;
        }

        spent = true;
        Destroy(gameObject);
    }

    public void Launch(Vector2 direction, BulletData data, Camera cam)
    {
        targetCamera = cam;
        remainingPierce = data.Pierce;
        damage = data.Attack;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.rotation = angle;
        rb.linearVelocity = direction * data.MoveSpeed;
    }

    private bool IsOffscreen()
    {
        float halfHeight = targetCamera.orthographicSize + offscreenMargin;
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect + offscreenMargin;
        Vector2 offset = transform.position - targetCamera.transform.position;
        return Mathf.Abs(offset.x) > halfWidth || Mathf.Abs(offset.y) > halfHeight;
    }
}