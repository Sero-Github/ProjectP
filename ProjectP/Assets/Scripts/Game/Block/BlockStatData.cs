using System;
using System.Collections.Generic;
using UnityEngine;

// 스탯 블록의 스탯별 계수. 스탯 수치 = 블록 점수 × 점수 1당 수치이다.
// 계수는 📊 스킬 스탯 2장과 📊 플레이어 스탯 문서를 따른다. 속성 공격력은 3, 나머지는 1(또는 1%)이다.
// 블록에는 스탯 종류만 저장하고, 수치는 BlockStatTotals가 합산할 때 계산한다. 그래서 계수를 고치면 가지고 있는 블록에도 바로 적용된다.
// 인스펙터에서는 BlockStatDataEditor가 행 = 스탯, 열 = 점수 1당 수치인 표로 그린다.
[CreateAssetMenu(fileName = "BlockStatData", menuName = "ProjectP/Block/Block Stat Data")]
public class BlockStatData : ScriptableObject
{
    [Serializable]
    private class StatRow
    {
        public StatType stat;
        [Min(0)] public int valuePerScore;
    }

    // BlockStatDataEditor가 SerializedProperty로 찾을 필드 이름.
    public const string RowsField = nameof(valuesPerScore);
    public const string RowStatField = nameof(StatRow.stat);
    public const string RowValuePerScoreField = nameof(StatRow.valuePerScore);

    // 기획 기본 계수. 에셋에 행이 없을 때도 이 값으로 계산한다.
    public const int DefaultElementAttackPerScore = 3;
    public const int DefaultValuePerScore = 1;

    // 예전의 블록 번호별 점수 표(rows)와 다른 이름을 쓴다. 같은 이름이면 옛 행이 계수 0으로 읽혀 모든 스탯이 0이 된다.
    [Tooltip("스탯별 점수 1당 수치. 행은 StatType 순서로 자동 관리됩니다.")]
    [SerializeField] private List<StatRow> valuesPerScore = new List<StatRow>();

    // 무작위로 고를 스탯 종류. None은 제외한다.
    private static readonly StatType[] StatTypes = Array.FindAll((StatType[])Enum.GetValues(typeof(StatType)), type => type != StatType.None);

    // 스탯 종류를 같은 확률로 고른다. 속성 공격력은 속성별로 각각 한 종류이다.
    public static StatType GetRandomStatType()
    {
        return StatTypes[UnityEngine.Random.Range(0, StatTypes.Length)];
    }

    public static int GetDefaultValuePerScore(StatType stat)
    {
        if (stat == StatType.None)
        {
            return 0;
        }

        return stat >= StatType.ElementAttack_1 && stat <= StatType.ElementAttack_7 ? DefaultElementAttackPerScore : DefaultValuePerScore;
    }

    // data가 없으면 기본 계수로 계산한다.
    public static int GetValueOrDefault(BlockStatData data, StatType stat, int score)
    {
        return data != null ? data.GetValue(stat, score) : Mathf.Max(0, score) * GetDefaultValuePerScore(stat);
    }

    // 에셋에 행이 없으면 기본 계수를 돌려준다.
    public int GetValuePerScore(StatType stat)
    {
        if (stat == StatType.None)
        {
            return 0;
        }

        foreach (var row in valuesPerScore)
        {
            if (row != null && row.stat == stat)
            {
                return Mathf.Max(0, row.valuePerScore);
            }
        }

        return GetDefaultValuePerScore(stat);
    }

    // 스탯 수치 = 블록 점수 × 점수 1당 수치.
    public int GetValue(StatType stat, int score)
    {
        return Mathf.Max(0, score) * GetValuePerScore(stat);
    }

    // 행을 StatType 순서(None 제외)로 맞춘다. 새로 생긴 행은 기본 계수로 채운다.
    private void OnValidate()
    {
        var synced = new List<StatRow>(StatTypes.Length);
        foreach (var stat in StatTypes)
        {
            StatRow row = valuesPerScore.Find(r => r != null && r.stat == stat)
                ?? new StatRow { stat = stat, valuePerScore = GetDefaultValuePerScore(stat) };
            synced.Add(row);
        }

        valuesPerScore = synced;
    }
}
