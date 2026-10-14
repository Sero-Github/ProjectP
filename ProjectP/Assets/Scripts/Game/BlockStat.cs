using System;
using UnityEngine;

// 스탯 블록(core 0)이 가지는 스탯 한 종류와 수치.
[Serializable]
public struct BlockStat
{
    [SerializeField] private StatType type;
    [SerializeField] private int value;

    // 스탯이 없는 속성 블록(core 1~7)이 가지는 값.
    public static readonly BlockStat None = new BlockStat(StatType.None, 0);

    public BlockStat(StatType type, int value)
    {
        this.type = type;
        this.value = value;
    }

    public StatType Type => type;
    public int Value => value;

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

    // 병합 시 두 블록의 능력치를 합산한다. 종류는 병합할 블록(a)을 따른다.
    public static BlockStat Merge(BlockStat a, BlockStat b)
    {
        return new BlockStat(a.type, a.value + b.value);
    }
}