namespace BluetoothMicrophoneApp.Models;

/// <summary>
/// Represents a saved sound preset with custom effects and settings
/// </summary>
public class SavedSound
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public int Volume { get; set; } = 100; // 0-200%
	public string EffectName { get; set; } = "None"; // "None", "Robot", "Echo", etc.
	public Dictionary<string, object> EffectSettings { get; set; } = new();
	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public DateTime LastUsedAt { get; set; }
	public string UserId { get; set; } = string.Empty;
}
