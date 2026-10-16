using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletShooter : MonoBehaviour
{
    // 쿨타임 감소 상한선이 정해지기 전까지, 매 프레임 발사되지 않도록 최소 쿨타임만 둡니다.
    private const float MinCooldown = 0.05f;

    [SerializeField] private BulletData data;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Camera targetCamera;

    private readonly List<Enemy> targets = new List<Enemy>();
    private float cooldownTimer;
    // 화면 밖으로 나가거나 관통을 다 쓴 탄환은 비활성화되어 여기로 돌아오고, 다음 발사 때 재사용된다.
    private ObjectPool<Bullet> pool;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        pool = CreatePool();
    }

    // 비활성화된 탄환이 있으면 꺼내 쓰고, 없을 때만 새로 만든다.
    // 위치를 옮긴 뒤 켜야 이전 위치에서 보간되지 않으므로, 꺼내고 돌려줄 때 켜고 끄는 것은 Fire와 Bullet이 맡는다.
    private ObjectPool<Bullet> CreatePool()
    {
        ObjectPool<Bullet> bulletPool = null;
        bulletPool = new ObjectPool<Bullet>(
            createFunc: () =>
            {
                Bullet bullet = Instantiate(bulletPrefab, bulletParent);
                bullet.gameObject.SetActive(false);
                bullet.SetPool(bulletPool);
                return bullet;
            },
            actionOnDestroy: bullet =>
            {
                if (bullet != null)
                {
                    Destroy(bullet.gameObject);
                }
            },
            // Bullet이 사라질 때 한 번만 돌려주므로 중복 반환 검사(매번 풀 전체 탐색)는 끈다.
            collectionCheck: false);
        return bulletPool;
    }

    private void Update()
    {
        if (data == null || bulletPrefab == null || targetCamera == null)
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
        {
            return;
        }

        CollectTargets(data.ProjectileCount);
        if (targets.Count == 0)
        {
            // 쏠 대상이 없으면 쿨타임은 흐르지 않고, 적이 들어오는 즉시 발사합니다.
            cooldownTimer = 0f;
            return;
        }

        // 대미지는 발사 순간의 블록 스탯으로 정해 탄환에 넘기므로, 날아가는 중에 블록이 바뀌어도 영향받지 않습니다.
        BlockStatTotals blockStats = SkillManager.Instance != null ? SkillManager.Instance.Stats : BlockStatTotals.Empty;
        int damage = blockStats.ApplyDamage(data.Attack);

        foreach (Enemy target in targets)
        {
            Fire(target, damage);
        }

        cooldownTimer = Mathf.Max(MinCooldown, blockStats.ApplyCooldown(data.Cooldown));
    }

    // 화면 안의 적 중 가까운 순으로 최대 count마리를 중복 없이 고릅니다.
    private void CollectTargets(int count)
    {
        targets.Clear();

        Vector2 origin = transform.position;
        foreach (Enemy enemy in Enemy.Actives)
        {
            if (IsOnScreen(enemy.transform.position))
            {
                targets.Add(enemy);
            }
        }

        targets.Sort((a, b) =>
            ((Vector2)a.transform.position - origin).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - origin).sqrMagnitude));

        if (targets.Count > count)
        {
            targets.RemoveRange(count, targets.Count - count);
        }
    }

    private bool IsOnScreen(Vector3 position)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(position);
        return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    private void Fire(Enemy target, int damage)
    {
        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)target.transform.position - origin).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        Bullet bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(origin, Quaternion.identity);
        bullet.gameObject.SetActive(true);
        bullet.Launch(direction, data, damage, targetCamera);
    }
}