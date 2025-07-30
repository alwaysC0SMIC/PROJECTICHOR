using System.IO;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;


public class SettingsManager : SerializedMonoBehaviour
{
    #region Singleton
    public static SettingsManager Instance { get; private set; }
    #endregion

    #region Config (Inspector)

    [FoldoutGroup("Config")]
    [LabelText("Auto Save On Change")]
    [SerializeField] private bool autoSave = true;

    [FoldoutGroup("Config")]
    [LabelText("Defaults Suffix")]
    [SerializeField] private string defaultsSuffix = "Default";

    [FoldoutGroup("Config"), ReadOnly, ShowInInspector]
    private string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

    #endregion

    #region Runtime (Inspector)

    [TitleGroup("Current Settings"), InlineProperty, HideLabel]
    [SerializeField] private SettingsData currentSettings = new SettingsData();

    [FoldoutGroup("Access"), ShowInInspector, ReadOnly]
    public SettingsData Settings => currentSettings;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        LoadSettings(); // Creates defaults if file missing
    }

    #endregion

    #region Persistence (Odin JSON)

    [HorizontalGroup("File Actions", 0.25f), Button("📂 Load"), GUIColor(0.6f, 1f, 0.6f)]
    public void LoadSettings()
    {
        if (File.Exists(SavePath))
        {
            // Read JSON string from file -> convert to bytes -> deserialize
            string json = File.ReadAllText(SavePath, Encoding.UTF8);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            currentSettings = SerializationUtility.DeserializeValue<SettingsData>(bytes, DataFormat.JSON)
                               ?? new SettingsData();

            // Patch any missing/new fields from static defaults (keep existing values)
            SettingsDefaultsMapper.ApplyDefaultsToInstance(
        currentSettings,
        typeof(SettingsDefaults),
        suffix: "Default",
        overwriteAll: true,
        warnWhenMissing: true
    );

        }
        else
        {
            // First-time run: build entirely from defaults and save immediately
            currentSettings = SettingsDefaultsMapper.CreateFromDefaults<SettingsData>(
                typeof(SettingsDefaults),
                suffix: "Default"
            );
            SaveSettings();
        }
    }


    [HorizontalGroup("File Actions", 0.25f), Button("💾 Save"), GUIColor(0.6f, 0.8f, 1f)]
    public void SaveSettings()
    {
        // Serialize to JSON bytes -> turn into string -> write to file
        byte[] bytes = SerializationUtility.SerializeValue(currentSettings, DataFormat.JSON);
        string json = Encoding.UTF8.GetString(bytes);

        File.WriteAllText(SavePath, json, Encoding.UTF8);
    }


    [HorizontalGroup("File Actions", 0.25f), Button("📁 Open Folder")]
    public void OpenSaveFolder()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(SavePath);
#else
        Debug.Log($"Save path: {SavePath}");
#endif
    }

    #endregion

    #region Defaults (Mapper Buttons)

    [HorizontalGroup("Defaults", 0.33f)]
    [Button("↩ Apply Defaults (Overwrite All)"), GUIColor(1f, 0.75f, 0.35f)]
    public void ApplyDefaultsOverwriteAll()
    {
        if (currentSettings == null) currentSettings = new SettingsData();

        SettingsDefaultsMapper.ApplyDefaultsToInstance(
            currentSettings,
            typeof(SettingsDefaults),
            defaultsSuffix,
            overwriteAll: true,
            warnWhenMissing: true
        );

        if (autoSave) SaveSettings();
    }

    [HorizontalGroup("Defaults", 0.33f)]
    [Button("🧩 Patch Missing From Defaults"), GUIColor(1f, 0.9f, 0.4f)]
    public void PatchMissingFromDefaults()
    {
        if (currentSettings == null) currentSettings = new SettingsData();

        SettingsDefaultsMapper.PatchMissingFromDefaults(
            currentSettings,
            typeof(SettingsDefaults),
            defaultsSuffix,
            warnWhenMissing: true
        );

        if (autoSave) SaveSettings();
    }

    [HorizontalGroup("Defaults", 0.33f)]
    [Button("🆕 Recreate From Defaults"), GUIColor(0.8f, 0.6f, 1f)]
    public void RecreateFromDefaults()
    {
        currentSettings = SettingsDefaultsMapper.CreateFromDefaults<SettingsData>(
            typeof(SettingsDefaults),
            defaultsSuffix
        );

        if (autoSave) SaveSettings();
    }

    #endregion

    #region Example Mutators (optional auto-save hooks)

    public void SetResolution(string value)
    {
        currentSettings.resolution = value;
        if (autoSave) SaveSettings();
    }

    public void SetMusicVolume(string value)
    {
        currentSettings.musicVolume = value;
        if (autoSave) SaveSettings();
    }

    #endregion
}
