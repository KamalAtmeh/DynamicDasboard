using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;
using OfficeOpenXml;

namespace DynamicDasboardWebAPI.Services
{
    public class BatchProcessingService
    {
        private readonly NlQueryService _nlQueryService;
        private readonly BatchProcessingRepository _batchProcessingRepository;

        public BatchProcessingService(
            NlQueryService nlQueryService,
            BatchProcessingRepository batchProcessingRepository)
        {
            _nlQueryService = nlQueryService;
            _batchProcessingRepository = batchProcessingRepository;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> ProcessQuestionsFile(Stream fileStream, string dbType, int? userId = null)
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];

            // Define column indexes
            const int questionCol = 1;  // Column A
            const int sqlCol = 2;       // Column B
            const int statusCol = 3;    // Column C
            const int errorCol = 4;     // Column D

            // Find the last row with data
            int rowCount = worksheet.Dimension?.Rows ?? 0;

            // Log batch job
            int totalQuestions = 0;
            int successCount = 0;
            string fileName = "BatchProcessing_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

            int batchId = await _batchProcessingRepository.LogBatchJobAsync(
                fileName,
                totalQuestions, // Will update later
                successCount,   // Will update later
                userId
            );

            // Process each row
            for (int row = 2; row <= rowCount; row++) // Assuming row 1 is header
            {
                string question = worksheet.Cells[row, questionCol].Text;

                if (string.IsNullOrWhiteSpace(question))
                    continue;

                totalQuestions++;

                try
                {
                    // Process the question
                    var request = new NlQueryRequest
                    {
                        Question = question,
                        DatabaseType = dbType // Use database type instead of connection string
                    };

                    var response = await _nlQueryService.ProcessNaturalLanguageQueryAsync(request);

                    // Write results back to Excel
                    worksheet.Cells[row, sqlCol].Value = response.GeneratedSql;
                    worksheet.Cells[row, statusCol].Value = response.Success ? "Success" : "Error";

                    if (response.Success)
                    {
                        successCount++;
                    }

                    if (!response.Success && !string.IsNullOrEmpty(response.ErrorMessage))
                    {
                        worksheet.Cells[row, errorCol].Value = response.ErrorMessage;
                    }

                    // Log batch detail
                    await _batchProcessingRepository.LogBatchDetailAsync(
                        batchId,
                        question,
                        response.GeneratedSql,
                        response.Success,
                        response.Success ? null : response.ErrorMessage
                    );
                }
                catch (Exception ex)
                {
                    worksheet.Cells[row, statusCol].Value = "Error";
                    worksheet.Cells[row, errorCol].Value = ex.Message;

                    // Log error
                    await _batchProcessingRepository.LogBatchDetailAsync(
                        batchId,
                        question,
                        null,
                        false,
                        ex.Message
                    );
                }
            }

            // Update batch job with final counts
            // In a real implementation, we'd update the database record

            // Return the modified Excel file
            return package.GetAsByteArray();
        }

        public byte[] GenerateTemplateFile()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Questions");

            // Add headers
            worksheet.Cells[1, 1].Value = "Question";
            worksheet.Cells[1, 2].Value = "Generated SQL";
            worksheet.Cells[1, 3].Value = "Status";
            worksheet.Cells[1, 4].Value = "Error Message";

            // Format headers
            using (var range = worksheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Set column widths
            worksheet.Column(1).Width = 50;  // Question
            worksheet.Column(2).Width = 70;  // SQL
            worksheet.Column(3).Width = 15;  // Status
            worksheet.Column(4).Width = 30;  // Error

            return package.GetAsByteArray();
        }

        public byte[] Generate50TestQuestionsFile()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Test Questions");

            // Add headers
            worksheet.Cells[1, 1].Value = "Question";
            worksheet.Cells[1, 2].Value = "Generated SQL";
            worksheet.Cells[1, 3].Value = "Status";
            worksheet.Cells[1, 4].Value = "Error Message";

            // Format headers
            using (var range = worksheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Add the 50 test questions
            string[] questions = Get50TestQuestions();
            for (int i = 0; i < questions.Length; i++)
            {
                worksheet.Cells[i + 2, 1].Value = questions[i];
            }

            // Set column widths
            worksheet.Column(1).Width = 50;  // Question
            worksheet.Column(2).Width = 70;  // SQL
            worksheet.Column(3).Width = 15;  // Status
            worksheet.Column(4).Width = 30;  // Error

            return package.GetAsByteArray();
        }

        private string[] Get50TestQuestions()
        {
            // Return the list of 50 test questions
            return new string[]
            {
                "Show me the top 10 customers by total order value in the last 6 months",
                "Which products have less than 10 items in stock and need to be reordered?",
                "Calculate the average order value grouped by customer country",
                "List all orders that contain products from at least 3 different categories",
                "Show me the customer who has spent the most in each product category",
                "Which employees have processed more than 100 orders in Q1 2023?",
                "Find all products that have been returned more than 5 times in the past year",
                "What is the monthly revenue trend for the last 12 months?",
                "Show me customers who have placed orders in consecutive months",
                "Identify products that have never been ordered",
                "Calculate the profit margin for each product category",
                "List customers who have spent more than $5000 but haven't made a purchase in the last 3 months",
                "Rank warehouses by available inventory value",
                "Which marketing campaigns had an ROI greater than 200%?",
                "Find all customers who purchased Product X but not Product Y",
                "What percentage of orders are shipped within 24 hours of being placed?",
                "Show me the top 5 most frequently purchased product pairs",
                "Calculate the average customer lifetime value by registration month",
                "Identify products whose sales have increased by more than 20% month-over-month",
                "List employees who have never processed an order with a return",
                "What is the distribution of order values across different payment methods?",
                "Find customers whose average order value has decreased in the last 3 months",
                "Which products have the highest variance in order quantity?",
                "Show me the suppliers with the longest average lead time for product delivery",
                "List all products with price higher than the average price in their category",
                "Find customers who have written reviews for products they haven't purchased",
                "What's the correlation between product price and customer rating?",
                "Identify products that are frequently purchased together with promotional items",
                "Calculate the percentage of customers who made a second purchase within 30 days of their first order",
                "Show me employees with the highest average order value per transaction",
                "List all customers who have used all available payment methods",
                "Which product categories have the highest customer retention rate?",
                "Find orders where the shipping cost is more than 15% of the order value",
                "Identify customers who consistently place orders on the same day of the week",
                "Show me the most popular products by age group (based on customer birth date)",
                "Calculate the average time between consecutive orders for each customer",
                "Which promotional campaigns resulted in the highest new customer acquisition?",
                "List all products where the inventory turnover rate is less than the category average",
                "Find customers who have complained about shipping delays but have never returned a product",
                "What is the trend of average product rating over time?",
                "Show me products that are often purchased as gifts (based on different shipping and billing addresses)",
                "Identify customers whose purchasing behavior changed significantly after a specific marketing campaign",
                "Calculate the optimal reorder point for each product based on historical sales and lead time",
                "List products that have been viewed more than 100 times but purchased less than 5 times",
                "Find all orders where the customer spent more than their average order value",
                "Show me product categories with the highest seasonal variation in sales",
                "Calculate the customer churn rate by month for the past year",
                "Identify employees whose sales performance has consistently improved quarter over quarter",
                "Which shipping carriers have the lowest rate of delivery delays?",
                "Find customers who have spent more than the average in each product category they've purchased from"
            };
        }
    }
}