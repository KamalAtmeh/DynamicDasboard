using System;
using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;
using DynamicDashboardCommon.Models;
using OpenAI;
using OpenAI.Chat;
using Polly;
using Polly.Retry;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;

namespace DynamicDasboardWebAPI.Services
{
    public class QueryService
    {
        private readonly QueryRepository _repository;
        private readonly QueryLogsRepository _logsRepository;
        private readonly OpenAIClient _openAIClient;
        private readonly AsyncRetryPolicy _retryPolicy;

        public QueryService(QueryRepository repository, QueryLogsRepository logsRepository, string openAiApiKey)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logsRepository = logsRepository ?? throw new ArgumentNullException(nameof(logsRepository));
            //_openAIClient = new OpenAIClient(openAiApiKey, "test");
            //_retryPolicy = Policy
            //    .Handle<Exception>()
            //    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            //_logger = logger;
        }

        //public async Task<string> GenerateSqlQueryAsync(string question)
        //{
        //    if (string.IsNullOrWhiteSpace(question))
        //        throw new ArgumentException("Question cannot be null or empty.", nameof(question));

        //    return await _retryPolicy.ExecuteAsync(async () =>
        //    {
        //        var schemaInfo = @"
        //        Database Schema:
        //        1. Employees (EmployeeID, Name, DepartmentID, Salary, HireDate)
        //        2. Departments (DepartmentID, DepartmentName, ManagerID)
        //        3. Projects (ProjectID, ProjectName, StartDate, EndDate, Budget)
        //        4. EmployeeProjects (EmployeeID, ProjectID, HoursWorked)
        //        5. Customers (CustomerID, Name, Email, Phone)
        //        6. Orders (OrderID, CustomerID, OrderDate, TotalAmount)
        //        7. Products (ProductID, ProductName, Price, StockQuantity)
        //        8. OrderDetails (OrderID, ProductID, Quantity, Price)
        //        Relationships:
        //        - Employees.DepartmentID -> Departments.DepartmentID
        //        - Departments.ManagerID -> Employees.EmployeeID
        //        - EmployeeProjects.EmployeeID -> Employees.EmployeeID
        //        - EmployeeProjects.ProjectID -> Projects.ProjectID
        //        - Orders.CustomerID -> Customers.CustomerID
        //        - OrderDetails.OrderID -> Orders.OrderID
        //        - OrderDetails.ProductID -> Products.ProductID
        //        ";

        //        var systemMessage = $@"
        //        You are a SQL expert. Given the following database schema:
        //        {schemaInfo}

        //        Rules:
        //        1. Use proper table joins to retrieve data from multiple tables.
        //        2. Apply filters and conditions as needed.
        //        3. Use DISTINCT or GROUP BY to avoid duplicate rows.
        //        4. Use aggregate functions (e.g., SUM, COUNT, AVG) for calculations.
        //        5. Ensure the query is syntactically correct and optimized.
        //        ";

        //        var userPrompt = $"Convert the following natural language query into a valid SQL query: {question}";

        //        // Create chat request with system and user messages
        //        var chatRequest = new ChatCompletionOptions
        //        {
        //            Messages =
        //            {
        //                new ChatMessage(ChatRole.System, systemMessage),
        //                new ChatMessage(ChatRole.User, userPrompt)
        //            },
        //            MaxTokens = 300,
        //            Temperature = 0
        //        };

        //        // Execute the chat request
        //        var response = await _openAIClient.GetChatClient(
        //            deploymentOrModelName: "gpt-3.5-turbo",
        //            chatRequest
        //        );

        //        if (response?.Choices == null || response.Choices.Count == 0)
        //        {
        //            throw new Exception("No response from the OpenAI API.");
        //        }

        //        var sqlQuery = response.Choices[0].Message.Content;
        //        return ExtractSqlQuery(sqlQuery);
        //    });
        //}

        //private string ExtractSqlQuery(string response)
        //{
        //    // Use regex to extract SQL query from code blocks
        //    var match = Regex.Match(
        //        response,
        //        @"```sql\n(.*?)\n```",
        //        RegexOptions.Singleline
        //    );

        //    if (!match.Success)
        //    {
        //        // Fallback: If no code block is found, return the entire response
        //        return response.Trim();
        //    }

        //    return match.Groups[1].Value.Trim();
        //}

        public async Task<dynamic> ExecuteQueryAsync(string query, string databaseType, int? executedBy)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(databaseType))
                throw new ArgumentException("Database type must be specified.");

            var result = await _repository.ExecuteQueryAsync(query);
            await _logsRepository.LogQueryAsync(query, executedBy, databaseType, result?.ToString());
            return result;
        }
    }
}
