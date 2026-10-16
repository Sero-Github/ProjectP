using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 원거리 몬스터가 플레이어를 향해 쏘는 탄환. 플레이어에게 닿으면 대미지를 주고 사라진다.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyBullet : MonoBehaviour
{
    // 쏜 몬스터가 죽어 몬스터 풀로 돌아가도 탄환은 계속 날아가야 하므로, 풀은 몬스터마다가 아니라 프리팹마다 하나씩 공유한다.
    private static readonly Dictionary<EnemyBullet, ObjectPool<EnemyBullet>> pools = new Dictionary<EnemyBullet, ObjectPool<EnemyBullet>>();
    // 풀의 탄환을 모아 두는 부모. 씬이 바뀌어 파괴되면 안의 탄환도 함께 사라지므로 풀을 모두 새로 만든다.
    private static Transform poolRoot;

    [Tooltip("화면 경계에서 이 거리(유닛)만큼 더 벗어나면 사라집니다.")]
    [Min(0f)]
    [SerializeField] private float offscreenMargin = 0.5f;

    private Rigidbody2D rb;
    private Camera targetCamera;
    private int damage;
    private bool spent;
    // 몬스터는 화면 밖에서도 쏠 수 있으므로, 한 번 화면 안에 들어온 뒤에 벗어날 때만 사라집니다.
    private bool enteredScreen;
    // Spawn으로 만든 탄환은 사라질 때 파괴하지 않고 이 풀로 돌아간다. 씬에 직접 배치한 탄환은 null이다.
    private IObjectPool<EnemyBullet> pool;

    // 비활성화된 탄환이 있으면 꺼내 쓰고, 없을 때만 새로 만든다. 위치를 옮긴 뒤 켜야 이전 위치에서 보간되지 않는다.
    public static EnemyBullet Spawn(EnemyBullet prefab, Vector2 position)
    {
        if (poolRoot == null)
        {
            pools.Clear();
            poolRoot = new GameObject("EnemyBullets").transform;
        }

        if (!pools.TryGetValue(prefab, out ObjectPool<EnemyBullet> bulletPool))
        {
            bulletPool = CreatePool(prefab);
            pools.Add(prefab, bulletPool);
        }

        EnemyBullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(position, Quaternion.identity);
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    private static ObjectPool<EnemyBullet> CreatePool(EnemyBullet prefab)
    {
        ObjectPool<EnemyBullet> bulletPool = null;
        bulletPool = new ObjectPool<EnemyBullet>(
            createFunc: () =>
            {
                EnemyBullet bullet = Instantiate(prefab, poolRoot);
                bullet.gameObject.SetActive(false);
                bullet.pool = bulletPool;
                return bullet;
            },
            actionOnDestroy: bullet =>
            {
                if (bullet != null)
                {
                    Destroy(bullet.gameObject);
                }
            },
            // 탄환이 사라질 때 한 번만 돌려주므로 중복 반환 검사(매번 풀 전체 탐색)는 끈다.
            collectionCheck: false);
        return bulletPool;
    }

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
            Despawn();
        }
    }

    // 풀에서 다시 꺼내진 탄환도 발사마다 호출되므로, 지난 발사의 소진·화면 진입 상태를 여기서 모두 덮어쓴다.
    public void Launch(Vector2 direction, float speed, int attack)
    {
        damage = attack;
        spent = false;
        enteredScreen = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.rotation = angle;
        rb.linearVelocity = direction * speed;
        rb.angularVelocity = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || !other.TryGetComponent(out Player player) || player.IsDead)
        {
            return;
        }

        player.TakeDamage(damage);
        Despawn();
    }

    // 같은 물리 스텝에 다시 닿아도 두 번 반환되지 않도록 먼저 소진 처리한다.
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