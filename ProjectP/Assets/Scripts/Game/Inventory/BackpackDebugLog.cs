using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 임시: 블록 데이터 V2 작업 중 확인용. 가방의 블록 배치가 바뀔 때마다 가방에 있는 모든 블록 정보를 콘솔에 남긴다.
// 씬에 붙이지 않고 플레이 시작 때 스스로 구독한다. 확인이 끝나면 이 파일을 지운다.
public static class BackpackDebugLog
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        // 도메인 리로드를 끈 경우에도 중복 구독되지 않게 먼저 뺀다.
        Backpack.BlocksChanged -= Log;
        Backpack.BlocksChanged += Log;
    }

    private static void Log(Backpack backpack)
    {
        BlockSettings settings = BlockManager.Instance != null ? BlockManager.Instance.Settings : null;

        // 코어 칸의 행, 열 순서로 정렬해 매번 같은 순서로 찍는다.
        var blocks = new List<KeyValuePair<Block, Vector2Int>>(backpack.PlacedBlocks);
        blocks.Sort((a, b) => a.Value.y != b.Value.y ? a.Value.y.CompareTo(b.Value.y) : a.Value.x.CompareTo(b.Value.x));

        var builder = new StringBuilder();
        builder.Append($"[가방] 블록 {blocks.Count}개 (Frame {Time.frameCount})");

        for (int i = 0; i < blocks.Count; i++)
        {
            Block block = blocks[i].Key;
            builder.AppendLine();
            builder.Append($"  {i + 1}. ");
            AppendBlock(builder, block, blocks[i].Value, backpack.GetOccupiedCells(block), settings);
        }

        Debug.Log(builder.ToString(), backpack);
    }

    private static void AppendBlock(StringBuilder builder, Block block, Vector2Int coreCell, List<Vector2Int> cells, BlockSettings settings)
    {
        builder.Append($"{block.GetType().Name} ");
        builder.Append(block.BlockId == 0 ? "병합 블록" : $"{block.BlockId}번 블록");
        builder.Append($" | 코어 칸 {coreCell}, {cells.Count}칸 [{string.Join(" ", cells)}]");

        switch (block)
        {
            case StatBlock statBlock:
            {
                BlockStatData statData = settings != null ? settings.StatData : null;
                int statValue = BlockStatData.GetValueOrDefault(statData, statBlock.Stat, statBlock.Score);
                int valuePerScore = statData != null ? statData.GetValuePerScore(statBlock.Stat) : BlockStatData.GetDefaultValuePerScore(statBlock.Stat);
                builder.Append($" | 스탯 {statBlock.Stat} {statValue} (점수 {statBlock.Score} × {valuePerScore})");
                break;
            }
            case SkillBlock skillBlock:
            {
                // 블록 하나만의 보너스 횟수다. 실제 보너스는 같은 스킬·속성 블록 점수를 모두 더해 계산한다.
                var skillScore = new SkillScore(skillBlock.Score);
                int bonusCount = skillScore.ElementAttackBonusCount + skillScore.CooldownBonusCount + skillScore.SkillBonusCount;
                builder.Append($" | 스킬 {skillBlock.Skill}, 속성 {skillBlock.Element}, 타격 {skillBlock.TargetPriority}, 스킬 점수 {skillScore.score}, 보너스 {bonusCount}회");
                break;
            }
            case ActiveSkillBlock activeSkillBlock:
                builder.Append($" | 액티브 스킬 {activeSkillBlock.ActiveSkill}");
                break;
        }

        int remainingMerges = BlockManager.Instance != null ? BlockManager.Instance.GetRemainingMerges(block) : 0;
        builder.Append($" | {block.MergeCount}합, 남은 병합 {remainingMerges}");
        builder.Append($" | 블록 점수 {block.Score} (원본 {block.BaseScore}, {block.EnhanceLevel}강)");

        // 강화 기능이 생기기 전 확인용 참고값.
        if (settings != null)
        {
            int nextEnhanceScore = BlockScore.Enhance(block.BaseScore, block.EnhanceLevel + 1, settings.EnhanceRatePercent);
            builder.Append($" | 참고: {block.EnhanceLevel + 1}강이면 {nextEnhanceScore}");
        }
    }
}
