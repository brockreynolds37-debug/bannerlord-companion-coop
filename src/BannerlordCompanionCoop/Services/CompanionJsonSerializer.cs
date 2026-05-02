using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BannerlordCompanionCoop.Services;

internal static class CompanionJsonSerializer
{
    public static T Deserialize<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON payload cannot be empty.", nameof(json));
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using MemoryStream stream = new(bytes);
        DataContractJsonSerializer serializer = new(typeof(T));
        return (T)(serializer.ReadObject(stream)
            ?? throw new InvalidOperationException($"JSON payload could not be deserialized as {typeof(T).Name}."));
    }

    public static string Serialize<T>(T value) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        using MemoryStream stream = new();
        DataContractJsonSerializer serializer = new(typeof(T));
        serializer.WriteObject(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
