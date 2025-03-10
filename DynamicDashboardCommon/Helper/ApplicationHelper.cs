using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DynamicDashboardCommon.Helper
{
    public static class ApplicationHelper
    {

        // Validates whether the input string is valid JSON.
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Deserializes JSON into an object of type T.
        public static T Deserialize<T>(string json)
        {
            if (IsValidJson(json))
                return JsonSerializer.Deserialize<T>(json);
            else
                throw new ArgumentException("Invalid JSON");
        }

        // Serializes an object to an indented JSON string.
        public static string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
