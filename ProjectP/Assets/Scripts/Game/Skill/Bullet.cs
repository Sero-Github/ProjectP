using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float offscreenMargin = 0.5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera targetCamera;
    private int remainingPierce;
    private int damage;
    private bool spent;
    // BulletShooter가 만든 탄환은 사라질 때 파괴하지 않고 이 풀로 돌아간다. 씬에 직접 배치한 탄환은 null이다.
    private IObjectPool<Bullet> pool;

    // 탄환의 속성. 무속성 탄환은 None이다.
    public Element Element { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            Despawn();
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

        Despawn();
    }

    public void SetPool(IObjectPool<Bullet> bulletPool)
    {
        pool = bulletPool;
    }

    // damage는 BulletData의 공격력에 가방 블록 스탯을 적용한 값입니다.
    // 풀에서 다시 꺼내진 탄환도 발사마다 호출되므로, 지난 발사의 소진·관통·속성 상태를 여기서 모두 덮어씁니다.
    public void Launch(Vector2 direction, BulletData data, int damage, Element element, Camera cam)
    {
        spent = false;
        targetCamera = cam;
        remainingPierce = data.Pierce;
        this.damage = damage;
        Element = element;

        // 탄환 색은 속성 색을 따르며, 무속성은 core 0(스탯 블록) 색이다.
        if (spriteRenderer != null && BlockManager.Instance != null)
        {
            spriteRenderer.color = BlockManager.Instance.GetColorForCore((int)element);
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.rotation = angle;
        rb.linearVelocity = direction * data.MoveSpeed;
        rb.angularVelocity = 0f;
    }

    // 같은 물리 스텝에 다른 적과 또 닿아도 두 번 반환되지 않도록 먼저 소진 처리합니다.
    private void Despawn()
    {
        spent = true;

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

    private bool IsOffscreen()
    {
        float halfHeight = targetCamera.orthographicSize + offscreenMargin;
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect + offscreenMargin;
        Vector2 offset = transform.position - targetCamera.transform.position;
        return Mathf.Abs(offset.x) > halfWidth || Mathf.Abs(offset.y) > halfHeight;
    }
}