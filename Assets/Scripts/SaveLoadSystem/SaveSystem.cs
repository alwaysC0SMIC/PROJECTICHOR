using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//https://www.youtube.com/watch?v=llmaxNvwy4E&ab_channel=JDDev

public static class SaveSystem
{
    public const string FILENAME_SAVEDATA = "/savedata.json";

    public static void SaveGameState()
    {
        #region BUILDING SAVE

        List<ObjectTransformData> objects = new List<ObjectTransformData>();
        List<GameObject> objectsToSave = PlayerData.Instance.placedObjects;
        foreach (var obj in objectsToSave)
        {
            ObjectTransformData data = new ObjectTransformData
            {
                objectID = RemoveCloneSuffix(obj.name),
                position = obj.transform.position,
                rotation = obj.transform.rotation
            };

            objects.Add(data);
        }

        #endregion

        #region PLAYER DATA SAVE

        PlayerDetails playerDetailsData = new PlayerDetails(PlayerData.Instance)
        {
            level = PlayerData.Instance.Level,
            coins = PlayerData.Instance.Coins,
            xp = PlayerData.Instance.XP,
            reputation = PlayerData.Instance.Reputation
        };

        #endregion

        SaveData saveData = new SaveData(objects.ToArray(), playerDetailsData);
        string txt = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(Application.persistentDataPath + FILENAME_SAVEDATA, txt);
    }

    public static string RemoveCloneSuffix(string name)
    {
        return name.Replace("(Clone)", "").Trim();
    }

    public static SaveData LoadGameState()
    {
        string path = Application.persistentDataPath + FILENAME_SAVEDATA;
        if (!File.Exists(path))
        {
            Debug.LogWarning("SaveSystem: Save file not found at " + path);
            return null;
        }

        string txt = File.ReadAllText(path);
        SaveData loaded = JsonUtility.FromJson<SaveData>(txt);
        EventBus<OnGameStateLoaded>.Raise(new OnGameStateLoaded { loadedData = loaded });
        return loaded;
    }
}

[Serializable] public class SaveData
{
    [SerializeField] public ObjectTransformData[] placedObjects;
    [SerializeField] public QuestSaveDataWrapper questSaveData;
    [SerializeField] public PlayerDetails playerDetails;

    public SaveData(ObjectTransformData[] inPlacedObject, PlayerDetails inPlayerDetails)
    {
        placedObjects = inPlacedObject;
        playerDetails = inPlayerDetails;
    }
    
}

#region OBJECT WRAPPER CLASSES

[Serializable]
public class QuestSaveDataWrapper
{
    public List<QuestSaveData> activeQuests;
    public List<QuestSaveData> completedQuests;
}

[Serializable]
public class ObjectTransformData
{
    public string objectID;  // Unique identifier, e.g., name or GUID
    public Vector3 position;
    public Quaternion rotation;
}

[Serializable]
public class QuestSaveData
{
    public string questID; // Could be questName or a custom unique ID
    public List<ObjectiveSaveData> objectives;
}

[Serializable]
public class ObjectiveSaveData
{
    public string objectiveID; // Could be description or unique ID
    public int currentAmount;
}


[Serializable]
public class PlayerDetails
{
    public float level;
    public float coins;
    public float xp;
    public float reputation;

    public PlayerDetails(PlayerData inData)
    {
        level = inData.Level;
        coins = inData.Coins;
        xp = inData.XP;
        reputation = inData.Reputation;
    }

    // Optional: parameterless constructor for deserialization
    public PlayerDetails() { }
}

#endregion