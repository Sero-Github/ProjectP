using System.Collections.Generic;
using UnityEngine;

// 몬스터가 사망할 때 몸이 조각나 흩어지는 파티클. 👿 몬스터 문서 3-3을 따른다.
// - 조각은 몬스터 스프라이트 모양 그대로(원형이면 원, 삼각형이면 삼각형)이며, 크기와 비율, 회전이 제각각이다.
// - 조각은 몬스터 몸 안에서 생겨 중심에서 바깥으로 퍼지고, 1초 동안 작아지며 사라진다.
// - 색은 사망 순간 몬스터의 색(= 주입된 속성의 색)을 따른다.
// 스프라이트 종류마다 파티클 시스템 하나를 만들어 두고 Emit으로 재사용하므로, 사망할 때마다 오브젝트를 만들지 않는다.
// 씬에 배치하면 인스펙터 값으로 조정할 수 있고, 없으면 처음 사망할 때 기본값으로 자동 생성된다.
public class EnemyDeathParticles : MonoBehaviour
{
    private static EnemyDeathParticles instance;

    [Header("개수")]
    [Tooltip("사망 한 번에 생성할 최소 조각 수")]
    [Min(1)]
    [SerializeField] private int minCount = 10;

    [Tooltip("사망 한 번에 생성할 최대 조각 수")]
    [Min(1)]
    [SerializeField] private int maxCount = 16;

    [Header("모양")]
    [Tooltip("조각 크기. 몬스터 크기에 대한 비율입니다.")]
    [SerializeField] private Vector2 sizeRatio = new Vector2(0.2f, 0.45f);

    [Tooltip("조각의 가로, 세로 비율이 달라지는 정도. 0이면 원래 모양 그대로입니다.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float aspectVariance = 0.25f;

    [Tooltip("조각의 회전 속도(도/초)")]
    [SerializeField] private Vector2 angularSpeed = new Vector2(90f, 360f);

    [Header("움직임")]
    [Tooltip("조각이 퍼져 나가는 처음 속도(유닛/초). 바깥쪽 조각일수록 빠릅니다.")]
    [SerializeField] private Vector2 speed = new Vector2(1.5f, 4f);

    [Tooltip("지속 시간(초). 이 시간 동안 작아지며 사라집니다.")]
    [Min(0.05f)]
    [SerializeField] private float lifetime = 1f;

    [Header("색")]
    [Tooltip("조각마다 흰색을 섞는 최대 비율. 같은 색 조각끼리 구분되어 보이게 합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float whiteBlend = 0.3f;

    [Tooltip("몬스터보다 앞에 그리도록 더할 Sorting Order")]
    [SerializeField] private int sortingOrderOffset = 1;

    private readonly Dictionary<Sprite, ParticleSystem> systems = new Dictionary<Sprite, ParticleSystem>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // 몬스터의 SpriteRenderer 모양, 위치, 색으로 사망 파티클을 낸다.
    public static void Play(SpriteRenderer source)
    {
        if (source == null || source.sprite == null)
        {
            return;
        }

        if (instance == null)
        {
            new GameObject(nameof(EnemyDeathParticles)).AddComponent<EnemyDeathParticles>();
        }

        instance.Emit(source);
    }

    private void Emit(SpriteRenderer source)
    {
        ParticleSystem system = GetSystem(source);
        Sprite sprite = source.sprite;
        Transform sourceTransform = source.transform;

        Vector3 center = source.bounds.center;
        Vector3 extents = source.bounds.extents;
        float bodySize = Mathf.Max(extents.x, extents.y) * 2f;
        float bodyRadius = Mathf.Max(0.0001f, bodySize * 0.5f);

        IReadOnlyList<Vector2> vertices = sprite.vertices;
        IReadOnlyList<ushort> triangles = sprite.triangles;
        float totalArea = GetMeshArea(vertices, triangles);

        int count = Random.Range(minCount, Mathf.Max(minCount, maxCount) + 1);
        var emitParams = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            // 조각은 몬스터 몸(스프라이트 메시) 안의 한 점에서 생긴다.
            Vector3 position = sourceTransform.TransformPoint(GetRandomPointInMesh(vertices, triangles, totalArea));
            position.z = center.z;

            Vector2 offset = position - center;
            Vector2 direction = offset.sqrMagnitude > 0.000001f ? offset.normalized : Random.insideUnitCircle.normalized;
            // 바깥쪽 조각일수록 빠르게 날아가 가운데에서 바깥으로 터지는 모양이 된다.
            float distanceRatio = Mathf.Clamp01(offset.magnitude / bodyRadius);
            float particleSpeed = Mathf.Lerp(speed.x, speed.y, Mathf.Lerp(Random.value, distanceRatio, 0.5f));

            float size = bodySize * Random.Range(sizeRatio.x, sizeRatio.y);
            float aspect = Random.Range(1f - aspectVariance, 1f + aspectVariance);

            emitParams.position = position;
            emitParams.velocity = direction * particleSpeed;
            emitParams.startSize3D = new Vector3(size * aspect, size / aspect, 1f);
            emitParams.rotation = Random.Range(0f, 360f);
            emitParams.angularVelocity = Random.Range(angularSpeed.x, angularSpeed.y) * (Random.value < 0.5f ? -1f : 1f);
            emitParams.startLifetime = lifetime;
            emitParams.startColor = Color.Lerp(source.color, Color.white, Random.Range(0f, whiteBlend));

            system.Emit(emitParams, 1);
        }
    }

    // 스프라이트 종류마다 파티클 시스템 하나를 만들어 둔다.
    private ParticleSystem GetSystem(SpriteRenderer source)
    {
        if (systems.TryGetValue(source.sprite, out ParticleSystem system) && system != null)
        {
            return system;
        }

        system = CreateSystem(source);
        systems[source.sprite] = system;
        return system;
    }

    private ParticleSystem CreateSystem(SpriteRenderer source)
    {
        var go = new GameObject($"DeathParticles_{source.sprite.name}");
        go.transform.SetParent(transform, false);

        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 위치, 속도, 크기, 색은 Emit에서 조각마다 정하므로 자동 방출과 모양 모듈은 끈다.
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSize3D = true;
        main.gravityModifier = 0f;
        main.maxParticles = 5000;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        // 빠르게 튀어 나간 뒤 점점 느려진다.
        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f, -3f, -3f),
            new Keyframe(1f, 0.1f, 0f, 0f)));

        // 지속 시간 동안 자연스럽게 작아지며 사라진다.
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -1.5f, -1.5f)));

        // 조각을 몬스터 스프라이트 모양으로 그린다.
        ParticleSystem.TextureSheetAnimationModule textureSheet = system.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Sprites;
        textureSheet.AddSprite(source.sprite);
        textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + sortingOrderOffset;

        system.Play();
        return system;
    }

    private static float GetMeshArea(IReadOnlyList<Vector2> vertices, IReadOnlyList<ushort> triangles)
    {
        float area = 0f;
        for (int i = 0; i + 2 < triangles.Count; i += 3)
        {
            area += GetTriangleArea(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
        }

        return area;
    }

    // 넓이에 비례해 삼각형을 고른 뒤 그 안의 한 점을 고르므로, 메시 전체에 고르게 분포한다.
    private static Vector2 GetRandomPointInMesh(IReadOnlyList<Vector2> vertices, IReadOnlyList<ushort> triangles, float totalArea)
    {
        if (triangles.Count < 3 || totalArea <= 0f)
        {
            return Vector2.zero;
        }

        float pick = Random.value * totalArea;
        int index = 0;
        for (; index + 2 < triangles.Count; index += 3)
        {
            pick -= GetTriangleArea(vertices[triangles[index]], vertices[triangles[index + 1]], vertices[triangles[index + 2]]);
            if (pick <= 0f)
            {
                break;
            }
        }

        // 부동소수점 오차로 끝까지 간 경우 마지막 삼각형을 쓴다.
        index = Mathf.Min(index, triangles.Count - 3);

        Vector2 a = vertices[triangles[index]];
        Vector2 b = vertices[triangles[index + 1]];
        Vector2 c = vertices[triangles[index + 2]];

        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        return a + (b - a) * u + (c - a) * v;
    }

    private static float GetTriangleArea(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
    }
}
