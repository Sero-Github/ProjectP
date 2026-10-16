using System;
using System.Collections.Generic;
using UnityEngine;

// 가방에 배치된 블록으로 얻는 스탯, 스킬, 속성 상태를 한 곳에서 가진다.
// 블록 조합이 바뀔 때마다 상태를 새로 만들며, 플레이어와 공격 스킬은 이 값을 참조만 한다.
// 인벤토리 패널은 꺼져 있을 수 있으므로 항상 켜진 오브젝트에 붙인다.
public class SkillManager : MonoBehaviour
{
    // 스킬 종류와 속성 조합 하나에 배치된 블록 수와 스킬 점수 합계.
    public class SkillState
    {
        public SkillState(SkillType skill, Element element)
        {
            Skill = skill;
            Element = element;
        }

        public SkillType Skill { get; }
        public Element Element { get; }
        public int BlockCount { get; private set; }
        public int Score { get; private set; }

        public void Add(int score)
        {
            BlockCount++;
            Score += score;
        }
    }

    public static SkillManager Instance { get; private set; }

    // 상태가 다시 만들어질 때 호출된다. Instance가 생기기 전에도 구독할 수 있도록 정적 이벤트로 둔다.
    public static event Action<SkillManager> Changed;

    private readonly List<SkillState> skills = new List<SkillState>();
    private readonly List<Element> elements = new List<Element>();

    // 스탯 블록의 스탯 합계. 대미지, 쿨타임 공식도 여기서 계산한다.
    public BlockStatTotals Stats { get; private set; } = BlockStatTotals.Empty;
    // 스킬 종류, 속성 순으로 정렬되어 있다. 탄환 외의 스킬은 집계만 되고 아직 동작하지 않는다.
    public IReadOnlyList<SkillState> Skills => skills;
    // 배치된 속성 블록의 속성 목록. 중복 없이 Element 순으로 정렬되어 있다.
    public IReadOnlyList<Element> Elements => elements;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Backpack.BlocksChanged += Rebuild;
    }

    private void OnDisable()
    {
        Backpack.BlocksChanged -= Rebuild;
    }

    private void Start()
    {
        // 구독 전에 가방이 채워진 경우를 반영한다.
        if (Backpack.Instance != null)
        {
            Rebuild(Backpack.Instance);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetSkillScore(SkillType skill, Element element)
    {
        SkillState state = FindSkill(skill, element);
        return state != null ? state.Score : 0;
    }

    private void Rebuild(Backpack backpack)
    {
        Stats = new BlockStatTotals(backpack.PlacedBlocks.Keys);

        skills.Clear();
        elements.Clear();

        foreach (var block in backpack.PlacedBlocks.Keys)
        {
            if (block == null || !block.HasSkill)
            {
                continue;
            }

            if (!elements.Contains(block.Element))
            {
                elements.Add(block.Element);
            }

            if (block.Skill == SkillType.None)
            {
                continue;
            }

            SkillState state = FindSkill(block.Skill, block.Element);
            if (state == null)
            {
                state = new SkillState(block.Skill, block.Element);
                skills.Add(state);
            }
            state.Add(block.SkillScore);
        }

        skills.Sort((a, b) => a.Skill != b.Skill ? a.Skill.CompareTo(b.Skill) : a.Element.CompareTo(b.Element));
        elements.Sort();

        Changed?.Invoke(this);
    }

    private SkillState FindSkill(SkillType skill, Element element)
    {
        foreach (var state in skills)
        {
            if (state.Skill == skill && state.Element == element)
            {
                return state;
            }
        }

        return null;
    }
}