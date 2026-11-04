// 속성 블록이 가지는 공격 스킬 종류. ⚔️ 스킬 문서의 공격 스킬을 따른다.
// 지형은 기획상 보류되어 제외한다. 탄환, 광역 탄환은 BulletShooter가, 지원은 SupportShooter가 맡는다.
public enum SkillType
{
    None,        // 스킬 없음(스탯 블록)
    Bullet,      // 탄환
    AreaBullet,  // 광역 탄환
    Support,     // 지원
}