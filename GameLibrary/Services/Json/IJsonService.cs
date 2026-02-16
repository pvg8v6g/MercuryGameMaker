using GameLibrary.Models;

namespace GameLibrary.Services.Json;

public interface IJsonService
{
    void EncryptData<T>(T data, string filePath) where T : BaseModel;

    void EncryptFile<T>(T data, string filePath);

    T? DecryptData<T>(string filePath) where T : BaseModel;

    T? DecryptFile<T>(string filePath);

    T? Clone<T>(T obj) where T : BaseModel;
}
