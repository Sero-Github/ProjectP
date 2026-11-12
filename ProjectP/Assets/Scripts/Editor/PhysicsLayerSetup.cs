using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 플레이어, 몬스터, 탄환, 몬스터 탄환을 각자의 물리 레이어로 나누고, 실제로 처리하는 조합만 서로 닿게 한다.
// 탄환끼리처럼 코드가 무시하는 조합까지 트리거 판정과 OnTriggerEnter2D 호출이 일어나지 않게 하는 최적화이며 동작은 같다.
// - 몬스터 ↔ 플레이어: 근접 공격 (Enemy)
// - 몬스터 ↔ 탄환: 탄환 타격 (Bullet)
// - 몬스터 탄환 ↔ 플레이어: 원거리 공격 (EnemyBullet)
// 여러 번 실행해도 결과는 같다. 실행 후 열린 씬(플레이어 레이어)을 저장해야 한다.
public static class PhysicsLayerSetup
{
    public const string PlayerLayer = "Player";
    public const string EnemyLayer = "Enemy";
    public const string PlayerBulletLayer = "PlayerBullet";
    public const string EnemyBulletLayer = "EnemyBullet";

    // 0~7번은 Unity 기본 레이어 자리이므로 사용자 레이어는 8번부터 쓴다.
    private const int FirstUserLayer = 8;
    private const int LayerCount = 32;

    [MenuItem("ProjectP/Physics/Setup Layers")]
    private static void Setup()
    {
        int player = EnsureLayer(PlayerLayer);
        int enemy = EnsureLayer(EnemyLayer);
        int playerBullet = EnsureLayer(PlayerBulletLayer);
        int enemyBullet = EnsureLayer(EnemyBulletLayer);
        if (player < 0 || enemy < 0 || playerBullet < 0 || enemyBullet < 0)
        {
            Debug.LogError("빈 사용자 레이어가 부족해 물리 레이어를 설정하지 못했습니다.");
            return;
        }

        ApplyCollisionMatrix(player, enemy, playerBullet, enemyBullet);

        int prefabCount = 0;
        prefabCount += SetPrefabLayers<Enemy>(enemy);
        prefabCount += SetPrefabLayers<Bullet>(playerBullet);
        prefabCount += SetPrefabLayers<EnemyBullet>(enemyBullet);
        int playerCount = SetScenePlayerLayers(player);

        AssetDatabase.SaveAssets();
        Debug.Log($"물리 레이어 설정 완료: 프리팹 {prefabCount}개, 씬 플레이어 {playerCount}개. 플레이어 레이어가 바뀐 씬을 저장하세요.");
    }

    // 이름이 같은 레이어가 있으면 그 번호를, 없으면 빈 사용자 레이어에 만들어 그 번호를 돌려준다. 자리가 없으면 -1이다.
    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
        {
            return existing;
        }

        Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = FirstUserLayer; i < LayerCount; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        return -1;
    }

    // 네 레이어끼리의 조합만 정한다. 다른 레이어(Default 등)와의 설정은 건드리지 않는다.
    private static void ApplyCollisionMatrix(int player, int enemy, int playerBullet, int enemyBullet)
    {
        int[] layers = { player, enemy, playerBullet, enemyBullet };
        foreach (int a in layers)
        {
            foreach (int b in layers)
            {
                Physics2D.IgnoreLayerCollision(a, b, !ShouldCollide(a, b));
            }
        }

        // IgnoreLayerCollision은 메모리의 설정만 바꾸므로 SaveAssets 때 파일에도 쓰이도록 표시한다.
        Object physicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0];
        EditorUtility.SetDirty(physicsSettings);

        bool ShouldCollide(int a, int b)
        {
            return IsPair(a, b, enemy, player)
                || IsPair(a, b, enemy, playerBullet)
                || IsPair(a, b, enemyBullet, player);
        }

        static bool IsPair(int a, int b, int x, int y)
        {
            return (a == x && b == y) || (a == y && b == x);
        }
    }

    // T 컴포넌트를 루트에 가진 프리팹의 레이어를 바꾼다. 콜라이더는 루트에만 있으므로 루트만 바꾼다.
    private static int SetPrefabLayers<T>(int layer) where T : Component
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<T>() == null)
            {
                continue;
            }

            count++;
            if (asset.layer == layer)
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.layer = layer;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return count;
    }

    // 플레이어는 씬에 직접 배치되어 있으므로 열린 씬의 플레이어 레이어를 바꾼다.
    private static int SetScenePlayerLayers(int layer)
    {
        Player[] players = Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Player player in players)
        {
            GameObject go = player.gameObject;
            if (go.layer == layer)
            {
                continue;
            }

            Undo.RecordObject(go, "Set Player Layer");
            go.layer = layer;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        return players.Length;
    }
}
