using System.Text.Json;
using GameLibrary.Models;

namespace GameLibrary.Services.Json;

public class JsonService : IJsonService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region Encrypt

    public void EncryptData<T>(T data, string filePath) where T : BaseModel
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public void EncryptFile<T>(T data, string filePath)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    #endregion

    #region Decrypt

    public T? DecryptData<T>(string filePath) where T : BaseModel
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public T? DecryptFile<T>(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    #endregion

    #region Clone

    public T? Clone<T>(T obj) where T : BaseModel
    {
        obj.Guid = Guid.NewGuid();
        var json = JsonSerializer.Serialize(obj, Options);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    #endregion
}
