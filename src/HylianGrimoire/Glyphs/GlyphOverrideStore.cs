using System.Text.Json;
using HylianGrimoire.Codecs;

namespace HylianGrimoire.Glyphs;

public sealed class GlyphOverrideStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static GlyphOverrideStore Current { get; } = Load();

    private readonly Dictionary<string, GlyphOverride> _overrides;

    private GlyphOverrideStore(Dictionary<string, GlyphOverride> overrides, string? loadWarning = null)
    {
        _overrides = overrides;
        LoadWarning = loadWarning;
    }

    public event EventHandler? Changed;

    public string? LoadWarning { get; }

    public int Version { get; private set; }

    public string OverrideAssetRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HylianGrimoire",
        "Assets",
        "Preview",
        "Oot");

    private static string ConfigPath => Path.Combine(
        Environment.GetEnvironmentVariable("OOT_EDITOR_GLYPH_OVERRIDE_CONFIG_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HylianGrimoire"),
        "glyph_overrides.json");

    public bool TryGetDisplayChar(byte value, out char displayChar)
    {
        displayChar = default;
        if (!TryGetOverride(value, out GlyphOverride? glyphOverride)
            || glyphOverride is null
            || string.IsNullOrEmpty(glyphOverride.DisplayChar))
        {
            return false;
        }

        displayChar = glyphOverride.DisplayChar[0];
        return true;
    }

    public bool TryGetByte(char displayChar, out byte value)
    {
        foreach ((string key, GlyphOverride glyphOverride) in _overrides)
        {
            if (glyphOverride.DisplayChar == displayChar.ToString()
                && TryParseKey(key, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    public bool TryGetWidth(byte value, out double width)
    {
        width = 0;
        if (TryGetOverride(value, out GlyphOverride? glyphOverride)
            && glyphOverride is not null
            && glyphOverride.Width is double configuredWidth)
        {
            width = configuredWidth;
            return true;
        }

        return false;
    }

    public bool TryGetImagePath(byte value, out string? path)
    {
        path = null;
        if (!TryGetOverride(value, out GlyphOverride? glyphOverride)
            || glyphOverride is null
            || string.IsNullOrWhiteSpace(glyphOverride.ImageRelativePath))
        {
            return false;
        }

        string candidate = Path.Combine(OverrideAssetRoot, glyphOverride.ImageRelativePath);
        if (!File.Exists(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public void SetDisplayChar(byte value, char displayChar)
    {
        if (displayChar == MessageEncodingProfile.Default.GetDefaultEditorChar(value))
        {
            ResetDisplayChar(value);
            return;
        }

        GetOrCreate(value).DisplayChar = displayChar.ToString();
        SaveAndNotify();
    }

    public void ResetDisplayChar(byte value)
    {
        if (TryGetOverride(value, out GlyphOverride? glyphOverride)
            && glyphOverride is not null)
        {
            glyphOverride.DisplayChar = null;
            RemoveIfEmpty(value, glyphOverride);
            SaveAndNotify();
        }
    }

    public void SetWidth(byte value, double width)
    {
        if (Math.Abs(width - OotGlyphMetrics.GetDefaultAdvance(value)) < 0.001)
        {
            ResetWidth(value);
            return;
        }

        GetOrCreate(value).Width = width;
        SaveAndNotify();
    }

    public void ResetWidth(byte value)
    {
        if (TryGetOverride(value, out GlyphOverride? glyphOverride)
            && glyphOverride is not null)
        {
            glyphOverride.Width = null;
            RemoveIfEmpty(value, glyphOverride);
            SaveAndNotify();
        }
    }

    public void SetImage(byte value, string sourcePath)
    {
        string relativePath = OotGlyphCatalog.GetGlyphRelativePath(value);
        string destination = Path.Combine(OverrideAssetRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(sourcePath, destination, overwrite: true);
        GetOrCreate(value).ImageRelativePath = relativePath;
        SaveAndNotify();
    }

    public void ResetImage(byte value)
    {
        if (TryGetOverride(value, out GlyphOverride? glyphOverride)
            && glyphOverride is not null)
        {
            if (!string.IsNullOrWhiteSpace(glyphOverride.ImageRelativePath))
            {
                string imagePath = Path.Combine(OverrideAssetRoot, glyphOverride.ImageRelativePath);
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }

            glyphOverride.ImageRelativePath = null;
            RemoveIfEmpty(value, glyphOverride);
            SaveAndNotify();
        }
    }

    private GlyphOverride GetOrCreate(byte value)
    {
        string key = ToKey(value);
        if (!_overrides.TryGetValue(key, out GlyphOverride? glyphOverride))
        {
            glyphOverride = new GlyphOverride();
            _overrides[key] = glyphOverride;
        }

        return glyphOverride;
    }

    private bool TryGetOverride(byte value, out GlyphOverride? glyphOverride)
    {
        return _overrides.TryGetValue(ToKey(value), out glyphOverride);
    }

    private void RemoveIfEmpty(byte value, GlyphOverride glyphOverride)
    {
        if (glyphOverride.DisplayChar is null
            && glyphOverride.Width is null
            && glyphOverride.ImageRelativePath is null)
        {
            _overrides.Remove(ToKey(value));
        }
    }

    private void SaveAndNotify()
    {
        WriteConfig(_overrides);
        Version++;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static GlyphOverrideStore Load()
    {
        if (Environment.GetEnvironmentVariable("OOT_EDITOR_DISABLE_GLYPH_OVERRIDES") == "1")
        {
            return new GlyphOverrideStore([]);
        }

        try
        {
            if (File.Exists(ConfigPath))
            {
                var overrides = JsonSerializer.Deserialize<Dictionary<string, GlyphOverride>>(File.ReadAllText(ConfigPath));
                overrides ??= [];
                string beforePrune = JsonSerializer.Serialize(overrides, JsonOptions);
                var pruned = PruneNoOpOverrides(overrides);
                if (!string.Equals(beforePrune, JsonSerializer.Serialize(pruned, JsonOptions), StringComparison.Ordinal))
                {
                    WriteConfig(pruned);
                }

                return new GlyphOverrideStore(pruned);
            }
        }
        catch (Exception ex)
        {
            return new GlyphOverrideStore([], $"Character overrides could not be loaded: {ex.Message}");
        }

        return new GlyphOverrideStore([]);
    }

    private static Dictionary<string, GlyphOverride> PruneNoOpOverrides(Dictionary<string, GlyphOverride> overrides)
    {
        foreach (string key in overrides.Keys.ToArray())
        {
            if (!TryParseKey(key, out byte value))
            {
                overrides.Remove(key);
                continue;
            }

            GlyphOverride glyphOverride = overrides[key];
            if (glyphOverride.DisplayChar == MessageEncodingProfile.Default.GetDefaultEditorChar(value).ToString())
            {
                glyphOverride.DisplayChar = null;
            }

            if (glyphOverride.Width is double width
                && Math.Abs(width - OotGlyphMetrics.GetDefaultAdvance(value)) < 0.001)
            {
                glyphOverride.Width = null;
            }

            if (glyphOverride.DisplayChar is null
                && glyphOverride.Width is null
                && glyphOverride.ImageRelativePath is null)
            {
                overrides.Remove(key);
            }
        }

        return overrides;
    }

    private static void WriteConfig(Dictionary<string, GlyphOverride> overrides)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(overrides, JsonOptions));
    }

    private static string ToKey(byte value) => $"0x{value:X2}";

    private static bool TryParseKey(string key, out byte value)
    {
        string text = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key[2..] : key;
        return byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out value);
    }
}
