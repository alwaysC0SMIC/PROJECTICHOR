using System.IO;
using UnityEngine;

//https://www.youtube.com/watch?v=llmaxNvwy4E&ab_channel=JDDev

public static class LoadSystem
{
    public static SaveData LoadGameData()
{
    try
    {
        string filepath = Application.persistentDataPath + SaveSystem.FILENAME_SAVEDATA;
        string fileContent = File.ReadAllText(filepath);

        SaveData saveData = JsonUtility.FromJson<SaveData>(fileContent);
        return saveData;
    }
        catch
    {
        return null;
    }
}


}
