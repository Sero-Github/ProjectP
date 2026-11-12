using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 라운드마다 정해진 소환 순서(SpawnSequence)대로, 라운드 시간 동안 같은 간격으로 몬스터를 소환한다.
// 어떤 라운드를 언제 시작할지는 RoundManager가 정한다.
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("몬스터 유형별 프리팹, 소환 위치 여백 등 몬스터 소환 설정")]
    [SerializeField] private EnemySpawnSettings settings;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private Camera targetCamera;

    // 사망 처리 등에서 Enemy.Actives를 순회하며 몬스터를 없앨 때 쓰는 복사본.
    private readonly List<Enemy> despawnTargets = new List<Enemy>();
    // 몬스터 목록 순서의 유형별 풀. 사망한 몬스터는 비활성화되어 여기로 돌아오고, 다음 생성 때 재사용된다.
    private ObjectPool<Enemy>[] pools = Array.Empty<ObjectPool<Enemy>>();

    // 몬스터 목록 인덱스로 된 이번 라운드의 소환 순서
    private List<int> sequence;
    private int nextIndex;
    private float interval;
    private float timer;
    private int level = 1;

    // 이번 라운드에 소환할 몬스터가 남아 있는지
    public bool IsSpawning => sequence != null && nextIndex < sequence.Count;
    public int SpawnedCount => nextIndex;
    public int TotalCount => sequence != null ? sequence.Count : 0;
    // 소환할 몬스터 종류. 스테이지 CSV의 몬스터 열과 소환 순서의 인덱스가 이 목록을 따른다.
    public IReadOnlyList<EnemyDefinition> Enemies => settings != null ? settings.Enemies : Array.Empty<EnemyDefinition>();

    // 이번 라운드의 마지막 몬스터를 소환했을 때 발생합니다.
    public event Action SpawnFinished;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (enemyParent == null)
        {
            enemyParent = transform;
        }

        if (settings == null)
        {
            Debug.LogError($"{name}: EnemySpawnSettings가 연결되지 않았습니다.", this);
            return;
        }

        IReadOnlyList<EnemyDefinition> enemies = settings.Enemies;
        pools = new ObjectPool<Enemy>[enemies.Count];
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null || enemies[i].Prefab == null)
            {
                Debug.LogWarning($"{name}: 몬스터 목록 {i + 1}번째 항목({enemies[i]?.Id})에 프리팹이 연결되지 않았습니다.", settings);
                continue;
            }

            pools[i] = CreatePool(enemies[i].Prefab);
        }
    }

    // 비활성화된 몬스터가 있으면 꺼내 쓰고, 없을 때만 새로 만든다.
    // 위치를 옮긴 뒤 켜야 이전 위치에서 보간되지 않으므로, 꺼내고 돌려줄 때 켜고 끄는 것은 Spawn과 Enemy가 맡는다.
    private ObjectPool<Enemy> CreatePool(Enemy prefab)
    {
        ObjectPool<Enemy> pool = null;
        pool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                Enemy enemy = Instantiate(prefab, enemyParent);
                enemy.gameObject.SetActive(false);
                enemy.SetPool(pool);
                return enemy;
            },
            actionOnDestroy: enemy =>
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            },
            // Enemy가 사망할 때 한 번만 돌려주므로 중복 반환 검사(매번 풀 전체 탐색)는 끈다.
            collectionCheck: false);
        return pool;
    }

    // 소환 순서 전체를 duration초 동안 같은 간격(duration / 개수)으로 소환한다. 첫 몬스터는 첫 간격이 지난 뒤 나온다.
    public void StartRound(List<int> spawnSequence, float duration, int roundLevel)
    {
        sequence = spawnSequence ?? new List<int>();
        nextIndex = 0;
        timer = 0f;
        level = Mathf.Max(1, roundLevel);
        interval = sequence.Count > 0 ? Mathf.Max(0.0001f, duration) / sequence.Count : 0f;

        if (sequence.Count == 0)
        {
            SpawnFinished?.Invoke();
        }
    }

    // 남은 소환을 취소한다. 이미 나온 몬스터는 그대로 둔다.
    public void StopSpawning()
    {
        sequence = null;
        nextIndex = 0;
        timer = 0f;
    }

    // 살아 있는 몬스터를 모두 없앤다. 처치가 아니므로 블록, 골드를 드랍하지 않는다.
    public void DespawnAll(bool playParticles)
    {
        despawnTargets.Clear();
        despawnTargets.AddRange(Enemy.Actives);
        foreach (Enemy enemy in despawnTargets)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.Despawn(playParticles);
            }
        }

        despawnTargets.Clear();
    }

    private void Update()
    {
        if (!IsSpawning)
        {
            return;
        }

        timer += Time.deltaTime;
        // 프레임이 길어져 간격을 여러 번 넘겨도 밀린 만큼 모두 소환한다.
        while (timer >= interval && IsSpawning)
        {
            timer -= interval;
            Spawn(sequence[nextIndex]);
            nextIndex++;
        }

        if (!IsSpawning)
        {
            SpawnFinished?.Invoke();
        }
    }

    // enemyIndex는 몬스터 목록(Enemies)의 인덱스이다.
    public Enemy Spawn(int enemyIndex)
    {
        if (targetCamera == null)
        {
            return null;
        }

        ObjectPool<Enemy> pool = enemyIndex >= 0 && enemyIndex < pools.Length ? pools[enemyIndex] : null;
        if (pool == null)
        {
            string id = enemyIndex >= 0 && enemyIndex < Enemies.Count ? Enemies[enemyIndex]?.Id : enemyIndex.ToString();
            Debug.LogError($"{name}: {id} 프리팹이 연결되지 않아 소환하지 못했습니다.", this);
            return null;
        }

        Enemy enemy = pool.Get();
        enemy.transform.SetPositionAndRotation(GetSpawnPosition(), Quaternion.identity);
        enemy.Initialize(level);
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 half = GetSpawnHalfExtents(targetCamera);
        float width = half.x * 2f;
        float height = half.y * 2f;
        float t = UnityEngine.Random.Range(0f, (width + height) * 2f);

        Vector2 offset;
        if (t < width)
        {
            offset = new Vector2(t - half.x, half.y);
        }
        else if (t < width * 2f)
        {
            offset = new Vector2(t - width - half.x, -half.y);
        }
        else if (t < width * 2f + height)
        {
            offset = new Vector2(-half.x, t - width * 2f - half.y);
        }
        else
        {
            offset = new Vector2(half.x, t - width * 2f - height - half.y);
        }

        Vector3 center = targetCamera.transform.position;
        return new Vector3(center.x + offset.x, center.y + offset.y, 0f);
    }

    private Vector2 GetSpawnHalfExtents(Camera cam)
    {
        float spawnMargin = settings != null ? settings.SpawnMargin : 0f;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        return new Vector2(halfWidth + spawnMargin, halfHeight + spawnMargin);
    }

    private void OnDrawGizmos()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector2 half = GetSpawnHalfExtents(cam);
        Vector3 center = cam.transform.position;
        center.z = 0f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, new Vector3(half.x * 2f, half.y * 2f, 0f));
    }
}
