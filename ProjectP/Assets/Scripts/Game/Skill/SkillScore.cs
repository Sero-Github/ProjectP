using UnityEngine;

// 공격 스킬의 스킬 점수 보너스 횟수. 📊 스킬 점수 문서 2장을 따른다.
// 스킬 점수는 같은 스킬·속성 블록의 블록 점수 합이다(SkillManager.SkillState.Score).
// 1점당 속성 공격력이 1 오르고, 10점마다 스킬별 보너스 세 가지를 순서대로 반복해 얻는다.
// 여기서는 각 보너스를 몇 번 얻었는지만 계산하며, 보너스 수치는 스킬마다 곱해 쓴다.
public readonly struct SkillScore
{
    private const int ScorePerBonus = 10;

    // 스킬 점수. 1점당 1씩 오르는 속성 공격력으로 그대로 쓴다. 보너스로 얻는 속성 공격력은 포함하지 않는다.
    public readonly int score;

    // 50점이면 보너스 5번이므로 속성 공격력 보너스 2번, 쿨타임 보너스 2번, 스킬 고유 보너스 1번이다.
    public SkillScore(int skillScore)
    {
        score = Mathf.Max(0, skillScore);
        int bonusCount = score / ScorePerBonus;

        ElementAttackBonusCount = (bonusCount + 2) / 3;
        CooldownBonusCount = (bonusCount + 1) / 3;
        SkillBonusCount = bonusCount / 3;
    }

    // 첫 번째 보너스(속성 공격력 증가)를 얻은 횟수. 세 스킬 모두 같은 종류이다.
    public int ElementAttackBonusCount { get; }
    // 두 번째 보너스(쿨타임 감소 5%)를 얻은 횟수. 세 스킬 모두 같은 종류이다.
    public int CooldownBonusCount { get; }
    // 세 번째 보너스(스킬마다 다름)를 얻은 횟수. 탄환은 탄환 수, 광역 탄환은 지속 시간·범위, 지원은 크리쳐 수가 오른다.
    public int SkillBonusCount { get; }
}