using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine; // for Debug.LogWarning

[System.Serializable]
public class SettingsData
{
    //VIDEO
    public string screenMode = "Fullscreen";
    public string resolution = "1920 x 1080";
    public string refreshRate = "60";
    public string vSync = "On";
    public string quality = "High";
    public string postProcessing = "On";
    public string antiAliasing = "2x";

    //AUDIO
    public string masterVolume = "5";
    public string musicVolume = "10";
    public string sFXVolume = "10";
    public string uIVolume = "10";

    //GENERAL
    public string mouseSensitivity = "5";
    public string controllerSensitivity = "5";


}

public static class SettingsDefaults
{
    // VIDEO
    public const string screenModeDefault        = "Fullscreen";
    public const string resolutionDefault        = "1920 x 1080";
    public const string refreshRateDefault       = "60";
    public const string vSyncDefault             = "On";
    public const string qualityDefault           = "High";
    public const string postProcessingDefault    = "On";
    public const string antiAliasingDefault      = "2x";

    // AUDIO
    public const string masterVolumeDefault      = "5";
    public const string musicVolumeDefault       = "10";
    public const string sFXVolumeDefault         = "10";
    public const string uIVolumeDefault          = "10";

    // GENERAL
    public const string mouseSensitivityDefault       = "5";
    public const string controllerSensitivityDefault  = "5";
}




public static class SettingsDefaultsMapper
{
    // Create a new TTarget and populate it using static defaults found on defaultsType.
    // Matches "<targetName><suffix>" (suffix = "Default" by default).
    public static TTarget CreateFromDefaults<TTarget>(Type defaultsType, string suffix = "Default")
        where TTarget : new()
    {
        var target = new TTarget();
        ApplyDefaultsToInstance(target, defaultsType, suffix, overwriteAll: true, warnWhenMissing: true);
        return target;
    }

    // Overwrite all target members from defaults
    public static void ApplyDefaultsToInstance<TTarget>(
        TTarget target,
        Type defaultsType,
        string suffix = "Default",
        bool overwriteAll = true,
        bool warnWhenMissing = true)
    {
        if (target == null) return;
        if (defaultsType == null) throw new ArgumentNullException(nameof(defaultsType));
        MapStaticToInstance(defaultsType, target, suffix, overwriteAll, onlyWhenMissingOrEmpty: false, warnWhenMissing);
    }

    // Only fill missing/empty/default members from defaults (good for migrations)
    public static void PatchMissingFromDefaults<TTarget>(
        TTarget target,
        Type defaultsType,
        string suffix = "Default",
        bool warnWhenMissing = true)
    {
        if (target == null) return;
        if (defaultsType == null) throw new ArgumentNullException(nameof(defaultsType));
        MapStaticToInstance(defaultsType, target, suffix, overwriteAll: false, onlyWhenMissingOrEmpty: true, warnWhenMissing);
    }

    // -------------------- INTERNALS --------------------

    private static void MapStaticToInstance(
        Type defaultsType,
        object target,
        string suffix,
        bool overwriteAll,
        bool onlyWhenMissingOrEmpty,
        bool warnWhenMissing)
    {
        var targetType  = target.GetType();

        var targetFields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        var targetProps  = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .Where(p => p.CanWrite && p.GetSetMethod(true) != null)
                                     .ToArray();

        var defaultFields = defaultsType.GetFields(BindingFlags.Public | BindingFlags.Static);
        var defaultProps  = defaultsType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                                        .Where(p => p.CanRead && p.GetGetMethod(true) != null)
                                        .ToArray();

        string Norm(string n) => NormalizeName(n);
        string DefaultKeyFor(string targetName) => Norm(targetName + suffix);

        var defaultsLookup = defaultFields
            .Cast<MemberInfo>()
            .Concat(defaultProps)
            .ToLookup(m => Norm(m.Name));

        void SetMember(MemberInfo member, object value)
        {
            if (member is FieldInfo fi)
            {
                var current = fi.GetValue(target);

                if (onlyWhenMissingOrEmpty && !IsDefaultOrEmpty(current)) return;
                if (!overwriteAll && !IsDefaultOrEmpty(current)) return;

                if (TryChangeType(value, fi.FieldType, out var converted))
                    fi.SetValue(target, converted);
            }
            else if (member is PropertyInfo pi)
            {
                var current = pi.CanRead ? pi.GetValue(target) : null;

                if (onlyWhenMissingOrEmpty && !IsDefaultOrEmpty(current)) return;
                if (!overwriteAll && !IsDefaultOrEmpty(current)) return;

                if (TryChangeType(value, pi.PropertyType, out var converted))
                    pi.SetValue(target, converted);
            }
        }

        // Fields
        foreach (var tf in targetFields)
        {
            var key = DefaultKeyFor(tf.Name);
            var src = defaultsLookup[key].FirstOrDefault();

            if (src != null) SetMember(tf, GetStaticValue(src));
            else if (warnWhenMissing)
                Debug.LogWarning($"[SettingsDefaultsMapper] No matching default for field '{tf.Name}'. Expected static '{tf.Name + suffix}'.");
        }

        // Properties
        foreach (var tp in targetProps)
        {
            var key = DefaultKeyFor(tp.Name);
            var src = defaultsLookup[key].FirstOrDefault();

            if (src != null) SetMember(tp, GetStaticValue(src));
            else if (warnWhenMissing)
                Debug.LogWarning($"[SettingsDefaultsMapper] No matching default for property '{tp.Name}'. Expected static '{tp.Name + suffix}'.");
        }
    }

    private static object GetStaticValue(MemberInfo m) =>
        m is FieldInfo fi ? fi.GetValue(null) :
        m is PropertyInfo pi ? pi.GetValue(null, null) : null;

    private static string NormalizeName(string name)
    {
        var arr = name.Where(char.IsLetterOrDigit).ToArray();
        return new string(arr).ToLowerInvariant();
    }

    private static bool IsDefaultOrEmpty(object value)
    {
        if (value == null) return true;
        if (value is string s) return string.IsNullOrEmpty(s);
        var t = value.GetType();
        return t.IsValueType && value.Equals(Activator.CreateInstance(t));
    }

    private static bool TryChangeType(object input, Type targetType, out object converted)
    {
        converted = null;

        if (input == null)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
            {
                converted = null;
                return true;
            }
            return false;
        }

        var inputType = input.GetType();

        if (targetType.IsAssignableFrom(inputType))
        {
            converted = input;
            return true;
        }

        if (targetType == typeof(string))
        {
            converted = input.ToString();
            return true;
        }

        var underlying = Nullable.GetUnderlyingType(targetType);
        var finalType  = underlying ?? targetType;

        try
        {
            if (finalType.IsEnum)
            {
                if (input is string es)
                {
                    converted = Enum.Parse(finalType, es, ignoreCase: true);
                    return true;
                }
                converted = Enum.ToObject(finalType, input);
                return true;
            }

            converted = Convert.ChangeType(input, finalType);
            return true;
        }
        catch { return false; }
    }
}


