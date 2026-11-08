using UnityEditor;
using UnityEngine;

// 테스트용 저장 파일 메뉴.
public static class SaveMenu
{
    [MenuItem("ProjectP/Save/Delete Save")]
    private static void DeleteSave()
    {
        SaveSystem.Delete();
        Debug.Log($"저장 파일을 삭제했습니다. ({SaveSystem.FilePath})");
    }

    [MenuItem("ProjectP/Save/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}