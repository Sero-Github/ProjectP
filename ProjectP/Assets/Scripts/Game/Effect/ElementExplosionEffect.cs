using System.Collections.Generic;
using UnityEngine;

// 빨강(폭발) 속성의 폭발 범위를 보여주는 임시 연출. 문서에 연출 정의가 없어 원이 반경까지 커지며 사라지게만 한다.
// 원 스프라이트는 실행 중에 만들고, 사라진 원은 비활성화해 두었다가 다음 폭발에 재사용한다.
// 씬에 배치하면 인스펙터 값으로 조정할 수 있고, 없으면 처음 폭발할 때 기본값으로 자동 생성된다.
public class ElementExplosionEffect : MonoBehaviour
{
    private class Burst
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public float Diameter;
        public float Elapsed;
    }

    // 원 스프라이트 텍스처의 한 변 픽셀 수. 스프라이트 지름이 1유닛이 되도록 Pixels Per Unit도 같은 값을 쓴다.
    private const int TextureSize = 128;

    private static ElementExplosionEffect instance;

    [Tooltip("원이 반경까지 커지며 사라지는 시간(초)")]
    [Min(0.05f)]
    [SerializeField] private float duration = 0.3f;

    [Tooltip("처음 크기. 폭발 지름에 대한 비율입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float startScale = 0.3f;

    [Tooltip("처음 불투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0.6f;

    [Tooltip("몬스터(Sorting Order 0) 기준으로 그릴 Sorting Order")]
    [SerializeField] private int sortingOrder = -1;

    private readonly List<Burst> actives = new List<Burst>();
    private readonly Stack<Burst> inactives = new Stack<Burst>();
    private Sprite circleSprite;

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

    // position을 중심으로 radius까지 퍼지는 원을 그린다.
    public static void Play(Vector2 position, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        if (instance == null)
        {
            new GameObject(nameof(ElementExplosionEffect)).AddComponent<ElementExplosionEffect>();
        }

        instance.Emit(position, radius, color);
    }

    private void Emit(Vector2 position, float radius, Color color)
    {
        Burst burst = inactives.Count > 0 ? inactives.Pop() : CreateBurst();
        burst.Color = color;
        burst.Diameter = radius * 2f;
        burst.Elapsed = 0f;

        burst.Renderer.transform.position = position;
        Apply(burst, 0f);
        burst.Renderer.gameObject.SetActive(true);
        actives.Add(burst);
    }

    private void Update()
    {
        for (int i = actives.Count - 1; i >= 0; i--)
        {
            Burst burst = actives[i];
            burst.Elapsed += Time.deltaTime;

            float t = burst.Elapsed / duration;
            if (t >= 1f)
            {
                burst.Renderer.gameObject.SetActive(false);
                actives.RemoveAt(i);
                inactives.Push(burst);
                continue;
            }

            Apply(burst, t);
        }
    }

    // 빠르게 퍼진 뒤 천천히 멈추며, 퍼지는 동안 옅어진다.
    private void Apply(Burst burst, float t)
    {
        float eased = 1f - (1f - t) * (1f - t);
        float scale = burst.Diameter * Mathf.Lerp(startScale, 1f, eased);
        burst.Renderer.transform.localScale = new Vector3(scale, scale, 1f);

        Color color = burst.Color;
        color.a = startAlpha * (1f - t);
        burst.Renderer.color = color;
    }

    private Burst CreateBurst()
    {
        var go = new GameObject("ExplosionBurst");
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetCircleSprite();
        spriteRenderer.sortingOrder = sortingOrder;

        return new Burst { Renderer = spriteRenderer };
    }

    // 가장자리가 부드러운 흰 원. 색은 SpriteRenderer.color로 입힌다.
    private Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "ExplosionCircle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[TextureSize * TextureSize];
        float center = (TextureSize - 1) * 0.5f;
        float radius = TextureSize * 0.5f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                // 바깥 1픽셀은 계단 현상이 없도록 투명하게 흐린다.
                byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                pixels[y * TextureSize + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return circleSprite;
    }
}