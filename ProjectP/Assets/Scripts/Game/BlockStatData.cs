using System;
using System.Collections.Generic;
using UnityEngine;

// 스탯 블록의 블록 종류별 스탯 점수. 🧱 블록 문서의 (스탯 번호, 블록 번호, 점수) 표를 따른다.
// 스탯 수치는 점수와 같다. 드랍되는 블록은 1성 기준 점수를 받는다.
// 인스펙터에서는 BlockStatDataEditor가 행 = 스탯, 열 = 블록 번호인 표로 그린다.
[CreateAssetMenu(fileName = "BlockStatData", menuName = "ProjectP/Block/Block Stat Data")]
public class BlockStatData : ScriptableObject
{
    [Serializable]
    private class StatRow
    {
        public StatType stat;
        // 인덱스가 블록 번호 - 1이다.
        public int[] scores = Array.Empty<int>();
    }

    // BlockStatDataEditor가 SerializedProperty로 찾을 필드 이름.
    public const string BlockCountField = nameof(blockCount);
    public const string RowsField = nameof(rows);
    public const string RowStatField = nameof(StatRow.stat);
    public const string RowScoresField = nameof(StatRow.scores);

    [Tooltip("블록 종류 수. 🧱 블록 문서의 블록 종류 순서를 따릅니다.")]
    [SerializeField, Min(1)] private int blockCount = 9;
    [Tooltip("스탯별 블록 번호 점수. 행은 StatType 순서로 자동 관리됩니다.")]
    [SerializeField] private List<StatRow> rows = new List<StatRow>();

    // 무작위로 고를 스탯 종류. None은 제외한다.
    private static readonly StatType[] StatTypes = Array.FindAll((StatType[])Enum.GetValues(typeof(StatType)), type => type != StatType.None);

    public int GetScore(StatType stat, int blockId)
    {
        foreach (var row in rows)
        {
            if (row.stat == stat)
            {
                int index = blockId - 1;
                return row.scores != null && index >= 0 && index < row.scores.Length ? row.scores[index] : 0;
            }
        }

        return 0;
    }

    // 스탯 종류를 같은 확률로 고른다. 속성 공격력은 속성별로 각각 한 종류이다.
    public BlockStat CreateRandomStat(int blockId)
    {
        StatType stat = StatTypes[UnityEngine.Random.Range(0, StatTypes.Length)];
        return new BlockStat(stat, GetScore(stat, blockId));
    }

    // 행을 StatType 순서(None 제외)로 맞추고, 점수 배열 길이를 블록 종류 수에 맞춘다.
    private void OnValidate()
    {
        var synced = new List<StatRow>(StatTypes.Length);
        foreach (var stat in StatTypes)
        {
            StatRow row = rows.Find(r => r != null && r.stat == stat) ?? new StatRow { stat = stat };
            row.scores ??= Array.Empty<int>();
            if (row.scores.Length != blockCount)
            {
                Array.Resize(ref row.scores, blockCount);
            }
            synced.Add(row);
        }

        rows = synced;
    }
}
