namespace DynamicDashboardCommon.Models
{
    public class TestCasesImportRequest
    {
        public int DatabaseId { get; set; }
        public List<TestCaseItem> TestCases { get; set; }
    }

    public class TestCaseItem
    {
        public string Question { get; set; }
        public string ExpectedSql { get; set; }
        public string ExpectedExplanation { get; set; }
        public string ComplexityLevel { get; set; }
        public string QueryCategory { get; set; }
    }
}
