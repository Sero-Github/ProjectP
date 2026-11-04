using UnityEngine;

// 플레이어를 지원하는 작은 원형 크리쳐. ⚔️ 스킬 문서 3-4의 행동을 반복한다.
// 1. 대기: 보이지 않은 채 쿨타임을 기다린다. 타겟은 SupportShooter가 골라 Launch로 넘긴다.
// 2. 돌진: 플레이어 위치에서 나타나 타겟을 향해 직선으로 이동한다. 타겟이 죽으면 마지막으로 본 위치까지 간다.
// 3. 통과: 감속한 채 같은 방향으로 몬스터 크기만큼 더 간 뒤 잠깐 멈춘다.
// 4. 복귀: 플레이어에게 돌아오면 사라지고 다시 쿨타임을 기다린다.
// 출격하는 동안 이동한 자리에 궤적(공격 지대)을 남기며, 대미지는 궤적만 준다. 이동은 SupportShooter가 매 프레임 Tick으로 진행한다.
public class SupportCreature : MonoBehaviour
{
    private enum State
    {
        Idle,
        Dash,
        Pass,
        Pause,
        Return,
    }

    private SupportData data;
    private SpriteRenderer spriteRenderer;
    private State state;
    private Enemy target;
    private Vector2 destination;
    private Vector2 passDirection;
    private float passRemaining;
    private float pauseTimer;
    // 복귀한 뒤 흐를 쿨타임. 출격할 때 그 순간의 블록 스탯으로 정해진다.
    private float sortieCooldown;
    // 이번 출격의 궤적. 같은 출격의 궤적 점은 같은 그룹이라 따라 걸어도 다른 지대로 치지 않는다.
    private int trailGroup;
    private ElementHit trailHit;
    private Vector2 lastTrailPoint;

    public bool IsIdle => state == State.Idle;
    // 돌진 중인 타겟. 같은 속성의 다른 크리쳐는 가능하면 이 적을 피한다.
    public Enemy Target => state == State.Dash ? target : null;
    // 대기 중에만 흐른다.
    public float CooldownTimer { get; set; }

    // 풀에서 꺼낼 때마다 호출된다. 출격 중이던 상태는 모두 버리고 보이지 않은 채 대기한다.
    public void Initialize(SupportData supportData, Color color, int sortingOrder)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CircleSprite.Get();
        }

        data = supportData;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        transform.localScale = new Vector3(data.CreatureDiameter, data.CreatureDiameter, 1f);

        Hide();
    }

    // origin(플레이어 위치)에서 나타나 enemy를 향해 출격한다. hit은 궤적이 줄 타격, cooldown은 돌아온 뒤 흐를 쿨타임이다.
    public void Launch(Vector2 origin, Enemy enemy, ElementHit hit, float cooldown)
    {
        transform.position = origin;
        spriteRenderer.enabled = true;

        target = enemy;
        destination = enemy.transform.position;
        // 돌진 중 타겟이 죽어도 지나갈 거리를 알 수 있도록 미리 잰다.
        passRemaining = enemy.BodyRadius * 2f;
        trailHit = hit;
        sortieCooldown = cooldown;
        trailGroup = AttackZoneSystem.CreateGroup();
        state = State.Dash;

        DropTrail(origin);
    }

    // home은 복귀할 플레이어 위치이다. 대기 중에는 보이지 않으므로 아무것도 하지 않는다.
    public void Tick(float deltaTime, Vector2 home)
    {
        switch (state)
        {
            case State.Dash:
                UpdateDash(deltaTime);
                break;
            case State.Pass:
                UpdatePass(deltaTime);
                break;
            case State.Pause:
                pauseTimer -= deltaTime;
                if (pauseTimer <= 0f)
                {
                    state = State.Return;
                }
                break;
            case State.Return:
                UpdateReturn(deltaTime, home);
                break;
        }
    }

    private void UpdateDash(float deltaTime)
    {
        if (target != null && !target.IsDead && target.isActiveAndEnabled)
        {
            destination = target.transform.position;
        }
        else
        {
            target = null;
        }

        Vector2 position = transform.position;
        Vector2 offset = destination - position;
        if (offset.sqrMagnitude > 0.000001f)
        {
            passDirection = offset.normalized;
        }

        if (MoveTowards(destination, data.MoveSpeed * deltaTime))
        {
            target = null;
            state = State.Pass;
        }
    }

    private void UpdatePass(float deltaTime)
    {
        float step = Mathf.Min(passRemaining, data.MoveSpeed * data.PassSpeedMultiplier * deltaTime);
        passRemaining -= step;
        Move((Vector2)transform.position + passDirection * step);

        if (passRemaining <= 0f)
        {
            pauseTimer = data.ReturnDelay;
            state = State.Pause;
        }
    }

    // 플레이어에게 닿으면 사라지고 쿨타임을 기다린다. 플레이어가 움직여도 현재 위치를 따라간다.
    private void UpdateReturn(float deltaTime, Vector2 home)
    {
        if (MoveTowards(home, data.MoveSpeed * deltaTime))
        {
            Hide();
            CooldownTimer = sortieCooldown;
        }
    }

    private void Hide()
    {
        spriteRenderer.enabled = false;
        state = State.Idle;
        target = null;
    }

    // 목표에 닿았으면 true.
    private bool MoveTowards(Vector2 goal, float maxDistance)
    {
        Vector2 next = Vector2.MoveTowards(transform.position, goal, maxDistance);
        Move(next);
        return next == goal;
    }

    // 이동하면서 궤적 점 간격만큼 갈 때마다 궤적을 남긴다. 빠르게 움직여도 간격이 벌어지지 않도록 사이를 채운다.
    private void Move(Vector2 next)
    {
        transform.position = next;

        Vector2 offset = next - lastTrailPoint;
        float distance = offset.magnitude;
        if (distance < data.TrailSpacing)
        {
            return;
        }

        Vector2 direction = offset / distance;
        int count = Mathf.FloorToInt(distance / data.TrailSpacing);
        for (int i = 1; i <= count; i++)
        {
            DropTrail(lastTrailPoint + direction * data.TrailSpacing);
        }
    }

    private void DropTrail(Vector2 point)
    {
        lastTrailPoint = point;
        AttackZoneSystem.SpawnTrail(trailGroup, point, data.TrailWidth * 0.5f, data.TrailDuration, trailHit);
    }
}
