using UnityEngine;

// 공격 스킬의 스킬 점수 보너스 횟수. 📊 스탯 문서 4장을 따른다.
// 1점당 속성 공격력이 1 오르고, 10점마다 스킬별 보너스 세 가지를 순서대로 반복해 얻는다.
// 여기서는 각 보너스를 몇 번 얻었는지만 계산하며, 보너스 수치는 스킬마다 곱해 쓴다.
public readonly struct SkillScore
{
    private const int ScorePerBonus = 10;

    // 50점이면 보너스 5번이므로 첫 번째 2번, 두 번째 2번, 세 번째 1번이다.
    public SkillScore(int score)
    {
        score = Mathf.Max(0, score);
        int bonusCount = score / ScorePerBonus;

        ElementAttack = score;
        FirstCount = (bonusCount + 2) / 3;
        SecondCount = (bonusCount + 1) / 3;
        ThirdCount = bonusCount / 3;
    }

    // 1점당 1씩 오르는 속성 공격력. 보너스로 얻는 속성 공격력은 포함하지 않는다.
    public int ElementAttack { get; }
    public int FirstCount { get; }
    public int SecondCount { get; }
    public int ThirdCount { get; }
}
