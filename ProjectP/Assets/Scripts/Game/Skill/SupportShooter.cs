using System.Collections.Generic;
using UnityEngine;

// 가방에 배치된 지원 스킬 블록의 속성마다 지원 크리쳐를 거느린다. ⚔️ 스킬 문서 3-4를 따르며 플레이어 오브젝트에 붙인다.
// 크리쳐는 각자 쿨타임을 가지고 출격하며, 타겟 선택과 쿨타임, 궤적 타격은 출격하는 순간의 블록 스탯으로 정한다.
// 크리쳐는 출격할 때 플레이어 위치에서 나타나고, 돌아오면 사라져 쿨타임을 기다린다.
// 크리쳐는 프리팹 없이 코드로 만들고, 블록이 빠져 남는 크리쳐는 비활성화해 두었다가 다시 쓴다.
public class SupportShooter : MonoBehaviour
{
    // 속성 하나의 지원 상태.
    private class Lane
    {
        public Lane(Element element)
        {
            Element = element;
        }

        public Element Element { get; }
        // 같은 속성 지원 블록의 스킬 점수 합계.
        public int Score { get; set; }
        public List<SupportCreature> Creatures { get; } = new List<SupportCreature>();
    }

    // 쿨타임 감소 상한선이 정해지기 전까지, 매 프레임 출격하지 않도록 최소 쿨타임만 둡니다.
    private const float MinCooldown = 0.05f;

    // 지원 스킬 점수 보너스(📊 스탯 4-4). 10점마다 [속성 공격력 +5 → 쿨타임 감소 5% → 크리쳐 +1]을 반복합니다.
    private const int BonusElementAttack = 5;
    private const float BonusCooldownMultiplier = 0.95f;
    private const int BonusCreatureCount = 1;

    [SerializeField] private SupportData data;
    [SerializeField] private Camera targetCamera;

    [Tooltip("크리쳐를 그릴 Sorting Order. 몬스터(0)보다 위에 그립니다.")]
    [SerializeField] private int sortingOrder = 2;

    private readonly List<Lane> lanes = new List<Lane>();
    private readonly Stack<SupportCreature> creaturePool = new Stack<SupportCreature>();

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (data == null)
        {
            Debug.LogWarning($"{name}: 지원 데이터(data)가 연결되지 않아 지원 크리쳐가 나오지 않습니다.", this);
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

    // 배치된 지원 블록의 속성마다 lane을 두고, 점수에 맞는 수의 크리쳐를 유지한다.
    // 이미 있던 lane의 크리쳐는 쿨타임과 출격 상태를 그대로 이어간다.
    private void RebuildLanes(SkillManager skillManager)
    {
        if (data == null)
        {
            return;
        }

        var previous = new List<Lane>(lanes);
        lanes.Clear();

        foreach (SkillManager.SkillState state in skillManager.Skills)
        {
            if (state.Skill != SkillType.Support)
            {
                continue;
            }

            Lane lane = previous.Find(l => l.Element == state.Element) ?? new Lane(state.Element);
            previous.Remove(lane);
            lane.Score = state.Score;
            lanes.Add(lane);

            int count = data.CreatureCount + BonusCreatureCount * new SkillScore(lane.Score).ThirdCount;
            ResizeCreatures(lane, count);
        }

        // 블록이 빠진 속성의 크리쳐는 출격 중이어도 바로 거둔다.
        foreach (Lane removed in previous)
        {
            ResizeCreatures(removed, 0);
        }
    }

    private void ResizeCreatures(Lane lane, int count)
    {
        while (lane.Creatures.Count > count)
        {
            int last = lane.Creatures.Count - 1;
            SupportCreature creature = lane.Creatures[last];
            lane.Creatures.RemoveAt(last);
            creature.gameObject.SetActive(false);
            creaturePool.Push(creature);
        }

        Color color = BlockManager.Instance != null ? BlockManager.Instance.GetColorForCore((int)lane.Element) : Color.white;
        while (lane.Creatures.Count < count)
        {
            SupportCreature creature = creaturePool.Count > 0 ? creaturePool.Pop() : CreateCreature();
            creature.Initialize(data, color, sortingOrder);
            // 처음 생긴 크리쳐는 바로 출격할 수 있다.
            creature.CooldownTimer = 0f;
            creature.gameObject.SetActive(true);
            lane.Creatures.Add(creature);
        }
    }

    private SupportCreature CreateCreature()
    {
        var go = new GameObject(nameof(SupportCreature));
        go.SetActive(false);
        return go.AddComponent<SupportCreature>();
    }

    private void Update()
    {
        if (data == null || targetCamera == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        foreach (Lane lane in lanes)
        {
            foreach (SupportCreature creature in lane.Creatures)
            {
                if (creature.IsIdle)
                {
                    UpdateIdle(lane, creature, deltaTime);
                }

                creature.Tick(deltaTime, transform.position);
            }
        }
    }

    private void UpdateIdle(Lane lane, SupportCreature creature, float deltaTime)
    {
        creature.CooldownTimer -= deltaTime;
        if (creature.CooldownTimer > 0f)
        {
            // 쿨타임은 대기 중에만 흐른다.
            return;
        }

        Enemy target = FindTarget(lane);
        if (target == null)
        {
            // 쏠 대상이 없으면 쿨타임은 흐르지 않고, 적이 들어오는 즉시 출격합니다.
            creature.CooldownTimer = 0f;
            return;
        }

        // 궤적의 타격과 복귀 후 쿨타임은 출격하는 순간의 블록 스탯으로 정합니다.
        BlockStatTotals blockStats = SkillManager.Instance != null ? SkillManager.Instance.Stats : BlockStatTotals.Empty;
        var score = new SkillScore(lane.Score);

        // 속성 효과는 대미지 증가가 적용되지 않은 속성 공격력으로 계산하므로 타격 대미지와 함께 넘깁니다.
        int elementAttack = blockStats.GetElementAttack(lane.Element) + score.ElementAttack + BonusElementAttack * score.FirstCount;
        var hit = new ElementHit(blockStats.ApplyDamage(data.Attack, elementAttack), lane.Element, elementAttack);

        float cooldown = blockStats.ApplyCooldown(data.Cooldown) * Mathf.Pow(BonusCooldownMultiplier, score.SecondCount);
        creature.Launch(transform.position, target, hit, Mathf.Max(MinCooldown, cooldown));
    }

    // 화면 안의 적 중 플레이어에게 가장 가까운 적. 같은 lane의 다른 크리쳐가 돌진 중인 적은 다른 적이 없을 때만 고른다.
    private Enemy FindTarget(Lane lane)
    {
        Vector2 origin = transform.position;
        Enemy best = null;
        bool bestTargeted = true;
        float bestDistance = float.PositiveInfinity;
        foreach (Enemy enemy in Enemy.Actives)
        {
            if (enemy.IsDead || !IsOnScreen(enemy.transform.position))
            {
                continue;
            }

            bool targeted = IsTargeted(lane, enemy);
            float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (best == null || (bestTargeted && !targeted) || (bestTargeted == targeted && distance < bestDistance))
            {
                best = enemy;
                bestTargeted = targeted;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool IsTargeted(Lane lane, Enemy enemy)
    {
        foreach (SupportCreature creature in lane.Creatures)
        {
            if (creature.Target == enemy)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnScreen(Vector3 position)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(position);
        return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }
}
