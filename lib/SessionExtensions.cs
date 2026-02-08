using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Marketplace.Lib
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var str = session.GetString(key);
            return str == null ? default : JsonSerializer.Deserialize<T>(str);
        }
    }
}
