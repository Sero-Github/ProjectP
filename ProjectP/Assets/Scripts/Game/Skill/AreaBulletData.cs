using UnityEngine;

// 광역 탄환 수치. ⚔️ 스킬 문서 3-2를 따른다.
// 탄환과 외형은 같지만 적에게 닿아 소멸할 때 원형의 공격 지대를 만들고, 대미지는 공격 지대가 준다.
[CreateAssetMenu(fileName = "AreaBulletData", menuName = "ProjectP/Skill/Area Bullet Data")]
public class AreaBulletData : ScriptableObject
{
    [Tooltip("재사용 대기 시간(초). 탄환이 사출된 시점부터 흐릅니다.")]
    [Min(0.05f)]
    [SerializeField] private float cooldown = 7f;

    [Tooltip("탄환 이동 속도(유닛/초)")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("공격 지대 유지 시간(초). 생성된 시점부터 흐르며, 끝나면 작아지며 사라집니다.")]
    [Min(0f)]
    [SerializeField] private float zoneDuration = 2f;

    [Tooltip("공격 지대 지름(유닛)")]
    [Min(0f)]
    [SerializeField] private float zoneDiameter = 2f;

    [Tooltip("공격 지대가 작은 원에서 지름까지 커지는 시간(초). 유지 시간에 포함됩니다.")]
    [Min(0f)]
    [SerializeField] private float zoneGrowTime = 0.2f;

    [Tooltip("유지 시간이 끝난 뒤 공격 지대가 작아지며 사라지는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float zoneShrinkTime = 0.2f;

    [Tooltip("기본 공격력. 가방 블록의 공격력과 대미지 증가가 더해져 공격 지대 안의 적에게 대미지를 줍니다.")]
    [Min(0)]
    [SerializeField] private int attack = 10;

    public float Cooldown => cooldown;
    public float MoveSpeed => moveSpeed;
    public float ZoneDuration => zoneDuration;
    public float ZoneDiameter => zoneDiameter;
    public float ZoneGrowTime => zoneGrowTime;
    public float ZoneShrinkTime => zoneShrinkTime;
    public int Attack => attack;
}
