using UnityEngine;

// 블록 점수 규칙. 🧱 블록 4장, 🧱 블록 강화 4장을 따른다.
// 칸당 점수, 최대 합, 1강당 증가율은 BlockSettings 에셋 값을 받아 쓴다.
public static class BlockScore
{
    // 원본 블록 점수 = 칸 수 × 칸당 점수. 1칸 5점, 3칸 15점이다.
    // 병합 결과 점수는 메인 블록 점수에 소모 블록 점수를 그대로 더한다.
    public static int FromCells(int cellCount, int scorePerCell)
    {
        return Mathf.Max(0, cellCount) * Mathf.Max(0, scorePerCell);
    }

    // 강화 점수 = 원본 점수 × (1 + 1강당 증가율 × 강). 원본 점수는 강화하지 않은(3합 0강) 상태의 점수다.
    // 원본 100점은 1강 120, 2강 140, 5강 200이다. 소수점 아래는 버린다.
    public static int Enhance(int baseScore, int enhanceLevel, int ratePercentPerLevel)
    {
        baseScore = Mathf.Max(0, baseScore);
        int bonusPercent = Mathf.Max(0, enhanceLevel) * Mathf.Max(0, ratePercentPerLevel);
        return baseScore + baseScore * bonusPercent / 100;
    }

    // 남은 병합 횟수 = 최대 합 − 지금까지 병합한 횟수. 0보다 작아지지 않는다.
    public static int GetRemainingMerges(int mergeCount, int maxMergeCount)
    {
        return Mathf.Max(0, maxMergeCount - Mathf.Max(0, mergeCount));
    }
}
