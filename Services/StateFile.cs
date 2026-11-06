using System.Text.Json;

namespace PPES.Services;

/// <summary>A small JSON file in the mod folder that PPES keeps between runs. A missing or unreadable one starts empty.</summary>
internal sealed class StateFile<T>(string path) where T : new()
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    public T Read()
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? new T() : new T();
        }
        catch
        {
            return new T();
        }
    }

    public void Write(T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
}
