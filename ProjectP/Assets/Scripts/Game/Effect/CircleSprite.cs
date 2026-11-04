using UnityEngine;

// 가장자리가 부드러운 흰 원 스프라이트. 색은 SpriteRenderer.color로 입힌다.
// 실행 중에 한 번만 만들어 폭발 연출, 공격 지대, 지원 크리쳐가 함께 쓴다.
public static class CircleSprite
{
    // 텍스처의 한 변 픽셀 수. 스프라이트 지름이 1유닛이 되도록 Pixels Per Unit도 같은 값을 쓴다.
    private const int TextureSize = 128;

    private static Sprite sprite;

    public static Sprite Get()
    {
        if (sprite != null)
        {
            return sprite;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "Circle",
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

        sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return sprite;
    }
}