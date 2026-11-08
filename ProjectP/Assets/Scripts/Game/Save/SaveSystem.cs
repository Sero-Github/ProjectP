using System;
using System.IO;
using UnityEngine;

// 진행 상황을 persistentDataPath/save.json에 저장하고 읽는다.
// 임시 파일에 다 쓴 뒤 교체하므로 쓰는 도중 게임이 꺼져도 이전 저장은 남는다.
public static class SaveSystem
{
    private const string FileName = "save.json";

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 저장 파일이 없거나 읽지 못하면 빈 진행 상황을 돌려준다.
    public static SaveData Load()
    {
        string path = FilePath;
        if (!File.Exists(path))
        {
            return new SaveData();
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null)
            {
                throw new InvalidDataException("내용이 비어 있습니다.");
            }

            data.bossClearedStages ??= new System.Collections.Generic.List<int>();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"저장 파일을 읽지 못해 처음부터 시작합니다. ({path})\n{e.Message}");
            return new SaveData();
        }
    }

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        string path = FilePath;
        string tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"진행 상황을 저장하지 못했습니다. ({path})\n{e.Message}");
        }
    }

    public static void Delete()
    {
        string path = FilePath;
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}