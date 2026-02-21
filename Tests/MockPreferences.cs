using System.Collections.Generic;

namespace Microsoft.Maui.Storage;

/// <summary>
/// Mock implementation of MAUI Preferences for testing.
/// Uses in-memory storage instead of platform-specific persistence.
/// </summary>
public static class Preferences
{
	private static Dictionary<string, object> _storage = new Dictionary<string, object>();

	public static string Get(string key, string defaultValue)
	{
		if (_storage.TryGetValue(key, out var value) && value is string stringValue)
		{
			return stringValue;
		}
		return defaultValue;
	}

	public static int Get(string key, int defaultValue)
	{
		if (_storage.TryGetValue(key, out var value))
		{
			if (value is int intValue)
				return intValue;
			if (value is string stringValue && int.TryParse(stringValue, out int parsed))
				return parsed;
		}
		return defaultValue;
	}

	public static bool Get(string key, bool defaultValue)
	{
		if (_storage.TryGetValue(key, out var value))
		{
			if (value is bool boolValue)
				return boolValue;
			if (value is string stringValue && bool.TryParse(stringValue, out bool parsed))
				return parsed;
		}
		return defaultValue;
	}

	public static void Set(string key, string value)
	{
		_storage[key] = value;
	}

	public static void Set(string key, int value)
	{
		_storage[key] = value;
	}

	public static void Set(string key, bool value)
	{
		_storage[key] = value;
	}

	public static void Remove(string key)
	{
		_storage.Remove(key);
	}

	public static bool ContainsKey(string key)
	{
		return _storage.ContainsKey(key);
	}

	public static void Clear()
	{
		_storage.Clear();
	}
}
