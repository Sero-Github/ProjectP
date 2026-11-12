using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// NotoSansKR Regular/Bold 폰트 에셋을 정해 둔 글자 집합으로 미리 구워 Static 아틀라스로 만든다.
// Dynamic 아틀라스는 플레이할 때마다 새 글리프가 에셋 파일에 저장되어 git 변경이 계속 생기므로 Static으로 고정한다.
// - 글자 집합: Assets/Data/FontCharset.txt (ASCII, 한글 자모, 기호, KS X 1001 한글 2350자)
//   + 스크립트·데이터 파일에 쓰인 한글 중 집합에 없는 글자 (자동으로 추가하고 로그로 알린다)
// - Bold 에셋은 Regular 에셋의 Font Weights(700)에 연결해 <b> 태그가 진짜 굵은 글꼴을 쓰게 한다.
// - TMP Settings 기본 폰트를 Regular로 바꾸고, 더 이상 쓰지 않는 폴백 목록을 비운다.
// 에셋을 지우지 않고 그 자리에서 다시 굽기 때문에 GUID가 유지된다. 텍스트에 새 글자가 생기면 다시 실행하면 된다.
public static class FontAtlasBaker
{
    private const string FontFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials/";
    private const string RegularPath = FontFolder + "NotoSansKR-Regular SDF.asset";
    private const string BoldPath = FontFolder + "NotoSansKR-Bold SDF.asset";
    private const string CharsetPath = "Assets/Data/FontCharset.txt";

    // 약 2,600자를 4096x2048 한 장에 담을 수 있는 크기. 패딩은 샘플링 크기의 약 1/8로 외곽선 효과 여유를 둔다.
    // 넘치면 Multi Atlas Textures 설정으로 두 번째 텍스처가 자동으로 추가된다.
    private const int SamplingPointSize = 40;
    private const int AtlasPadding = 5;
    private const int AtlasWidth = 4096;
    private const int AtlasHeight = 2048;

    // TMP는 굵기 / 100 을 Font Weights 표의 인덱스로 쓴다. (Bold = 700)
    private const int BoldWeightIndex = 7;

    // 글자 집합에 없는 글자를 찾을 때 훑어볼 파일 확장자.
    private static readonly string[] ScanExtensions = { ".cs", ".csv", ".txt" };
    private const string ScanExcludedFolder = "TextMesh Pro";

    // public: 에디터를 닫은 채 batchmode의 -executeMethod로도 실행할 수 있게 한다.
    [MenuItem("ProjectP/Font/Bake Static Atlases")]
    public static void BakeAll()
    {
        string charset = BuildCharset();
        if (string.IsNullOrEmpty(charset))
            return;

        TMP_FontAsset regular = Bake(RegularPath, charset);
        TMP_FontAsset bold = Bake(BoldPath, charset);
        if (regular == null || bold == null)
            return;

        LinkBoldWeight(regular, bold);
        SetDefaultFont(regular);
        AssetDatabase.SaveAssets();
        Debug.Log($"폰트 아틀라스를 Static으로 구웠습니다. ({charset.Length}자)");
    }

    // 모든 씬의 TMP 텍스트 중 Regular/Bold가 아닌 폰트를 쓰는 것을 Regular로 바꾼다.
    [MenuItem("ProjectP/Font/Apply Default Font To Scenes")]
    public static void ApplyDefaultFontToScenes()
    {
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularPath);
        TMP_FontAsset bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldPath);
        if (regular == null)
        {
            Debug.LogError($"{RegularPath}를 찾을 수 없습니다.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var setup = EditorSceneManager.GetSceneManagerSetup();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            int changed = 0;
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.font == regular || text.font == bold)
                    continue;

                Undo.RecordObject(text, "Apply Default Font");
                text.font = regular;
                text.fontSharedMaterial = regular.material;
                EditorUtility.SetDirty(text);
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"{path}: TMP 텍스트 {changed}개의 폰트를 {regular.name}로 바꿨습니다.");
            }
        }

        if (setup.Length > 0)
            EditorSceneManager.RestoreSceneManagerSetup(setup);
    }

    private static string BuildCharset()
    {
        if (!File.Exists(CharsetPath))
        {
            Debug.LogError($"{CharsetPath}를 찾을 수 없습니다.");
            return null;
        }

        var chars = new SortedSet<char>();
        foreach (char c in File.ReadAllText(CharsetPath))
        {
            if (!char.IsControl(c))
                chars.Add(c);
        }

        var extra = new StringBuilder();
        foreach (string file in Directory.EnumerateFiles("Assets", "*.*", SearchOption.AllDirectories))
        {
            if (!IsScanTarget(file))
                continue;

            foreach (char c in File.ReadAllText(file))
            {
                if (IsHangul(c) && chars.Add(c))
                    extra.Append(c);
            }
        }

        if (extra.Length > 0)
            Debug.Log($"글자 집합에 없던 한글 {extra.Length}자를 추가했습니다: {extra}");

        var result = new StringBuilder(chars.Count);
        foreach (char c in chars)
            result.Append(c);
        return result.ToString();
    }

    private static bool IsScanTarget(string file)
    {
        string normalized = file.Replace('\\', '/');
        if (normalized.Contains(ScanExcludedFolder) || normalized == CharsetPath)
            return false;

        string extension = Path.GetExtension(file);
        foreach (string target in ScanExtensions)
        {
            if (extension == target)
                return true;
        }
        return false;
    }

    // 한글 음절(가~힣)과 호환용 자모(ㄱ~ㅣ).
    private static bool IsHangul(char c)
    {
        return (c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅣ');
    }

    private static TMP_FontAsset Bake(string path, string charset)
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (fontAsset == null)
        {
            Debug.LogError($"{path}를 찾을 수 없습니다.");
            return null;
        }

        // Static 에셋은 원본 폰트 참조를 들고 있지 않으므로, 에셋에 남아 있는 원본 폰트 GUID로 찾는다.
        var serialized = new SerializedObject(fontAsset);
        string sourceGuid = serialized.FindProperty("m_SourceFontFileGUID").stringValue;
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(sourceGuid));
        if (sourceFont == null)
        {
            Debug.LogError($"{fontAsset.name}: 원본 폰트 파일(.ttf)이 연결되어 있지 않습니다.", fontAsset);
            return null;
        }

        if (FontEngine.LoadFontFace(sourceFont, SamplingPointSize) != FontEngineError.Success)
        {
            Debug.LogError($"{sourceFont.name}을 불러오지 못했습니다. 폰트 Import Settings의 Include Font Data를 확인하세요.", sourceFont);
            return null;
        }

        // 아틀라스 크기·패딩은 internal setter라 SerializedObject로 바꾼다.
        serialized.FindProperty("m_AtlasWidth").intValue = AtlasWidth;
        serialized.FindProperty("m_AtlasHeight").intValue = AtlasHeight;
        serialized.FindProperty("m_AtlasPadding").intValue = AtlasPadding;
        serialized.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = true;
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 글리프를 추가하려면 잠시 Dynamic이어야 한다. (setter가 원본 폰트 연결도 되살린다)
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.faceInfo = FontEngine.GetFaceInfo();
        fontAsset.ClearFontAssetData(false);

        if (!fontAsset.TryAddCharacters(charset, out string missing, true))
            Debug.LogWarning($"{fontAsset.name}: 폰트에 없는 글자 {missing.Length}자는 빠졌습니다: {missing}", fontAsset);

        UpdateMaterial(fontAsset);
        UpdateCreationSettings(fontAsset);

        // Static으로 고정하고, 런타임에 CPU 사본이 남지 않도록 텍스처를 읽기 불가로 둔다. (인스펙터에서 Static으로 바꿀 때와 같다)
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        foreach (var texture in fontAsset.atlasTextures)
        {
            if (texture == null)
                continue;

            SetTextureReadable(texture, false);
            EditorUtility.SetDirty(texture);
        }

        EditorUtility.SetDirty(fontAsset.material);
        EditorUtility.SetDirty(fontAsset);
        Debug.Log($"{fontAsset.name}: 글리프 {fontAsset.characterTable.Count}자, 아틀라스 {fontAsset.atlasTextures.Length}장", fontAsset);
        return fontAsset;
    }

    private static void UpdateMaterial(TMP_FontAsset fontAsset)
    {
        Material material = fontAsset.material;
        material.SetFloat(ShaderUtilities.ID_TextureWidth, AtlasWidth);
        material.SetFloat(ShaderUtilities.ID_TextureHeight, AtlasHeight);
        // SDF 렌더 모드에서 TMP는 패딩 + 1을 Gradient Scale로 쓴다.
        material.SetFloat(ShaderUtilities.ID_GradientScale, AtlasPadding + 1);
        material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
        material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);
    }

    // 인스펙터의 Font Asset Creator 정보가 실제 설정과 어긋나지 않게 맞춘다.
    private static void UpdateCreationSettings(TMP_FontAsset fontAsset)
    {
        FontAssetCreationSettings settings = fontAsset.creationSettings;
        settings.pointSize = SamplingPointSize;
        settings.padding = AtlasPadding;
        settings.atlasWidth = AtlasWidth;
        settings.atlasHeight = AtlasHeight;
        settings.includeFontFeatures = true;
        fontAsset.creationSettings = settings;
    }

    private static void SetTextureReadable(Texture2D texture, bool readable)
    {
        var serialized = new SerializedObject(texture);
        serialized.FindProperty("m_IsReadable").boolValue = readable;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void LinkBoldWeight(TMP_FontAsset regular, TMP_FontAsset bold)
    {
        TMP_FontWeightPair[] table = regular.fontWeightTable;
        if (table == null || table.Length <= BoldWeightIndex)
        {
            Debug.LogError($"{regular.name}: Font Weights 표가 올바르지 않습니다.", regular);
            return;
        }

        table[BoldWeightIndex].regularTypeface = bold;
        EditorUtility.SetDirty(regular);
    }

    private static void SetDefaultFont(TMP_FontAsset regular)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogError("TMP Settings를 찾을 수 없습니다.");
            return;
        }

        var serialized = new SerializedObject(settings);
        serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
        // 한글을 대신 그리던 VariableFont 폴백은 더 이상 필요 없다. 기본 폰트가 한글을 모두 가지고 있다.
        serialized.FindProperty("m_fallbackFontAssets").ClearArray();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }
}
