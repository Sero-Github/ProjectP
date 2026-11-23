using UnityEngine;

// 스탯 블록. 📊 플레이어 스탯, 📊 스킬 스탯의 ★ 스탯 중 한 가지를 가진다. 🧱 블록 문서 2장을 따른다.
// 스탯 수치는 저장하지 않는다. 블록 점수 × BlockStatData의 점수 1당 수치로 BlockStatTotals가 계산한다.
// 드롭할 때의 스탯 종류는 BlockStatData.GetRandomStatType으로 고른다.
public class StatBlock : Block
{
    [SerializeField] private StatType stat;

    public StatType Stat => stat;

    // 속성 공격력 스탯이 올리는 속성. ElementAttack_N이면 Element N이고, 다른 스탯은 None이다.
    public Element StatElement
    {
        get
        {
            if (stat >= StatType.ElementAttack_1 && stat <= StatType.ElementAttack_7)
            {
                return (Element)(stat - StatType.ElementAttack_1 + (int)Element.Red);
            }

            return Element.None;
        }
    }

    public override BlockKind Kind => BlockKind.Stat;
    // 스탯 블록 색은 코어 색상 목록의 0번이다. 스탯별 색은 기획에서 정해지지 않았다.
    public override int ColorIndex => 0;

    public void SetStat(StatType statType)
    {
        stat = statType;
    }

    // 스탯 블록은 같은 스탯을 가진 스탯 블록끼리 병합한다.
    public override MergeMismatch GetMergeMismatch(Block material)
    {
        if (material is not StatBlock other)
        {
            return MergeMismatch.DifferentKind;
        }

        return other.stat == stat ? MergeMismatch.None : MergeMismatch.DifferentStat;
    }

    public override void CopyKindDataFrom(Block source)
    {
        if (source is StatBlock other)
        {
            stat = other.stat;
        }
    }

    // 옛 저장 파일과 맞추기 위해 core는 0으로 쓴다.
    public override void WriteKindData(BlockSaveData data)
    {
        data.core = 0;
        data.stat = stat.ToString();
    }

    public override void ReadKindData(BlockSaveData data)
    {
        stat = data.StatType;
    }
}
