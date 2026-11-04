using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// SkillManager가 가진 스탯과 스킬(속성별) 상태를 텍스트로 보여준다. 블록 배치 결과를 테스트 중에 바로 확인하는 용도이다.
// 인벤토리 패널이 꺼졌다 켜지면 OnEnable에서 최신 상태로 다시 그린다.
public class SkillStatusView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private readonly StringBuilder builder = new StringBuilder();

    private void OnEnable()
    {
        SkillManager.Changed += Refresh;
        Refresh(SkillManager.Instance);
    }

    private void OnDisable()
    {
        SkillManager.Changed -= Refresh;
    }

    private void Refresh(SkillManager skillManager)
    {
        if (text == null)
        {
            return;
        }

        if (skillManager == null)
        {
            text.text = "SkillManager가 씬에 없습니다.";
            return;
        }

        builder.Clear();
        AppendStats(skillManager.Stats);
        builder.AppendLine();
        AppendSkills(skillManager.Skills);

        text.text = builder.ToString();
    }

    private void AppendStats(BlockStatTotals stats)
    {
        builder.AppendLine("<b>[스탯]</b>");

        var lines = new List<string>();
        AddStat(lines, stats, StatType.Hp, "체력", false);
        AddStat(lines, stats, StatType.Defense, "방어력", false);
        AddStat(lines, stats, StatType.HealIncrease, "회복 증가", true);
        AddStat(lines, stats, StatType.Attack, "공격력", false);

        for (Element element = Element.Red; element <= Element.Gray; element++)
        {
            AddStat(lines, stats, StatType.ElementAttack_1 + (element - Element.Red), $"{ColorElement(element)} 속성 공격력", false);
        }

        AddStat(lines, stats, StatType.DamageIncrease, "대미지 증가", true);

        // 쿨타임 감소는 곱 적용이므로 합계 대신 실제로 줄어드는 비율을 보여준다.
        if (stats.Get(StatType.CooldownReduction) > 0)
        {
            float reduction = (1f - stats.ApplyCooldown(1f)) * 100f;
            lines.Add($"쿨타임 감소 -{reduction:0.#}%");
        }

        AddStat(lines, stats, StatType.SkillDurationIncrease, "스킬 지속 시간 증가", true);
        AddStat(lines, stats, StatType.SkillRangeIncrease, "스킬 범위 증가", true);

        AppendLines(lines);
    }

    private void AppendSkills(IReadOnlyList<SkillManager.SkillState> skills)
    {
        builder.AppendLine("<b>[스킬]</b>");

        var lines = new List<string>();
        foreach (var skill in skills)
        {
            lines.Add($"{GetSkillName(skill.Skill)} - {ColorElement(skill.Element)} : 블록 {skill.BlockCount}개, 점수 {skill.Score}");
        }

        AppendLines(lines);
    }

    private static void AddStat(List<string> lines, BlockStatTotals stats, StatType type, string label, bool percent)
    {
        int value = stats.Get(type);
        if (value != 0)
        {
            lines.Add(percent ? $"{label} +{value}%" : $"{label} +{value}");
        }
    }

    private void AppendLines(List<string> lines)
    {
        if (lines.Count == 0)
        {
            builder.AppendLine("없음");
            return;
        }

        foreach (var line in lines)
        {
            builder.AppendLine(line);
        }
    }

    // 속성 이름을 블록 색으로 칠한다.
    private static string ColorElement(Element element)
    {
        string name = GetElementName(element);
        if (BlockManager.Instance == null)
        {
            return name;
        }

        string color = ColorUtility.ToHtmlStringRGB(BlockManager.Instance.GetColorForCore((int)element));
        return $"<color=#{color}>{name}</color>";
    }

    private static string GetSkillName(SkillType skill)
    {
        switch (skill)
        {
            case SkillType.Bullet: return "탄환";
            case SkillType.AreaBullet: return "광역 탄환";
            case SkillType.Support: return "지원";
            default: return "없음";
        }
    }

    private static string GetElementName(Element element)
    {
        switch (element)
        {
            case Element.Red: return "빨강(폭발)";
            case Element.Orange: return "주황(강화)";
            case Element.Yellow: return "노랑(감전)";
            case Element.Green: return "초록(둔화)";
            case Element.Blue: return "파랑(방어력 하락)";
            case Element.Purple: return "보라(독)";
            case Element.Gray: return "회색(넉백)";
            default: return "없음";
        }
    }
}