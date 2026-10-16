// 스킬 속성. ⚔️ 스킬 문서의 속성 종류를 따른다.
// 값은 블록의 core 번호와 같다. core 0은 속성이 없는 스탯 블록이다.
public enum Element
{
    None = 0,
    Red = 1,     // 빨강(폭발)
    Orange = 2,  // 주황(강화)
    Yellow = 3,  // 노랑(감전)
    Green = 4,   // 초록(둔화)
    Blue = 5,    // 파랑(방어력 하락)
    Purple = 6,  // 보라(독)
    Gray = 7,    // 회색(넉백)
}