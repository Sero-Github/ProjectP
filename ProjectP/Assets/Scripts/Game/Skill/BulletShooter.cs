using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 가방에 배치된 탄환 스킬 블록의 속성마다 탄환을 따로 쏜다. ⚔️ 스킬 문서 3-1을 따른다.
// 탄환 블록이 하나도 없으면 무속성 탄환만 쏘고, 하나라도 배치되면 무속성 탄환은 멈춘다.
// 광역 탄환과 지원은 구현되기 전까지 임시로 탄환으로 쏜다(FiresAsBullet).
public class BulletShooter : MonoBehaviour
{
    // 속성 하나의 탄환 발사 상태. 쿨타임은 속성마다 따로 흐른다.
    private class Lane
    {
        public Lane(Element element)
        {
            Element = element;
        }

        public Element Element { get; }
        // 같은 속성 탄환 블록의 스킬 점수 합계. 무속성은 0이다.
        public int Score { get; set; }
        public float CooldownTimer { get; set; }
        // 새로 생겼거나 쏠 대상이 없어 기다리던 중이면 true. 이때 쏘는 첫 발은 다른 속성과 간격을 둔다.
        public bool WaitingFirstShot { get; set; } = true;
    }

    // 쿨타임 감소 상한선이 정해지기 전까지, 매 프레임 발사되지 않도록 최소 쿨타임만 둡니다.
    private const float MinCooldown = 0.05f;

    // 스킬 점수 보너스(📊 스탯 4-1). 1점당 속성 공격력이 오르고, 10점마다 아래 보너스를 순서대로 반복합니다.
    private const int ScorePerBonus = 10;
    private const int BonusElementAttack = 10;
    private const float BonusCooldownMultiplier = 0.95f;
    private const int BonusProjectileCount = 1;

    [SerializeField] private BulletData data;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Camera targetCamera;

    private readonly List<Enemy> targets = new List<Enemy>();
    // 이번 프레임에 다른 속성 탄환이 이미 조준한 적. 같은 프레임에 쏘는 속성끼리는 가능하면 다른 적을 노린다.
    private readonly HashSet<Enemy> targetedThisFrame = new HashSet<Enemy>();
    // 탄환 블록이 없으면 무속성 lane 하나만 있다.
    private readonly List<Lane> lanes = new List<Lane> { new Lane(Element.None) };
    // 마지막으로 어떤 속성이든 탄환을 쏜 시각. 첫 발 간격을 둘 때 쓴다.
    private float lastFireTime = float.NegativeInfinity;
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

    private void Start()
    {
        // 구독 전에 SkillManager가 이미 상태를 만든 경우를 반영합니다.
        if (SkillManager.Instance != null)
        {
            RebuildLanes(SkillManager.Instance);
        }
    }

    private void OnEnable()
    {
        SkillManager.Changed += RebuildLanes;
    }

    private void OnDisable()
    {
        SkillManager.Changed -= RebuildLanes;
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

    // 탄환으로 쏠 스킬인지. 광역 탄환과 지원은 구현되기 전까지 임시로 탄환으로 쏜다.
    // TODO: 광역 탄환, 지원을 구현하면 SkillType.Bullet만 남긴다.
    private static bool FiresAsBullet(SkillType skill)
    {
        return skill == SkillType.Bullet || skill == SkillType.AreaBullet || skill == SkillType.Support;
    }

    // 배치된 탄환 블록의 속성마다 lane을 둔다. 이미 있던 속성의 lane은 남은 쿨타임을 유지한다.
    // 같은 속성에 탄환으로 쏘는 스킬이 여럿이면 하나의 lane으로 합쳐 점수를 더한다.
    private void RebuildLanes(SkillManager skillManager)
    {
        var previous = new List<Lane>(lanes);
        lanes.Clear();

        foreach (SkillManager.SkillState state in skillManager.Skills)
        {
            if (!FiresAsBullet(state.Skill))
            {
                continue;
            }

            Lane lane = lanes.Find(l => l.Element == state.Element);
            if (lane != null)
            {
                lane.Score += state.Score;
                continue;
            }

            lane = previous.Find(l => l.Element == state.Element) ?? new Lane(state.Element);
            lane.Score = state.Score;
            lanes.Add(lane);
        }

        // 탄환 블록이 없으면 무속성 탄환을 쏜다.
        if (lanes.Count == 0)
        {
            lanes.Add(previous.Find(l => l.Element == Element.None) ?? new Lane(Element.None));
        }
    }

    private void Update()
    {
        if (data == null || bulletPrefab == null || targetCamera == null)
        {
            return;
        }

        targetedThisFrame.Clear();
        foreach (Lane lane in lanes)
        {
            UpdateLane(lane);
        }
    }

    private void UpdateLane(Lane lane)
    {
        lane.CooldownTimer -= Time.deltaTime;
        if (lane.CooldownTimer > 0f)
        {
            return;
        }

        GetScoreBonus(lane.Score, out int bonusElementAttack, out int bonusCooldownCount, out int bonusProjectileCount);

        CollectTargets(data.ProjectileCount + bonusProjectileCount);
        if (targets.Count == 0)
        {
            // 쏠 대상이 없으면 쿨타임은 흐르지 않고, 적이 들어오는 즉시 발사합니다.
            lane.CooldownTimer = 0f;
            lane.WaitingFirstShot = true;
            return;
        }

        // 대미지는 발사 순간의 블록 스탯으로 정해 탄환에 넘기므로, 날아가는 중에 블록이 바뀌어도 영향받지 않습니다.
        BlockStatTotals blockStats = SkillManager.Instance != null ? SkillManager.Instance.Stats : BlockStatTotals.Empty;

        // 기본 쿨타임이 같은 속성들이 같은 순간에 첫 발을 쏘면 계속 함께 나가므로,
        // 첫 발은 직전 발사로부터 (쿨타임 / 속성 수)만큼 간격을 두어 속성들이 번갈아 나가게 합니다.
        if (lane.WaitingFirstShot && lanes.Count > 1)
        {
            float spacing = blockStats.ApplyCooldown(data.Cooldown) / lanes.Count;
            float elapsed = Time.time - lastFireTime;
            if (elapsed < spacing)
            {
                lane.CooldownTimer = spacing - elapsed;
                return;
            }
        }

        // 속성 효과는 대미지 증가가 적용되지 않은 속성 공격력으로 계산하므로 타격 대미지와 함께 넘깁니다.
        int elementAttack = blockStats.GetElementAttack(lane.Element) + bonusElementAttack;
        var hit = new ElementHit(blockStats.ApplyDamage(data.Attack, elementAttack), lane.Element, elementAttack);

        foreach (Enemy target in targets)
        {
            Fire(target, hit);
            targetedThisFrame.Add(target);
        }

        lane.WaitingFirstShot = false;
        lastFireTime = Time.time;

        float cooldown = blockStats.ApplyCooldown(data.Cooldown) * Mathf.Pow(BonusCooldownMultiplier, bonusCooldownCount);
        lane.CooldownTimer = Mathf.Max(MinCooldown, cooldown);
    }

    // 1점당 속성 공격력 +1, 10점마다 [속성 공격력 +10 → 쿨타임 감소 5% → 탄환 개수 +1]을 순서대로 반복합니다.
    // 50점이면 보너스 5번이므로 속성 공격력 +10 두 번, 쿨타임 감소 두 번, 탄환 개수 +1 한 번입니다.
    private static void GetScoreBonus(int score, out int elementAttack, out int cooldownCount, out int projectileCount)
    {
        int bonusCount = Mathf.Max(0, score) / ScorePerBonus;

        elementAttack = Mathf.Max(0, score) + BonusElementAttack * ((bonusCount + 2) / 3);
        cooldownCount = (bonusCount + 1) / 3;
        projectileCount = BonusProjectileCount * (bonusCount / 3);
    }

    // 화면 안의 적 중 가까운 순으로 최대 count마리를 중복 없이 고릅니다.
    // 이번 프레임에 다른 속성이 이미 조준한 적은 뒤로 미뤄, 적이 충분하면 서로 다른 적을 노리고 모자라면 겹쳐 노립니다.
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
        {
            bool aTargeted = targetedThisFrame.Contains(a);
            bool bTargeted = targetedThisFrame.Contains(b);
            if (aTargeted != bTargeted)
            {
                return aTargeted ? 1 : -1;
            }

            return ((Vector2)a.transform.position - origin).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - origin).sqrMagnitude);
        });

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

    private void Fire(Enemy target, ElementHit hit)
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
        bullet.Launch(direction, data, hit, targetCamera);
    }
}