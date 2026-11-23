using System;
using UnityEngine;

// 스탯 블록(core 0)이 가지는 스탯 종류.
// 수치는 저장하지 않는다. 블록 점수 × BlockStatData의 점수 1당 수치로 BlockStatTotals가 계산한다.
[Serializable]
public struct BlockStat
{
    [SerializeField] private StatType type;

    // 스탯이 없는 속성 블록(core 1~7)이 가지는 값.
    public static readonly BlockStat None = new BlockStat(StatType.None);

    public BlockStat(StatType type)
    {
        this.type = type;
    }

    public StatType Type => type;

    // 속성 공격력 스탯이 올리는 속성. ElementAttack_N이면 core N과 같은 속성이고, 다른 스탯은 None이다.
    public Element Element
    {
        get
        {
            if (type >= StatType.ElementAttack_1 && type <= StatType.ElementAttack_7)
            {
                return (Element)(type - StatType.ElementAttack_1 + (int)Element.Red);
            }

            return Element.None;
        }
    }
}
