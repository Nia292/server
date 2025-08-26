namespace SinusSynchronousShared.Utils.Configuration;

public interface ISinusConfiguration
{
    T GetValueOrDefault<T>(string key, T defaultValue);
    T GetValue<T>(string key);
    string SerializeValue(string key, string defaultValue);
}
