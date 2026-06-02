using System.Text;
using System.Text.Json;

namespace WebApi.Tests;

public static class HttpHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static StringContent ToJsonContent(this object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    public static async Task<T?> ReadFromJsonAsync<T>(this HttpResponseMessage response) =>
        await JsonSerializer.DeserializeAsync<T>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);
}

public record BookBody(string Column, string TypeOfCSharp, string Nullable);
public record BookModel(int Id, string Column, string TypeOfCSharp, string Nullable) : BookBody(Column, TypeOfCSharp, Nullable);