// using System.Text.Json;

// public static class JsonUtils
// {
//     public static object? SafeJson(JsonElement element)
//     {
//         try
//         {
//             if (element.ValueKind == JsonValueKind.Undefined ||
//                 element.ValueKind == JsonValueKind.Null)
//                 return null;

//             if (element.ValueKind == JsonValueKind.String)
//                 return element.GetString();

//             return JsonSerializer.Deserialize<object>(element.GetRawText());
//         }
//         catch
//         {
//             return null;
//         }
//     }
// }
