using UnityEngine;

// 지원 수치. ⚔️ 스킬 문서 3-4를 따른다.
// 지원 크리쳐는 가까운 적에게 돌진해 지나간 뒤 플레이어에게 돌아오며, 이동한 궤적이 속성을 가진 공격 지대가 된다.
// 크리쳐 몸체에는 판정이 없고 대미지는 궤적만 준다.
[CreateAssetMenu(fileName = "SupportData", menuName = "ProjectP/Skill/Support Data")]
public class SupportData : ScriptableObject
{
    [Tooltip("재사용 대기 시간(초). 크리쳐마다 따로 흐르며, 출격 후 플레이어에게 돌아온 시점부터 흐릅니다.")]
    [Min(0.05f)]
    [SerializeField] private float cooldown = 10f;

    [Tooltip("크리쳐 이동 속도(유닛/초)")]
    [Min(0.01f)]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("궤적 유지 시간(초)")]
    [Min(0f)]
    [SerializeField] private float trailDuration = 5f;

    [Tooltip("기본 크리쳐 수. 스킬 점수 보너스로 늘어납니다.")]
    [Min(1)]
    [SerializeField] private int creatureCount = 1;

    [Tooltip("기본 공격력. 가방 블록의 공격력과 대미지 증가가 더해져 궤적 위의 적에게 대미지를 줍니다.")]
    [Min(0)]
    [SerializeField] private int attack = 10;

    [Header("임시값 (문서에 수치 없음)")]
    [Tooltip("궤적 폭(유닛)")]
    [Min(0.01f)]
    [SerializeField] private float trailWidth = 0.5f;

    [Tooltip("궤적 점을 남기는 이동 거리 간격(유닛)")]
    [Min(0.01f)]
    [SerializeField] private float trailSpacing = 0.15f;

    [Tooltip("적의 위치에 닿은 뒤 몬스터 크기만큼 지나가는 동안의 속도 배율")]
    [Range(0.05f, 1f)]
    [SerializeField] private float passSpeedMultiplier = 0.4f;

    [Tooltip("지나간 뒤 플레이어에게 돌아가기 전에 멈추는 시간(초)")]
    [Min(0f)]
    [SerializeField] private float returnDelay = 0.2f;

    [Tooltip("크리쳐 지름(유닛)")]
    [Min(0.01f)]
    [SerializeField] private float creatureDiameter = 0.3f;

    public float Cooldown => cooldown;
    public float MoveSpeed => moveSpeed;
    public float TrailDuration => trailDuration;
    public int CreatureCount => creatureCount;
    public int Attack => attack;
    public float TrailWidth => trailWidth;
    public float TrailSpacing => trailSpacing;
    public float PassSpeedMultiplier => passSpeedMultiplier;
    public float ReturnDelay => returnDelay;
    public float CreatureDiameter => creatureDiameter;
}
