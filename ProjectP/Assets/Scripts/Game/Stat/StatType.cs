// 블록이 가질 수 있는 스탯 종류. 수식은 📊 스탯 문서를 따른다.
// 가방에 배치된 블록의 스탯은 SkillManager가 BlockStatTotals로 합산하며, 플레이어와 탄환이 이를 참조한다.
// 스킬 지속 시간 증가, 스킬 범위 증가는 해당 스킬이 구현되기 전까지 합산만 되고 적용되지 않는다.
public enum StatType
{
    None,                   // 스탯 없음(속성 블록)
    Hp,                     // 체력
    HealIncrease,           // 회복량 증가
    Defense,                // 방어력
    Attack,                 // 공격력
    // 속성 공격력. 뒤의 번호는 Element 값(= 속성 블록의 core 번호)과 같다.
    ElementAttack_1,        // 속성 공격력 - 빨강(폭발)
    ElementAttack_2,        // 속성 공격력 - 주황(강화)
    ElementAttack_3,        // 속성 공격력 - 노랑(감전)
    ElementAttack_4,        // 속성 공격력 - 초록(둔화)
    ElementAttack_5,        // 속성 공격력 - 파랑(방어력 하락)
    ElementAttack_6,        // 속성 공격력 - 보라(독)
    ElementAttack_7,        // 속성 공격력 - 회색(넉백)
    DamageIncrease,         // 데미지 증가
    CooldownReduction,      // 쿨타임 감소
    SkillDurationIncrease,  // 스킬 지속 시간 증가
    SkillRangeIncrease,     // 스킬 범위 증가
}