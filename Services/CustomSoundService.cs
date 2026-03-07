using System.Text.Json;

namespace BluetoothMicrophoneApp.Services;

public class CustomSound
{
    public string Name { get; set; } = "";
    public string BasePreset { get; set; } = "clean";
    public float Bass { get; set; }
    public float Mid { get; set; }
    public float Treble { get; set; }
    public float Distortion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class CustomSoundService
{
    private const string StorageKey = "custom_sounds";

    public static List<CustomSound> LoadAll()
    {
        try
        {
            var json = Preferences.Get(StorageKey, "[]");
            return JsonSerializer.Deserialize<List<CustomSound>>(json) ?? new List<CustomSound>();
        }
        catch
        {
            return new List<CustomSound>();
        }
    }

    public static void Save(CustomSound sound)
    {
        var sounds = LoadAll();
        // Replace if same name exists
        sounds.RemoveAll(s => s.Name.Equals(sound.Name, StringComparison.OrdinalIgnoreCase));
        sounds.Add(sound);
        var json = JsonSerializer.Serialize(sounds);
        Preferences.Set(StorageKey, json);
    }

    public static void Delete(string name)
    {
        var sounds = LoadAll();
        sounds.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var json = JsonSerializer.Serialize(sounds);
        Preferences.Set(StorageKey, json);
    }
}
