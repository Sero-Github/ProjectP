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
        builder.Append(block.BlockId == 0 ? "병합 블록" : $"{block.BlockId}번 블록");
        builder.Append($" | 코어 칸 {coreCell}, {cells.Count}칸 [{string.Join(" ", cells)}]");

        if (block.HasStat)
        {
            builder.Append($" | 스탯 {block.Stat.Type} {block.Stat.Value}");
        }
        else
        {
            builder.Append($" | 스킬 {block.Skill}, 속성 {block.Element}, 스킬 점수 {block.SkillScore}");
        }

        builder.Append($" | 등급 {block.Grade}, 합 {block.MergeCount}, 남은 병합 {block.RemainingMerges}");
        builder.Append($" | 블록 점수 {block.Score} (원본 {block.BaseScore}, {block.EnhanceLevel}강)");

        // 아직 게임에 적용하지 않은 V2 규칙으로 계산한 참고값. 다음 강 점수는 강화 기능이 생기기 전 확인용이다.
        if (settings != null)
        {
            int v2Remaining = BlockScore.GetRemainingMerges(block.MergeCount, settings.MaxMergeCount);
            int nextEnhanceScore = BlockScore.Enhance(block.BaseScore, block.EnhanceLevel + 1, settings.EnhanceRatePercent);
            builder.Append($" | V2 참고: 남은 병합 {v2Remaining}, {block.EnhanceLevel + 1}강이면 {nextEnhanceScore}");
        }
    }
}
