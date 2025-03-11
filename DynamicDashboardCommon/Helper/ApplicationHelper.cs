using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;


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

        // Returns the details of an exception as a string.
        public static string GetExceptionDetails(Exception ex)
        {
            if (ex == null) return string.Empty;

            // Create a stack trace with file info
            var stackTrace = new StackTrace(ex, true);
            // Get the first frame (where exception originated)
            var frame = stackTrace.GetFrame(0);
            var fileName = frame?.GetFileName() ?? "Unknown File";
            var methodName = frame?.GetMethod()?.Name ?? "Unknown Method";

            var sb = new StringBuilder();
            sb.AppendLine("===== Exception Details =====");
            sb.AppendLine($"File: {fileName}");
            sb.AppendLine($"Method: {methodName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine("Full Stack Trace:");
            sb.AppendLine(ex.StackTrace);
            sb.AppendLine("=============================");

            return sb.ToString();

        }


    }
}
