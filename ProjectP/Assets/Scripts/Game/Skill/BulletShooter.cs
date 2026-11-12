using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

// 가방에 배치된 탄환, 광역 탄환 스킬 블록의 (스킬, 속성)마다 탄환을 따로 쏜다. ⚔️ 스킬 문서 3-1, 3-2를 따른다.
// 공격 스킬 블록(지원 포함)이 하나도 없으면 무속성 탄환만 쏘고, 하나라도 배치되면 무속성 탄환은 멈춘다.
// 무속성 탄환은 basicData, 속성 탄환은 elementData, 광역 탄환은 areaData의 수치를 쓴다. 지원은 SupportShooter가 맡는다.
public class BulletShooter : MonoBehaviour
{
    // (스킬, 속성) 하나의 발사 상태. 쿨타임은 lane마다 따로 흐른다. 무속성 탄환은 (Bullet, None)이다.
    private class Lane
    {
        public Lane(SkillType skill, Element element)
        {
            Skill = skill;
            Element = element;
        }

        public SkillType Skill { get; }
        public Element Element { get; }
        // 같은 스킬, 같은 속성 블록의 스킬 점수 합계. 무속성은 0이다.
        public int Score { get; set; }
        public float CooldownTimer { get; set; }
        // 새로 생겼거나 쏠 대상이 없어 기다리던 중이면 true. 이때 쏘는 첫 발은 다른 lane과 간격을 둔다.
        public bool WaitingFirstShot { get; set; } = true;
    }

    // 조준할 적 후보와 정렬 기준. 정렬 중에 바뀌지 않으므로 모을 때 한 번만 계산한다.
    private readonly struct TargetCandidate
    {
        public TargetCandidate(Enemy enemy, bool targeted, float sqrDistance)
        {
            Enemy = enemy;
            Targeted = targeted;
            SqrDistance = sqrDistance;
        }

        public Enemy Enemy { get; }
        // 이번 프레임에 다른 lane이 이미 조준한 적이면 true.
        public bool Targeted { get; }
        public float SqrDistance { get; }
    }

    // 메서드 그룹을 Sort에 바로 넘기면 호출마다 델리게이트가 할당되므로 한 번만 만들어 둔다.
    private static readonly System.Comparison<TargetCandidate> CandidateComparison = CompareCandidates;

    // 쿨타임 감소 상한선이 정해지기 전까지, 매 프레임 발사되지 않도록 최소 쿨타임만 둡니다.
    private const float MinCooldown = 0.05f;

    // 탄환 스킬 점수 보너스(📊 스탯 4-1). 10점마다 [속성 공격력 +10 → 쿨타임 감소 5% → 탄환 개수 +1]을 반복합니다.
    private const int BulletBonusElementAttack = 10;
    private const float BulletBonusCooldownMultiplier = 0.95f;
    private const int BulletBonusProjectileCount = 1;

    // 광역 탄환 스킬 점수 보너스(📊 스탯 4-2). 10점마다 [속성 공격력 +5 → 쿨타임 감소 5% → 지속 시간, 범위 +5%]를 반복합니다.
    private const int AreaBonusElementAttack = 5;
    private const float AreaBonusCooldownMultiplier = 0.95f;
    private const int AreaBonusDurationPercent = 5;
    private const int AreaBonusRangePercent = 5;

    [Tooltip("무속성 탄환. 공격 스킬 블록이 없을 때만 쏩니다.")]
    [FormerlySerializedAs("data")]
    [SerializeField] private BulletData basicData;
    [Tooltip("속성 탄환. 탄환 스킬 블록의 속성마다 쏩니다.")]
    [SerializeField] private BulletData elementData;
    [Tooltip("광역 탄환. 광역 탄환 스킬 블록의 속성마다 쏩니다.")]
    [SerializeField] private AreaBulletData areaData;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Camera targetCamera;

    private readonly List<Enemy> targets = new List<Enemy>();
    private readonly List<TargetCandidate> candidates = new List<TargetCandidate>();
    // 이번 프레임에 다른 lane이 이미 조준한 적. 같은 프레임에 쏘는 lane끼리는 가능하면 다른 적을 노린다.
    private readonly HashSet<Enemy> targetedThisFrame = new HashSet<Enemy>();
    // 공격 스킬 블록이 없으면 무속성 lane 하나만 있다.
    private readonly List<Lane> lanes = new List<Lane> { new Lane(SkillType.Bullet, Element.None) };
    // 마지막으로 어떤 lane이든 탄환을 쏜 시각. 첫 발 간격을 둘 때 쓴다.
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

        // 데이터가 빠진 lane은 조용히 쏘지 않으므로, 어떤 탄환이 빠졌는지 시작할 때 알린다.
        if (basicData == null)
        {
            Debug.LogWarning($"{name}: 무속성 탄환 데이터(basicData)가 연결되지 않아 무속성 탄환을 쏘지 않습니다.", this);
        }

        if (elementData == null)
        {
            Debug.LogWarning($"{name}: 속성 탄환 데이터(elementData)가 연결되지 않아 속성 탄환을 쏘지 않습니다.", this);
        }

        if (areaData == null)
        {
            Debug.LogWarning($"{name}: 광역 탄환 데이터(areaData)가 연결되지 않아 광역 탄환을 쏘지 않습니다.", this);
        }
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

    // 배치된 탄환, 광역 탄환 블록의 (스킬, 속성)마다 lane을 둔다. 이미 있던 lane은 남은 쿨타임을 유지한다.
    private void RebuildLanes(SkillManager skillManager)
    {
        var previous = new List<Lane>(lanes);
        lanes.Clear();

        // SkillManager.Skills는 (스킬, 속성)마다 하나씩이므로 같은 lane이 두 번 생기지 않는다.
        foreach (SkillManager.SkillState state in skillManager.Skills)
        {
            if (state.Skill != SkillType.Bullet && state.Skill != SkillType.AreaBullet)
            {
                continue;
            }

            Lane lane = previous.Find(l => l.Skill == state.Skill && l.Element == state.Element)
                ?? new Lane(state.Skill, state.Element);
            lane.Score = state.Score;
            lanes.Add(lane);
        }

        // 지원을 포함해 공격 스킬 블록이 하나도 없으면 무속성 탄환을 쏜다.
        if (skillManager.Skills.Count == 0)
        {
            lanes.Add(previous.Find(l => l.Element == Element.None) ?? new Lane(SkillType.Bullet, Element.None));
        }
    }

    private void Update()
    {
        if (bulletPrefab == null || targetCamera == null)
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
        if (!TryGetBaseCooldown(lane, out float baseCooldown))
        {
            return;
        }

        lane.CooldownTimer -= Time.deltaTime;
        if (lane.CooldownTimer > 0f)
        {
            return;
        }

        var score = new SkillScore(lane.Score);

        // 광역 탄환은 한 번에 한 발만 쏜다.
        int projectileCount = lane.Skill == SkillType.Bullet
            ? GetBulletData(lane).ProjectileCount + BulletBonusProjectileCount * score.ThirdCount
            : 1;
        CollectTargets(projectileCount);
        if (targets.Count == 0)
        {
            // 쏠 대상이 없으면 쿨타임은 흐르지 않고, 적이 들어오는 즉시 발사합니다.
            lane.CooldownTimer = 0f;
            lane.WaitingFirstShot = true;
            return;
        }

        // 대미지는 발사 순간의 블록 스탯으로 정해 탄환에 넘기므로, 날아가는 중에 블록이 바뀌어도 영향받지 않습니다.
        BlockStatTotals blockStats = SkillManager.Instance != null ? SkillManager.Instance.Stats : BlockStatTotals.Empty;

        // 기본 쿨타임이 같은 lane들이 같은 순간에 첫 발을 쏘면 계속 함께 나가므로,
        // 첫 발은 직전 발사로부터 (쿨타임 / lane 수)만큼 간격을 두어 lane들이 번갈아 나가게 합니다.
        if (lane.WaitingFirstShot && lanes.Count > 1)
        {
            float spacing = blockStats.ApplyCooldown(baseCooldown) / lanes.Count;
            float elapsed = Time.time - lastFireTime;
            if (elapsed < spacing)
            {
                lane.CooldownTimer = spacing - elapsed;
                return;
            }
        }

        float cooldownMultiplier;
        if (lane.Skill == SkillType.AreaBullet)
        {
            FireAreaBullets(lane, score, blockStats);
            cooldownMultiplier = Mathf.Pow(AreaBonusCooldownMultiplier, score.SecondCount);
        }
        else
        {
            FireBullets(lane, score, blockStats);
            cooldownMultiplier = Mathf.Pow(BulletBonusCooldownMultiplier, score.SecondCount);
        }

        lane.WaitingFirstShot = false;
        lastFireTime = Time.time;
        lane.CooldownTimer = Mathf.Max(MinCooldown, blockStats.ApplyCooldown(baseCooldown) * cooldownMultiplier);
    }

    // lane이 쓸 데이터가 연결되어 있으면 그 기본 쿨타임을 돌려준다.
    private bool TryGetBaseCooldown(Lane lane, out float cooldown)
    {
        if (lane.Skill == SkillType.AreaBullet)
        {
            cooldown = areaData != null ? areaData.Cooldown : 0f;
            return areaData != null;
        }

        BulletData data = GetBulletData(lane);
        cooldown = data != null ? data.Cooldown : 0f;
        return data != null;
    }

    private BulletData GetBulletData(Lane lane)
    {
        return lane.Element == Element.None ? basicData : elementData;
    }

    // 속성 효과는 대미지 증가가 적용되지 않은 속성 공격력으로 계산하므로 타격 대미지와 함께 넘깁니다.
    private static ElementHit CreateHit(Lane lane, BlockStatTotals blockStats, int baseAttack, int bonusElementAttack)
    {
        int elementAttack = blockStats.GetElementAttack(lane.Element) + bonusElementAttack;
        return new ElementHit(blockStats.ApplyDamage(baseAttack, elementAttack), lane.Element, elementAttack);
    }

    private void FireBullets(Lane lane, SkillScore score, BlockStatTotals blockStats)
    {
        BulletData data = GetBulletData(lane);
        ElementHit hit = CreateHit(lane, blockStats, data.Attack, score.ElementAttack + BulletBonusElementAttack * score.FirstCount);

        foreach (Enemy target in targets)
        {
            Bullet bullet = Spawn(target, out Vector2 direction);
            bullet.Launch(direction, data.MoveSpeed, data.Pierce, hit, targetCamera);
            targetedThisFrame.Add(target);
        }
    }

    // 공격 지대의 크기와 유지 시간은 발사 순간의 지속 시간, 범위 증가 스탯과 스킬 점수 보너스로 정합니다.
    private void FireAreaBullets(Lane lane, SkillScore score, BlockStatTotals blockStats)
    {
        ElementHit hit = CreateHit(lane, blockStats, areaData.Attack, score.ElementAttack + AreaBonusElementAttack * score.FirstCount);
        var impact = new Bullet.AreaImpact(
            blockStats.ApplyRange(areaData.ZoneDiameter * 0.5f, AreaBonusRangePercent * score.ThirdCount),
            blockStats.ApplyDuration(areaData.ZoneDuration, AreaBonusDurationPercent * score.ThirdCount),
            areaData.ZoneGrowTime,
            areaData.ZoneShrinkTime);

        foreach (Enemy target in targets)
        {
            Bullet bullet = Spawn(target, out Vector2 direction);
            bullet.LaunchArea(direction, areaData.MoveSpeed, impact, hit, targetCamera);
            targetedThisFrame.Add(target);
        }
    }

    // 화면 안의 적 중 가까운 순으로 최대 count마리를 중복 없이 고릅니다.
    // 이번 프레임에 다른 lane이 이미 조준한 적은 뒤로 미뤄, 적이 충분하면 서로 다른 적을 노리고 모자라면 겹쳐 노립니다.
    // 정렬 기준(조준 여부, 거리)은 후보마다 한 번만 계산해 두고, 비교할 때마다 transform.position과 HashSet을 읽지 않습니다.
    private void CollectTargets(int count)
    {
        targets.Clear();
        candidates.Clear();

        Vector2 origin = transform.position;
        // 인터페이스 foreach는 열거자 박싱으로 매번 GC 할당이 생기므로 인덱스로 순회한다.
        IReadOnlyList<Enemy> enemies = Enemy.Actives;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            Vector3 position = enemy.transform.position;
            if (IsOnScreen(position))
            {
                candidates.Add(new TargetCandidate(enemy, targetedThisFrame.Contains(enemy), ((Vector2)position - origin).sqrMagnitude));
            }
        }

        candidates.Sort(CandidateComparison);

        int taken = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < taken; i++)
        {
            targets.Add(candidates[i].Enemy);
        }

        candidates.Clear();
    }

    // 조준하지 않은 적이 먼저, 같으면 가까운 적이 먼저다.
    private static int CompareCandidates(TargetCandidate a, TargetCandidate b)
    {
        if (a.Targeted != b.Targeted)
        {
            return a.Targeted ? 1 : -1;
        }

        return a.SqrDistance.CompareTo(b.SqrDistance);
    }

    private bool IsOnScreen(Vector3 position)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(position);
        return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    // 풀에서 탄환을 꺼내 플레이어 위치에 켜고, target을 향하는 방향을 돌려줍니다.
    private Bullet Spawn(Enemy target, out Vector2 direction)
    {
        Vector2 origin = transform.position;
        direction = ((Vector2)target.transform.position - origin).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        // 회전은 Launch에서 발사 방향으로 맞춘다.
        Bullet bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(origin, Quaternion.identity);
        bullet.gameObject.SetActive(true);
        return bullet;
    }
}
