// 공격 스킬이 적에게 주는 한 번의 타격.
// 타격 대미지는 (공격력 + 속성 공격력) * (1 + 대미지 증가%)이며, 속성 효과(폭발, 감전, 둔화, 방어력 감소, 독)는
// 대미지 증가가 적용되지 않은 속성 공격력만으로 계산한다. 무속성 타격은 Element.None, 속성 공격력 0이다.
public readonly struct ElementHit
{
    public ElementHit(int damage, Element element, int elementAttack)
    {
        Damage = damage;
        Element = element;
        ElementAttack = elementAttack;
    }

    public int Damage { get; }
    public Element Element { get; }
    // 속성 공격력 스탯과 스킬 점수로 얻은 속성 공격력의 합.
    public int ElementAttack { get; }
}