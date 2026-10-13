using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "ProjectP/Skill/Bullet Data")]
public class BulletData : ScriptableObject
{
    [Tooltip("재사용 대기 시간(초). 탄환이 사출된 시점부터 흐릅니다.")]
    [Min(0.05f)]
    [SerializeField] private float cooldown = 1f;

    [Tooltip("탄환 이동 속도(유닛/초)")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("동시 발사 개수. 가까운 적부터 중복 없이 한 발씩 타게팅합니다.")]
    [Min(1)]
    [SerializeField] private int projectileCount = 1;

    [Tooltip("관통력. N이면 N마리의 적을 관통하고, N+1번째 적에 맞으면 소멸합니다.")]
    [Min(0)]
    [SerializeField] private int pierce = 0;

    [Tooltip("공격력. 적중한 적에게 고정 값으로 대미지를 줍니다.")]
    [Min(0)]
    [SerializeField] private int attack = 10;

    public float Cooldown => cooldown;
    public float MoveSpeed => moveSpeed;
    public int ProjectileCount => projectileCount;
    public int Pierce => pierce;
    public int Attack => attack;
}