using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private class SpawnEntry
    {
        [SerializeField] private Enemy prefab;

        [Tooltip("스폰 가중치. 실제 확률 = 가중치 / 스폰 가능한 항목의 가중치 합. 합을 100으로 맞추면 그대로 %가 됩니다.")]
        [Min(0f)]
        [SerializeField] private float weight = 1f;

        [Tooltip("한 스테이지에서 생성될 수 있는 최대 개수. 0이면 제한이 없습니다. 다 채우면 남은 항목끼리 확률을 나눕니다.")]
        [Min(0)]
        [SerializeField] private int maxPerStage = 0;

        public Enemy Prefab => prefab;
        public float Weight => weight;
        public int SpawnedThisStage { get; set; }
        // 이 프리팹으로 만든 몬스터 풀. 사망한 몬스터는 비활성화되어 여기로 돌아오고, 다음 생성 때 재사용된다.
        public ObjectPool<Enemy> Pool { get; set; }

        public bool CanSpawn => prefab != null && weight > 0f && (maxPerStage == 0 || SpawnedThisStage < maxPerStage);
    }

    [SerializeField] private List<SpawnEntry> entries = new List<SpawnEntry>();
    [SerializeField] private Transform enemyParent;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnMargin = 1f;
    [SerializeField] private int maxEnemies = 0;

    // 라운드 진행이 생기기 전까지는 인스펙터 값으로 레벨을 정합니다.
    [Header("스테이지")]
    [Min(1)]
    [SerializeField] private int stage = 1;

    [Min(1)]
    [SerializeField] private int round = 1;

    [Min(1)]
    [SerializeField] private int roundsPerStage = 10;

    private float timer;

    // 1스테이지 10라운드는 10, 2스테이지 1라운드는 11입니다.
    public int Level => (stage - 1) * roundsPerStage + round;

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

        foreach (SpawnEntry entry in entries)
        {
            entry.Pool = CreatePool(entry);
        }
    }

    // 비활성화된 몬스터가 있으면 꺼내 쓰고, 없을 때만 새로 만든다.
    // 위치를 옮긴 뒤 켜야 이전 위치에서 보간되지 않으므로, 꺼내고 돌려줄 때 켜고 끄는 것은 SpawnEnemy와 Enemy가 맡는다.
    private ObjectPool<Enemy> CreatePool(SpawnEntry entry)
    {
        ObjectPool<Enemy> pool = null;
        pool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                Enemy enemy = Instantiate(entry.Prefab, enemyParent);
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

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval)
        {
            return;
        }

        timer -= spawnInterval;
        SpawnEnemy();
    }

    public Enemy SpawnEnemy()
    {
        if (targetCamera == null)
        {
            return null;
        }

        // 몬스터 부모 아래에는 풀에서 쉬는 비활성 몬스터도 있으므로 자식 수 대신 살아 있는 수를 센다.
        if (maxEnemies > 0 && CountActive() >= maxEnemies)
        {
            return null;
        }

        SpawnEntry entry = PickEntry();
        if (entry == null)
        {
            return null;
        }

        Enemy enemy = entry.Pool.Get();
        enemy.transform.SetPositionAndRotation(GetSpawnPosition(), Quaternion.identity);
        enemy.Initialize(Level);
        enemy.gameObject.SetActive(true);
        entry.SpawnedThisStage++;
        return enemy;
    }

    private int CountActive()
    {
        int count = 0;
        foreach (SpawnEntry entry in entries)
        {
            if (entry.Pool != null)
            {
                count += entry.Pool.CountActive;
            }
        }

        return count;
    }

    // 스테이지가 바뀌면 스테이지당 생성 개수 제한을 초기화합니다.
    public void SetRound(int newStage, int newRound)
    {
        newStage = Mathf.Max(1, newStage);
        if (newStage != stage)
        {
            foreach (SpawnEntry entry in entries)
            {
                entry.SpawnedThisStage = 0;
            }
        }

        stage = newStage;
        round = Mathf.Max(1, newRound);
    }

    // 스폰 가능한 항목 중 가중치에 비례한 확률로 하나를 고릅니다.
    private SpawnEntry PickEntry()
    {
        float total = 0f;
        foreach (SpawnEntry entry in entries)
        {
            if (entry.CanSpawn)
            {
                total += entry.Weight;
            }
        }

        if (total <= 0f)
        {
            return null;
        }

        float pick = Random.value * total;
        SpawnEntry last = null;
        foreach (SpawnEntry entry in entries)
        {
            if (!entry.CanSpawn)
            {
                continue;
            }

            last = entry;
            pick -= entry.Weight;
            if (pick < 0f)
            {
                return entry;
            }
        }

        // Random.value가 1이거나 부동소수점 오차가 남으면 마지막 후보를 고릅니다.
        return last;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 half = GetSpawnHalfExtents(targetCamera);
        float width = half.x * 2f;
        float height = half.y * 2f;
        float t = Random.Range(0f, (width + height) * 2f);

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