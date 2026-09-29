using System.Collections.Generic;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{
    [SerializeField] private BulletData data;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Camera targetCamera;

    private readonly List<Enemy> targets = new List<Enemy>();
    private float cooldownTimer;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
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

        foreach (Enemy target in targets)
        {
            Fire(target);
        }

        cooldownTimer = data.Cooldown;
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

    private void Fire(Enemy target)
    {
        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)target.transform.position - origin).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        Bullet bullet = Instantiate(bulletPrefab, origin, Quaternion.identity, bulletParent);
        bullet.Launch(direction, data, targetCamera);
    }
}