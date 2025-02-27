using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    public class QueryIntent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Template { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> Examples { get; set; } = new List<string>();
    }

    public class QueryOperation
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Template { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
    }

    public class QueryParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string EntityType { get; set; } // table, column, value, etc.
    }

    public class NlQueryResponse
    {
        public string FormattedQuestion { get; set; }
        public string GeneratedSql { get; set; }
        public List<Dictionary<string, object>> Results { get; set; }
        public string Explanation { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> SuggestedQuestions { get; set; }
        public TemplateMatchInfo TemplateInfo { get; set; }
    }

    //public class NlQueryRequest
    //{
    //    public int DatabaseId { get; set; }
    //    public string Question { get; set; }

    //    public string DatabaseType { get; set; } // Changed from DatabaseType to DbType
    //}

    public class TemplateMatchInfo
    {
        public string Intent { get; set; }
        public List<string> Operations { get; set; } = new List<string>();
        public List<QueryParameter> Parameters { get; set; } = new List<QueryParameter>();
        public double ConfidenceScore { get; set; }
    }

    //public class DeepSeekResponse
    //{
    //    [JsonPropertyName("choices")]
    //    public Choice[] choices { get; set; }
    //}

    //public class Choice
    //{
    //    [JsonPropertyName("message")]
    //    public Message message { get; set; }
    //}

    //public class Message
    //{
    //    [JsonPropertyName("content")]
    //    public string content { get; set; }
    //}
}