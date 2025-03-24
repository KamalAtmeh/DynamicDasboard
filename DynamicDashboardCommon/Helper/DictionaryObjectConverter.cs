using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDashboardCommon.Helper
{
    /// <summary>
    /// Custom JsonConverter for Dictionary<string, object> to handle the complex deserialization.
    /// </summary>
    public class DictionaryObjectConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected object, got {reader.TokenType}");
            }

            var dictionary = new Dictionary<string, object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected property name, got {reader.TokenType}");
                }

                var propertyName = reader.GetString();

                reader.Read();
                object value = ReadValue(ref reader, options);

                if (propertyName != null)
                {
                    dictionary[propertyName] = value;
                }
            }

            return dictionary;
        }

        private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int intValue))
                        return intValue;
                    if (reader.TryGetInt64(out long longValue))
                        return longValue;
                    if (reader.TryGetDecimal(out decimal decimalValue))
                        return decimalValue;
                    return reader.GetDouble();
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    // Try parsing datetime
                    if (DateTime.TryParse(stringValue, out DateTime dateTime))
                        return dateTime;
                    return stringValue;
                case JsonTokenType.StartObject:
                    var nestedDictionary = new Dictionary<string, object>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            return nestedDictionary;

                        if (reader.TokenType != JsonTokenType.PropertyName)
                            throw new JsonException();

                        var propertyName = reader.GetString();
                        reader.Read();
                        nestedDictionary[propertyName] = ReadValue(ref reader, options);
                    }
                    return nestedDictionary;
                case JsonTokenType.StartArray:
                    var list = new List<object>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            return list;

                        list.Add(ReadValue(ref reader, options));
                    }
                    return list;
                default:
                    throw new JsonException($"Unsupported token type: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
