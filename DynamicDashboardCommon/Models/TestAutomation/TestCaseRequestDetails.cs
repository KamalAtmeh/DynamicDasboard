using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    public class TestCasesImportRequest
    {
        [JsonPropertyName("databaseId")]
        public int DatabaseId { get; set; }

        [JsonPropertyName("testCases")]
        public List<TestCaseItem> TestCases { get; set; }
    }

    public class TestCaseItem
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }
        public string ExpectedSql { get; set; }
        public string ExpectedExplanation { get; set; }
        public string ComplexityLevel { get; set; }
        public string QueryCategory { get; set; }
    }
}
