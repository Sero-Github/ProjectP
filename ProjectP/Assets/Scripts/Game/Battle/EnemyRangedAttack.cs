using UnityEngine;

// 원거리 공격형(삼각형) 몬스터가 일정 주기로 플레이어를 향해 탄환을 쏜다.
// 근접 공격은 Enemy가 그대로 처리하므로 이 컴포넌트는 원거리 공격만 담당한다.
[RequireComponent(typeof(Enemy))]
public class EnemyRangedAttack : MonoBehaviour
{
    [SerializeField] private EnemyBullet bulletPrefab;

    // 발사 주기(Cooldown)와 탄속(MoveSpeed)만 사용합니다. 대미지는 몬스터 공격력을 씁니다.
    [Tooltip("탄환 데이터. Cooldown과 MoveSpeed를 사용하며, 생성 직후에는 한 주기를 기다린 뒤 첫 발을 쏩니다.")]
    [SerializeField] private BulletData data;

    private Enemy enemy;
    private float timer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        timer = data != null ? data.Cooldown : 0f;
    }

    private void Update()
    {
        if (data == null || bulletPrefab == null || enemy.IsDead)
        {
            return;
        }

        Player player = Player.Instance;
        if (player == null || player.IsDead)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        timer = data.Cooldown;
        Fire(player);
    }

    private void Fire(Player player)
    {
        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)player.transform.position - origin).normalized;
        if (direction == Vector2.zero)
        {
            return;
        }

        // 스포너의 최대 몬스터 수가 자식 개수로 세어지므로 몬스터 부모 아래에 두지 않습니다.
        EnemyBullet bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
        bullet.Launch(direction, data.MoveSpeed, enemy.Attack);
    }
}