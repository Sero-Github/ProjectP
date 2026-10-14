using System;
using System.Collections.Generic;
using UnityEngine;

// 스탯 블록의 블록 종류별 스탯 점수. 🧱 블록 문서의 (스탯 번호, 블록 번호, 점수) 표를 따른다.
// 스탯 수치는 점수와 같다. 드랍되는 블록은 1성 기준 점수를 받는다.
[CreateAssetMenu(fileName = "BlockStatData", menuName = "ProjectP/Block/Block Stat Data")]
public class BlockStatData : ScriptableObject
{
    [Serializable]
    private struct StatScore
    {
        public StatType stat;
        [Tooltip("블록 번호. 🧱 블록 문서의 블록 종류 순서를 따릅니다.")]
        public int blockId;
        [Min(0)]
        public int score;
    }

    [Tooltip("스탯과 블록 번호별 점수. 없는 조합은 0점입니다.")]
    [SerializeField] private List<StatScore> scores = new List<StatScore>();

    // 무작위로 고를 스탯 종류. None은 제외한다.
    private static readonly StatType[] StatTypes = Array.FindAll((StatType[])Enum.GetValues(typeof(StatType)), type => type != StatType.None);

    public int GetScore(StatType stat, int blockId)
    {
        foreach (var entry in scores)
        {
            if (entry.stat == stat && entry.blockId == blockId)
            {
                return entry.score;
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
}