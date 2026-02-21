using BluetoothMicrophoneApp.Models;
using System.Text.Json;

namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Manages saved sound presets for users
/// </summary>
public class SavedSoundsManager
{
	private const string SavedSoundsPrefix = "saved_sounds_";

	/// <summary>
	/// Get all saved sounds for a user
	/// </summary>
	public static List<SavedSound> GetSavedSounds(string userId)
	{
		System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Getting saved sounds for user: {userId}");

		try
		{
			var key = SavedSoundsPrefix + userId;
			var json = Preferences.Get(key, string.Empty);

			if (string.IsNullOrWhiteSpace(json))
			{
				System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] No saved sounds found");
				return new List<SavedSound>();
			}

			var sounds = JsonSerializer.Deserialize<List<SavedSound>>(json);
			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Loaded {sounds?.Count ?? 0} saved sounds");

			return sounds ?? new List<SavedSound>();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Error loading saved sounds: {ex.Message}");
			return new List<SavedSound>();
		}
	}

	/// <summary>
	/// Save a sound preset
	/// </summary>
	public static void SaveSound(SavedSound sound, string userId)
	{
		System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Saving sound: {sound.Name} for user: {userId}");

		try
		{
			var sounds = GetSavedSounds(userId);

			// Check if sound with same ID already exists (update)
			var existing = sounds.FirstOrDefault(s => s.Id == sound.Id);
			if (existing != null)
			{
				sounds.Remove(existing);
				System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Updating existing sound");
			}

			sounds.Add(sound);

			var key = SavedSoundsPrefix + userId;
			var json = JsonSerializer.Serialize(sounds);
			Preferences.Set(key, json);

			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Sound saved successfully. Total sounds: {sounds.Count}");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Error saving sound: {ex.Message}");
		}
	}

	/// <summary>
	/// Delete a saved sound
	/// </summary>
	public static void DeleteSound(string soundId, string userId)
	{
		System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Deleting sound: {soundId} for user: {userId}");

		try
		{
			var sounds = GetSavedSounds(userId);
			var sound = sounds.FirstOrDefault(s => s.Id == soundId);

			if (sound != null)
			{
				sounds.Remove(sound);

				var key = SavedSoundsPrefix + userId;
				var json = JsonSerializer.Serialize(sounds);
				Preferences.Set(key, json);

				System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Sound deleted. Remaining sounds: {sounds.Count}");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Sound not found");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Error deleting sound: {ex.Message}");
		}
	}

	/// <summary>
	/// Update last used timestamp for a sound
	/// </summary>
	public static void MarkSoundAsUsed(string soundId, string userId)
	{
		System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Marking sound as used: {soundId}");

		try
		{
			var sounds = GetSavedSounds(userId);
			var sound = sounds.FirstOrDefault(s => s.Id == soundId);

			if (sound != null)
			{
				sound.LastUsedAt = DateTime.Now;

				var key = SavedSoundsPrefix + userId;
				var json = JsonSerializer.Serialize(sounds);
				Preferences.Set(key, json);

				System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Sound usage timestamp updated");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[SavedSoundsManager] Error updating sound usage: {ex.Message}");
		}
	}
}
