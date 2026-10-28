using System.Collections.Generic;

// 몬스터 한 마리의 속성 주입 상태. ⚡ 속성 문서를 따르며 Enemy가 하나씩 가진다.
// - 다른 속성에 맞으면 그 속성이 주입되고, 이전 속성의 스택은 사라진다. 같은 속성에 맞으면 주입이 아니다.
// - 주황은 주입되지 않고, 주입된 속성의 효과를 (주입된 속성 공격력 + 주황 속성 공격력)의 세기로 한 번 더 발생시킨다.
// - 스택은 효과를 계산할 속성 공격력을 하나씩 가진다. 첫 스택은 주입한 타격의 속성 공격력이다.
// 대미지를 주거나 몬스터를 움직이는 일은 Enemy가 Apply의 결과와 배율 값을 보고 처리한다.
public class EnemyElementStatus
{
    // Apply로 생긴 즉시 효과. 대미지나 배율이 0이면 해당 효과가 없다.
    public readonly struct HitResult
    {
        public HitResult(bool injected, int explosionDamage, float knockbackMultiplier)
        {
            Injected = injected;
            ExplosionDamage = explosionDamage;
            KnockbackMultiplier = knockbackMultiplier;
        }

        // 속성이 바뀌어 몬스터 색을 바꿔야 하면 true.
        public bool Injected { get; }
        // 빨강 폭발 대미지. 주변 몬스터에게 속성 주입 없이 준다.
        public int ExplosionDamage { get; }
        // 회색 넉백 거리 배율.
        public float KnockbackMultiplier { get; }
    }

    private readonly ElementSettings settings;
    private readonly List<int> stacks = new List<int>();
    private float tickTimer;

    public EnemyElementStatus(ElementSettings settings)
    {
        this.settings = settings;
    }

    public Element Element { get; private set; }
    // 스택별 속성 공격력. 속성이 없으면 비어 있다.
    public IReadOnlyList<int> Stacks => stacks;
    // 파랑 속성을 잃은 뒤 남아 있는 방어력 감소와 남은 시간.
    public float LingeringDefenseReduction { get; private set; }
    public float LingeringTime { get; private set; }

    // 초록 속성을 가진 동안 이동 속도가 느려진다.
    public float MoveSpeedMultiplier => Element == Element.Green ? 1f - settings.GetMoveSlow(stacks) : 1f;

    // 노랑 속성을 가진 동안 공격 속도가 느려진다. 공격 간격을 이 값으로 나눈다.
    public float AttackSpeedMultiplier => Element == Element.Yellow ? 1f - settings.GetAttackSlow(stacks) : 1f;

    // 방어력 감소 비율(0~1). 파랑 속성을 가진 동안과, 잃은 뒤 일정 시간 동안 적용된다.
    public float DefenseReduction
    {
        get
        {
            if (Element == Element.Blue)
            {
                return settings.GetDefenseReduction(stacks);
            }

            return LingeringTime > 0f ? LingeringDefenseReduction : 0f;
        }
    }

    // 풀에서 다시 꺼내질 때 지난 생애의 속성을 지운다.
    public void Reset()
    {
        Element = Element.None;
        stacks.Clear();
        tickTimer = 0f;
        LingeringDefenseReduction = 0f;
        LingeringTime = 0f;
    }

    public HitResult Apply(ElementHit hit)
    {
        if (hit.Element == Element.None)
        {
            return default;
        }

        if (hit.Element == Element.Orange)
        {
            return Reinforce(hit.ElementAttack);
        }

        if (hit.Element == Element)
        {
            // 같은 속성은 주입이 아니다. 첫 스택만 최신 속성 공격력으로 갱신하고, 보라는 독이 중첩된다.
            stacks[0] = hit.ElementAttack;
            if (Element == Element.Purple)
            {
                AddStack(hit.ElementAttack, settings.PoisonMaxStacks);
            }

            return default;
        }

        Inject(hit.Element, hit.ElementAttack);

        switch (hit.Element)
        {
            case Element.Red:
                return new HitResult(true, hit.ElementAttack, 0f);
            case Element.Gray:
                return new HitResult(true, 0, 1f);
            default:
                return new HitResult(true, 0, 0f);
        }
    }

    // 감전, 독 대미지를 틱 간격마다 돌려준다. 틱이 아니면 0이다.
    public int Tick(float deltaTime)
    {
        if (LingeringTime > 0f)
        {
            LingeringTime -= deltaTime;
        }

        if (Element != Element.Yellow && Element != Element.Purple)
        {
            return 0;
        }

        tickTimer -= deltaTime;
        int damage = 0;
        while (tickTimer <= 0f)
        {
            tickTimer += settings.TickInterval;
            damage += Element == Element.Yellow ? settings.GetShockDamage(stacks) : settings.GetPoisonDamage(stacks);
        }

        return damage;
    }

    private void Inject(Element element, int elementAttack)
    {
        // 파랑을 잃으면 방어력 감소가 잠시 남는다. 다시 파랑이 되면 남은 감소는 더하지 않고 새 값으로 바뀐다.
        if (Element == Element.Blue)
        {
            LingeringDefenseReduction = settings.GetDefenseReduction(stacks);
            LingeringTime = settings.DefenseReductionLinger;
        }
        else if (element == Element.Blue)
        {
            LingeringTime = 0f;
        }

        Element = element;
        stacks.Clear();
        stacks.Add(elementAttack);
        // 주입된 순간이 아니라 한 틱 뒤부터 감전, 독 대미지를 준다.
        tickTimer = settings.TickInterval;
    }

    // 주황 타격. 주입된 속성의 효과를 (주입된 속성 공격력 + 주황 속성 공격력)의 세기로 한 번 더 발생시킨다.
    private HitResult Reinforce(int orangeElementAttack)
    {
        if (Element == Element.None)
        {
            return default;
        }

        int elementAttack = stacks[0] + orangeElementAttack;
        switch (Element)
        {
            case Element.Red:
                return new HitResult(false, elementAttack, 0f);
            case Element.Yellow:
            case Element.Green:
            case Element.Blue:
                AddStack(elementAttack, 1 + settings.OrangeExtraStacks);
                return default;
            case Element.Purple:
                AddStack(elementAttack, settings.PoisonMaxStacks);
                return default;
            case Element.Gray:
                return new HitResult(false, 0, settings.OrangeKnockbackMultiplier);
            default:
                return default;
        }
    }

    private void AddStack(int elementAttack, int maxStacks)
    {
        if (stacks.Count < maxStacks)
        {
            stacks.Add(elementAttack);
        }
    }
}