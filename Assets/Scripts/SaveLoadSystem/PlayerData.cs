using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

[Serializable]
public class PlayerSaveData
{
    [BoxGroup("Stats")]
    public float coins;
    
    [BoxGroup("Stats")]
    public float reputation;

    [BoxGroup("XP")]
    public int currentXP;
    
    [BoxGroup("XP")]
    public int currentLevel;

    //[BoxGroup("Placed Objects")]
    //public List<SerializablePlacedObject> placedObjects = new();
}

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    [BoxGroup("Config")]
    [SerializeField] private bool allowSaving = true;

    [BoxGroup("Config")]
    [SerializeField] private SO_PlayerProfileDefaults defaults;

    [BoxGroup("Object Instancing")]
    [SerializeField] private List<GameObject> buildObjects;

    [BoxGroup("Runtime")]
    [ReadOnly] public List<GameObject> placedObjects = new();

    [BoxGroup("Data"), ShowInInspector, ReadOnly]
    private PlayerSaveData currentData;

    private string savePath => Path.Combine(Application.persistentDataPath, "playerData");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        LoadData();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }

    #region Save/Load

    [Button("🧷 Save Data")]
    public void SaveData()
    {
        var bytes = SerializationUtility.SerializeValue(currentData, DataFormat.Binary);
        File.WriteAllBytes(savePath, bytes);
    }

    [Button("📂 Load Data")]
    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            byte[] bytes = File.ReadAllBytes(savePath);
            currentData = SerializationUtility.DeserializeValue<PlayerSaveData>(bytes, DataFormat.Binary);
        }
        else
        {
            currentData = new PlayerSaveData
            {
                coins = defaults.startingCoins,
                reputation = defaults.startingReputation,
                currentXP = defaults.startingXP,
                currentLevel = defaults.startingLevel,
                //placedObjects = new()
            };

            SaveData(); // Initialize file
        }

        //RebuildPlacedObjects();
    }

    // private void RebuildPlacedObjects()
    // {
    //     placedObjects.Clear();
    //     foreach (var obj in currentData.placedObjects)
    //     {
    //         GameObject prefab = buildObjects.Find(p => p.name == obj.objectID);
    //         if (prefab != null)
    //         {
    //             GameObject instance = Instantiate(prefab, obj.position, obj.rotation);
    //             placedObjects.Add(instance);
    //         }
    //     }
    // }

    #endregion

    #region Public API

    public float Coins => currentData.coins;
    public float Reputation => currentData.reputation;
    public int XP => currentData.currentXP;
    public int Level => currentData.currentLevel;

    public int XPToNextLevel => Level * 100; // Optional: Use a dynamic value

    public void AddXP(int amount)
    {
        currentData.currentXP += amount;
        if (currentData.currentXP >= XPToNextLevel)
        {
            currentData.currentXP -= XPToNextLevel;
            currentData.currentLevel++;
        }

        SaveData();
    }

    public void AddCoins(float amount)
    {
        currentData.coins += amount;
        SaveData();
    }

    #endregion
}
