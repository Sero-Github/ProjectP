using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnMargin = 1f;
    [SerializeField] private int maxEnemies = 0;

    private float timer;

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
        if (enemyPrefab == null || targetCamera == null)
        {
            return null;
        }

        if (maxEnemies > 0 && enemyParent.childCount >= maxEnemies)
        {
            return null;
        }

        return Instantiate(enemyPrefab, GetSpawnPosition(), Quaternion.identity, enemyParent);
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