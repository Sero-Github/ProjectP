using System.Collections.Generic;
using UnityEngine;

// 광역 탄환의 공격 지대와 지원 크리쳐의 궤적처럼, 범위 안의 몬스터를 일정 간격으로 타격하는 지대를 관리한다.
// ⚔️ 스킬 문서 3-2(광역 탄환), 3-4(지원)를 따른다.
// - 몬스터는 자신에게 닿은 지대 중 가장 최근에 생긴 지대 하나에게만 맞는다. 겹치면 최근 색이 덮고 대미지는 중복되지 않는다.
// - 같은 지대 안에서는 몬스터마다 hitInterval에 한 번 맞고, 다른 지대에 들어서면 내부 쿨타임과 관계없이 바로 맞는다.
// - 지원 궤적처럼 여러 원이 하나의 지대를 이루면 같은 그룹 id를 써서, 궤적을 따라 움직여도 다른 지대로 치지 않는다.
// 씬에 배치하면 인스펙터 값으로 조정할 수 있고, 없으면 처음 지대가 생길 때 기본값으로 자동 생성된다.
public class AttackZoneSystem : MonoBehaviour
{
    private enum ZoneStyle
    {
        Area,   // 원 하나가 커지며 생기고 작아지며 사라진다.
        Trail,  // 작은 점을 흩뿌리고 옅어지며 사라진다.
    }

    private class Zone
    {
        public ZoneStyle Style;
        public int Group;
        public ElementHit Hit;
        public Color Color;
        public Vector2 Center;
        public float MaxRadius;
        // 지금 판정에 쓰는 반경. Area는 커지고 작아지는 동안 보이는 크기를 따른다.
        public float Radius;
        public float Elapsed;
        public float Duration;
        public float GrowTime;
        public float ShrinkTime;
        public readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
    }

    // 몬스터 한 마리가 마지막으로 맞은 지대 그룹과 다음 타격까지 남은 시간.
    private struct Contact
    {
        public int Group;
        public float Timer;
    }

    // 지대는 폭발 연출(-1)과 몬스터(0)보다 아래에 그린다. 최근 지대가 위에 오도록 생성 순서대로 올리고, 위쪽 끝에 닿으면 다시 채운다.
    private const int LowestSortingOrder = -30000;
    private const int HighestSortingOrder = -2;

    private static AttackZoneSystem instance;
    private static int nextGroup;

    [Tooltip("같은 지대 안에서 몬스터 한 마리가 다시 맞기까지의 시간(초)")]
    [Min(0.05f)]
    [SerializeField] private float hitInterval = 1f;

    [Header("공격 지대")]
    [Tooltip("공격 지대 원의 불투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float areaAlpha = 0.35f;

    [Header("궤적")]
    [Tooltip("궤적 점 하나마다 흩뿌리는 작은 원의 수")]
    [Min(1)]
    [SerializeField] private int trailDotsPerPoint = 3;

    [Tooltip("흩뿌리는 원의 지름. 궤적 폭에 대한 비율의 최소, 최대값입니다.")]
    [SerializeField] private Vector2 trailDotScale = new Vector2(0.4f, 0.9f);

    [Tooltip("궤적 점의 불투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float trailAlpha = 0.25f;

    [Tooltip("궤적이 사라지기 전 옅어지는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float trailFadeTime = 0.5f;

    // 생성 순서대로 들어 있다. 뒤쪽일수록 최근 지대다.
    private readonly List<Zone> zones = new List<Zone>();
    private readonly Stack<Zone> zonePool = new Stack<Zone>();
    private readonly Stack<SpriteRenderer> rendererPool = new Stack<SpriteRenderer>();
    private readonly Dictionary<Enemy, Contact> contacts = new Dictionary<Enemy, Contact>();
    // 타격하다 몬스터가 죽어 Enemy.Actives가 바뀌어도 순회가 깨지지 않도록 복사해 쓴다.
    private readonly List<Enemy> enemyBuffer = new List<Enemy>();
    private int nextSortingOrder = LowestSortingOrder;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        Enemy.Killed += OnEnemyKilled;
        Enemy.Despawned += OnEnemyKilled;
    }

    private void OnDisable()
    {
        Enemy.Killed -= OnEnemyKilled;
        Enemy.Despawned -= OnEnemyKilled;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // 지원 궤적처럼 여러 지대를 하나로 묶을 때 쓰는 그룹 id를 새로 만든다.
    public static int CreateGroup()
    {
        return ++nextGroup;
    }

    // 광역 탄환의 공격 지대. growTime 동안 커지며 생기고, duration이 지나면 shrinkTime 동안 작아지며 사라진다.
    public static void SpawnArea(Vector2 center, float radius, float duration, float growTime, float shrinkTime, ElementHit hit)
    {
        if (radius <= 0f)
        {
            return;
        }

        Zone zone = GetInstance().CreateZone(ZoneStyle.Area, CreateGroup(), center, radius, duration, hit);
        zone.GrowTime = Mathf.Min(growTime, duration);
        zone.ShrinkTime = shrinkTime;
        zone.Radius = zone.GrowTime > 0f ? 0f : radius;
        instance.AddRenderers(zone, 1);
        instance.ApplyVisual(zone);
    }

    // 지원 크리쳐의 궤적 점 하나. 같은 출격의 점들은 같은 group을 쓴다.
    public static void SpawnTrail(int group, Vector2 center, float radius, float duration, ElementHit hit)
    {
        if (radius <= 0f)
        {
            return;
        }

        Zone zone = GetInstance().CreateZone(ZoneStyle.Trail, group, center, radius, duration, hit);
        zone.Radius = radius;
        instance.AddRenderers(zone, instance.trailDotsPerPoint);
        instance.ApplyVisual(zone);
    }

    private static AttackZoneSystem GetInstance()
    {
        if (instance == null)
        {
            new GameObject(nameof(AttackZoneSystem)).AddComponent<AttackZoneSystem>();
        }

        return instance;
    }

    private Zone CreateZone(ZoneStyle style, int group, Vector2 center, float radius, float duration, ElementHit hit)
    {
        Zone zone = zonePool.Count > 0 ? zonePool.Pop() : new Zone();
        zone.Style = style;
        zone.Group = group;
        zone.Hit = hit;
        zone.Color = GetElementColor(hit.Element);
        zone.Center = center;
        zone.MaxRadius = radius;
        zone.Elapsed = 0f;
        zone.Duration = duration;
        zone.GrowTime = 0f;
        zone.ShrinkTime = 0f;
        zones.Add(zone);
        return zone;
    }

    private void Update()
    {
        UpdateZones(Time.deltaTime);
        HitEnemies(Time.deltaTime);
    }

    private void UpdateZones(float deltaTime)
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            Zone zone = zones[i];
            zone.Elapsed += deltaTime;

            if (zone.Elapsed >= zone.Duration + zone.ShrinkTime)
            {
                zones.RemoveAt(i);
                ReleaseZone(zone);
                continue;
            }

            if (zone.Style == ZoneStyle.Area)
            {
                zone.Radius = zone.MaxRadius * GetAreaScale(zone);
            }

            ApplyVisual(zone);
        }
    }

    // 몬스터마다 닿은 지대 중 가장 최근 지대를 골라 타격한다.
    private void HitEnemies(float deltaTime)
    {
        enemyBuffer.Clear();
        enemyBuffer.AddRange(Enemy.Actives);

        foreach (Enemy enemy in enemyBuffer)
        {
            // 앞서 다른 몬스터의 폭발로 죽었을 수 있다.
            if (enemy.IsDead || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            bool hasContact = contacts.TryGetValue(enemy, out Contact contact);
            if (hasContact)
            {
                contact.Timer -= deltaTime;
            }

            Zone zone = FindTopZone(enemy.transform.position, enemy.BodyRadius);
            if (zone == null)
            {
                // 내부 쿨타임이 끝났으면 다음에 어느 지대에 들어가든 바로 맞으므로 기록을 지운다.
                if (!hasContact)
                {
                    continue;
                }

                if (contact.Timer > 0f)
                {
                    contacts[enemy] = contact;
                }
                else
                {
                    contacts.Remove(enemy);
                }

                continue;
            }

            if (hasContact && contact.Group == zone.Group && contact.Timer > 0f)
            {
                contacts[enemy] = contact;
                continue;
            }

            enemy.TakeHit(zone.Hit);

            // 이 타격으로 죽었으면 Killed에서 이미 기록을 지웠다.
            if (!enemy.IsDead)
            {
                contacts[enemy] = new Contact { Group = zone.Group, Timer = hitInterval };
            }
        }

        enemyBuffer.Clear();
    }

    private Zone FindTopZone(Vector2 position, float bodyRadius)
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            Zone zone = zones[i];
            float reach = zone.Radius + bodyRadius;
            if (zone.Radius > 0f && (position - zone.Center).sqrMagnitude <= reach * reach)
            {
                return zone;
            }
        }

        return null;
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        contacts.Remove(enemy);
    }

    // Area 지대의 크기 비율. 커지는 동안 0→1, 유지 중 1, 작아지는 동안 1→0이다.
    private static float GetAreaScale(Zone zone)
    {
        if (zone.Elapsed < zone.GrowTime)
        {
            return zone.Elapsed / zone.GrowTime;
        }

        if (zone.Elapsed > zone.Duration)
        {
            return zone.ShrinkTime > 0f ? 1f - (zone.Elapsed - zone.Duration) / zone.ShrinkTime : 0f;
        }

        return 1f;
    }

    private void AddRenderers(Zone zone, int count)
    {
        int sortingOrder = TakeSortingOrder();

        for (int i = 0; i < count; i++)
        {
            SpriteRenderer spriteRenderer = rendererPool.Count > 0 ? rendererPool.Pop() : CreateRenderer();
            spriteRenderer.sortingOrder = sortingOrder;

            Transform t = spriteRenderer.transform;
            if (zone.Style == ZoneStyle.Trail)
            {
                // 궤적 폭 안에서 위치와 크기를 흩어 에어브러시처럼 보이게 한다.
                float scale = zone.MaxRadius * 2f * Random.Range(trailDotScale.x, trailDotScale.y);
                t.position = zone.Center + Random.insideUnitCircle * zone.MaxRadius * 0.6f;
                t.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                t.position = zone.Center;
            }

            spriteRenderer.gameObject.SetActive(true);
            zone.Renderers.Add(spriteRenderer);
        }
    }

    private void ApplyVisual(Zone zone)
    {
        Color color = zone.Color;

        if (zone.Style == ZoneStyle.Area)
        {
            float diameter = zone.Radius * 2f;
            color.a = areaAlpha;
            foreach (SpriteRenderer spriteRenderer in zone.Renderers)
            {
                spriteRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
                spriteRenderer.color = color;
            }

            return;
        }

        float remaining = zone.Duration - zone.Elapsed;
        float fade = trailFadeTime > 0f ? Mathf.Clamp01(remaining / trailFadeTime) : 1f;
        color.a = trailAlpha * fade;
        foreach (SpriteRenderer spriteRenderer in zone.Renderers)
        {
            spriteRenderer.color = color;
        }
    }

    private void ReleaseZone(Zone zone)
    {
        foreach (SpriteRenderer spriteRenderer in zone.Renderers)
        {
            spriteRenderer.gameObject.SetActive(false);
            rendererPool.Push(spriteRenderer);
        }

        zone.Renderers.Clear();
        zonePool.Push(zone);
    }

    // 새 지대가 기존 지대 위에 그려지도록 정렬 순서를 하나씩 올린다.
    // 위쪽 끝에 닿으면 남아 있는 지대를 생성 순서대로 아래부터 다시 채운다.
    private int TakeSortingOrder()
    {
        if (nextSortingOrder > HighestSortingOrder)
        {
            nextSortingOrder = LowestSortingOrder;
            foreach (Zone zone in zones)
            {
                if (zone.Renderers.Count == 0)
                {
                    continue;
                }

                foreach (SpriteRenderer spriteRenderer in zone.Renderers)
                {
                    spriteRenderer.sortingOrder = nextSortingOrder;
                }

                nextSortingOrder++;
            }
        }

        return nextSortingOrder++;
    }

    private SpriteRenderer CreateRenderer()
    {
        var go = new GameObject("ZoneCircle");
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CircleSprite.Get();
        return spriteRenderer;
    }

    // 속성 색은 탄환, 블록과 같은 core 색을 쓴다.
    private static Color GetElementColor(Element element)
    {
        return BlockManager.Instance != null ? BlockManager.Instance.GetColorForCore((int)element) : Color.white;
    }
}