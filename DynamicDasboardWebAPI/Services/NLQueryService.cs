using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicDashboardCommon.Models;
using DynamicDasboardWebAPI.Repositories;
using DynamicDasboardWebAPI.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DynamicDasboardWebAPI.Services
{
    public class NlQueryService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly DatabaseService _databaseService;
        private readonly NlQueryRepository _nlQueryRepository;
        private readonly List<QueryIntent> _intents;
        private readonly List<QueryOperation> _operations;

        public NlQueryService(
            IConfiguration config,
            HttpClient httpClient,
            DatabaseService databaseService,
            NlQueryRepository nlQueryRepository)
        {
            _config = config;
            _httpClient = httpClient;
            _databaseService = databaseService;
            _nlQueryRepository = nlQueryRepository;

            // Initialize templates
            _intents = InitializeIntents();
            _operations = InitializeOperations();
        }

        private List<QueryIntent> InitializeIntents()
        {
            return new List<QueryIntent>
            {
                new QueryIntent
                {
                    Name = "retrieve",
                    Description = "Get records from the database",
                    Template = "SELECT {columns} FROM {table}",
                    Keywords = new List<string> { "show", "get", "display", "list", "find", "retrieve", "what", "which" },
                    Examples = new List<string>
                    {
                        "Show me all customers",
                        "List all products with price greater than $100"
                    }
                },
                new QueryIntent
                {
                    Name = "count",
                    Description = "Count records in the database",
                    Template = "SELECT COUNT({column}) FROM {table}",
                    Keywords = new List<string> { "count", "how many", "number of", "total number", "quantity" },
                    Examples = new List<string>
                    {
                        "How many orders were placed last month?",
                        "Count the number of customers in each country"
                    }
                },
                new QueryIntent
                {
                    Name = "aggregate",
                    Description = "Calculate aggregate values",
                    Template = "SELECT {agg_func}({column}) FROM {table}",
                    Keywords = new List<string> { "sum", "average", "total", "maximum", "minimum", "avg", "min", "max" },
                    Examples = new List<string>
                    {
                        "What is the total sales amount for each product?",
                        "Find the average order value by customer"
                    }
                },
                new QueryIntent
                {
                    Name = "rank",
                    Description = "Rank records by a criteria",
                    Template = "SELECT TOP {limit} * FROM {table} ORDER BY {column} {direction}",
                    Keywords = new List<string> { "top", "best", "worst", "highest", "lowest", "most", "least", "ranking" },
                    Examples = new List<string>
                    {
                        "Show me the top 10 customers by total order value",
                        "What are the 5 best-selling products?"
                    }
                },
                new QueryIntent
                {
                    Name = "compare",
                    Description = "Compare different groups of data",
                    Template = "SELECT {group_column}, {agg_func}({value_column}) FROM {table} GROUP BY {group_column}",
                    Keywords = new List<string> { "compare", "difference", "versus", "vs", "against", "comparison" },
                    Examples = new List<string>
                    {
                        "Compare sales by month for this year vs last year",
                        "What's the difference in order volume between our top 3 products?"
                    }
                }
            };
        }

        private List<QueryOperation> InitializeOperations()
        {
            return new List<QueryOperation>
            {
                new QueryOperation
                {
                    Name = "filter",
                    Description = "Filter records based on conditions",
                    Template = "WHERE {condition}",
                    Keywords = new List<string> { "where", "with", "filter", "only", "if", "when", "having" }
                },
                new QueryOperation
                {
                    Name = "group",
                    Description = "Group records by attributes",
                    Template = "GROUP BY {columns}",
                    Keywords = new List<string> { "group by", "grouped by", "for each", "by", "per" }
                },
                new QueryOperation
                {
                    Name = "sort",
                    Description = "Sort records by attributes",
                    Template = "ORDER BY {columns} {direction}",
                    Keywords = new List<string> { "order by", "ordered by", "sort by", "sorted by", "in order" }
                },
                new QueryOperation
                {
                    Name = "limit",
                    Description = "Limit the number of records",
                    Template = "TOP {number}",
                    Keywords = new List<string> { "limit", "top", "first", "only", "just" }
                },
                new QueryOperation
                {
                    Name = "join",
                    Description = "Join multiple tables",
                    Template = "JOIN {table} ON {condition}",
                    Keywords = new List<string> { "join", "with", "and", "related", "linked", "connected" }
                },
                new QueryOperation
                {
                    Name = "time_filter",
                    Description = "Filter by time periods",
                    Template = "WHERE {date_column} BETWEEN '{start_date}' AND '{end_date}'",
                    Keywords = new List<string> { "last month", "this year", "previous quarter", "ytd", "mtd", "between" }
                }
            };
        }

        public async Task<NlQueryResponse> ProcessNaturalLanguageQueryAsync(NlQueryRequest request)
        {
            try
            {
                // 1. Get database schema
                var database = await _databaseService.GetDatabaseByIdAsync(request.DatabaseId);
                if (database == null)
                {
                    return new NlQueryResponse
                    {
                        Success = false,
                        ErrorMessage = "Database not found"
                    };
                }

                var schemaData = await _nlQueryRepository.GetDatabaseMetadataAsync(request.DatabaseId);
                var schema = FormatSchemaFromMetadata(schemaData);

                // 2. Call LLM to analyze the question and match templates
                var templateMatch = await MatchTemplatesAsync(request.Question, schema);

                if (templateMatch == null || string.IsNullOrEmpty(templateMatch.Intent))
                {
                    return new NlQueryResponse
                    {
                        Success = false,
                        ErrorMessage = "Could not understand the question",
                        SuggestedQuestions = GenerateSuggestedQuestions(schema)
                    };
                }

                // 3. Generate SQL from the template match
                var sql = await GenerateSqlAsync(templateMatch, schema);

                if (string.IsNullOrEmpty(sql))
                {
                    return new NlQueryResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to generate SQL query",
                        TemplateInfo = templateMatch
                    };
                }

                // 4. Execute the query
                // In ProcessNaturalLanguageQueryAsync method:

                // Execute the query on the requested database
                var results = await _nlQueryRepository.ExecuteQueryOnDatabaseAsync(sql, request.DatabaseId);

                // 5. Generate explanation
                var explanation = await GenerateExplanationAsync(request.Question, sql, results);

                //// 6. Log the query for auditing
                //int logId = await _nlQueryRepository.LogNlQueryAsync(
                //    request.Question,
                //    sql,
                //    null, // UserId (we could get this from the controller if available)
                //    request.DatabaseId
                //);

                //// 7. Save the template match for reference
                //await _nlQueryRepository.SaveTemplateMatchAsync(templateMatch, logId);
                var (viewingTypeId, viewingTypeName, formattedResult) = DetermineDataViewingType(results, sql);
                return new NlQueryResponse
                {
                    FormattedQuestion = FormatQuestion(templateMatch),
                    GeneratedSql = sql,
                    Results = results,
                    Explanation = explanation,
                    Success = true,
                    TemplateInfo = templateMatch,
                    RecommendedDataViewingTypeID = viewingTypeId,
                    RecommendedDataViewingTypeName = viewingTypeName,
                    FormattedResult = formattedResult
                };
            }
            catch (Exception ex)
            {
                return new NlQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"An error occurred: {ex.Message}"
                };
            }
        }

        private string FormatSchemaFromMetadata(Dictionary<string, object> metadata)
        {
            // In a real implementation, this would format the metadata into a string representation
            // For now, we'll use the simplified schema

            return @"
Tables:
- Customers(CustomerID, FirstName, LastName, Email, Phone, Address, City, State, ZipCode, Country, RegistrationDate, LoyaltyPoints, CustomerRating, IsActive)
- ProductCategories(CategoryID, CategoryName, ParentCategoryID, Description, ImageURL)
- Products(ProductID, ProductName, SKU, CategoryID, Description, UnitPrice, DiscountPercentage, StockQuantity, ReorderLevel, Weight, Dimensions, ImageURL, IsActive, DateAdded, ManufacturerID)
- ProductAttributes(AttributeID, ProductID, AttributeName, AttributeValue)
- Manufacturers(ManufacturerID, ManufacturerName, ContactName, ContactEmail, ContactPhone, Address, Website)
- Suppliers(SupplierID, SupplierName, ContactName, ContactEmail, ContactPhone, Address, PaymentTerms, LeadTimeDays, IsActive, Rating)
- ProductSuppliers(ProductID, SupplierID, SupplierPrice, MinOrderQuantity)
- Warehouses(WarehouseID, WarehouseName, Address, City, State, ZipCode, Country, ManagerID, Capacity)
- Inventory(InventoryID, ProductID, WarehouseID, Quantity, LastUpdated, ShelfLocation)
- Employees(EmployeeID, FirstName, LastName, Email, Phone, HireDate, Position, DepartmentID, ManagerID, Salary, Address, IsActive)
- Departments(DepartmentID, DepartmentName, Description, ManagerID, Location, Budget)
- Orders(OrderID, CustomerID, OrderDate, ShippingMethod, ShippingAddress, ShippingCity, ShippingState, ShippingZipCode, ShippingCountry, OrderStatus, PaymentStatus, TrackingNumber, TotalAmount, DiscountAmount, TaxAmount, ShippingAmount, EmployeeID, Notes)
- OrderItems(OrderItemID, OrderID, ProductID, Quantity, UnitPrice, Discount)
- Payments(PaymentID, OrderID, PaymentDate, PaymentMethod, Amount, TransactionID, Status)
- Shipping(ShippingID, OrderID, ShippingDate, Carrier, TrackingNumber, EstimatedDelivery, ActualDelivery, ShippingCost, Status)
- Returns(ReturnID, OrderID, ProductID, Quantity, ReturnDate, Reason, Status, RefundAmount)
- ProductReviews(ReviewID, ProductID, CustomerID, Rating, ReviewText, ReviewDate, IsVerifiedPurchase, Helpful)
- MarketingCampaigns(CampaignID, CampaignName, Description, StartDate, EndDate, Budget, ActualCost, TargetAudience, Goals, ROI, Status)
- Promotions(PromotionID, PromotionName, Description, DiscountType, DiscountValue, StartDate, EndDate, MinOrderAmount, MaxUsages, CurrentUsages, PromoCode, IsActive)
- UserActivityLogs(LogID, UserID, UserType, ActivityType, ActivityDescription, IPAddress, UserAgent, ActivityTimestamp, RelatedEntityID, RelatedEntityType)

Relationships:
- Customers.CustomerID -> Orders.CustomerID
- Products.CategoryID -> ProductCategories.CategoryID
- Products.ManufacturerID -> Manufacturers.ManufacturerID
- ProductAttributes.ProductID -> Products.ProductID
- ProductSuppliers.ProductID -> Products.ProductID
- ProductSuppliers.SupplierID -> Suppliers.SupplierID
- Warehouses.ManagerID -> Employees.EmployeeID
- Inventory.ProductID -> Products.ProductID
- Inventory.WarehouseID -> Warehouses.WarehouseID
- Employees.DepartmentID -> Departments.DepartmentID
- Employees.ManagerID -> Employees.EmployeeID
- Departments.ManagerID -> Employees.EmployeeID
- Orders.CustomerID -> Customers.CustomerID
- Orders.EmployeeID -> Employees.EmployeeID
- OrderItems.OrderID -> Orders.OrderID
- OrderItems.ProductID -> Products.ProductID
- Payments.OrderID -> Orders.OrderID
- Shipping.OrderID -> Orders.OrderID
- Returns.OrderID -> Orders.OrderID
- Returns.ProductID -> Products.ProductID
- ProductReviews.ProductID -> Products.ProductID
- ProductReviews.CustomerID -> Customers.CustomerID
";
        }

        private async Task<TemplateMatchInfo> MatchTemplatesAsync(string question, string schema)
        {
            var systemMessage = $@"
You are a database query analyzer. Your task is to analyze a natural language question and identify the intent, operations, and parameters.

Database Schema:
{schema}

Available intents:
{System.Text.Json.JsonSerializer.Serialize(_intents, new JsonSerializerOptions { WriteIndented = true })}

Available operations:
{System.Text.Json.JsonSerializer.Serialize(_operations, new JsonSerializerOptions { WriteIndented = true })}

For the given question, analyze:
1. Which intent best matches the question's primary goal
2. Which operations are needed to satisfy the question
3. What parameters need to be extracted (tables, columns, values, etc.)

Return your analysis as a JSON object with the following structure:
{{
  ""intent"": ""name_of_intent"",
  ""operations"": [""operation1"", ""operation2"", ...],
  ""parameters"": [
    {{ ""name"": ""param_name"", ""value"": ""param_value"", ""entityType"": ""table|column|value|etc"" }},
    ...
  ],
  ""confidenceScore"": 0.95
}}";

            var userMessage = $"Question: {question}";

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.1,
                max_tokens = 500
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<DeepSeekResponse>(content);

                var jsonMatch = result?.choices[0].message.content;

                return Newtonsoft.Json.JsonConvert.DeserializeObject<TemplateMatchInfo>(jsonMatch,
        new Newtonsoft.Json.JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                Console.WriteLine($"Deserialization Error: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            },
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore
        });


                //  return JsonSerializer.Deserialize<TemplateMatchInfo>(jsonMatch);
            }

            return null;
        }

        private async Task<string> GenerateSqlAsync(TemplateMatchInfo templateMatch, string schema)
        {
            var intent = _intents.FirstOrDefault(i => i.Name == templateMatch.Intent);
            if (intent == null) return null;

            var operations = _operations
                .Where(o => templateMatch.Operations.Contains(o.Name))
                .ToList();

            var systemMessage = $@"
You are a SQL expert. Your task is to generate SQL code based on a template match and database schema.

Database Schema:
{schema}

Intent: {intent.Name} - {intent.Description}
Intent SQL Template: {intent.Template}

Operations:
{System.Text.Json.JsonSerializer.Serialize(operations, new JsonSerializerOptions { WriteIndented = true })}

Parameters:
{System.Text.Json.JsonSerializer.Serialize(templateMatch.Parameters, new JsonSerializerOptions { WriteIndented = true })}

Rules:
1. Generate a valid SQL query using the intent and operations as guidance
2. Use the parameters to fill in specific values
3. Ensure the SQL is valid for SQL Server syntax
4. Include appropriate JOINs when multiple tables are involved
5. The query should be as simple as possible while still accurately answering the question
6. Return ONLY the SQL query without any additional text or explanation";

            var userMessage = "Generate the SQL query based on the template and parameters.";

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                temperature = 0,
                max_tokens = 500
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<DeepSeekResponse>(content);

                var sqlresult = result?.choices[0].message.content.Trim();
                if (sqlresult != null)
                {
                    return QueryBuilder.ExtractSqlQueryFromMarkdown(sqlresult);
                }
            }

            return null;
        }

        private async Task<string> GenerateExplanationAsync(string question, string sql, List<Dictionary<string, object>> results)
        {
            var systemMessage = @"
You are a database expert explaining SQL queries to non-technical users.
Given the following question, SQL query, and results, provide a concise, business-friendly explanation.
Focus on what insights the query reveals and why it might be useful.
Keep your explanation to 2-3 sentences, avoiding technical jargon when possible.";

            var userMessage = $@"
Question: {question}

SQL Query:
{sql}

Query Results (first few rows):
{System.Text.Json.JsonSerializer.Serialize(results.Take(3), new JsonSerializerOptions { WriteIndented = true })}

Number of rows returned: {results.Count}";

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                temperature = 0,
                max_tokens = 500
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<DeepSeekResponse>(content);

                return result?.choices[0].message.content.Trim();
            }

            return "This query shows the requested data from the database.";
        }

        private string FormatQuestion(TemplateMatchInfo templateMatch)
        {
            var intent = _intents.FirstOrDefault(i => i.Name == templateMatch.Intent);
            if (intent == null || intent.Examples.Count == 0) return null;

            // Get a random example from the intent
            var example = intent.Examples[new Random().Next(intent.Examples.Count)];

            // In a real implementation, we would actually format the question based on the template
            // and parameters, but for simplicity, we'll just return the example
            return example;
        }

        private List<string> GenerateSuggestedQuestions(string schema)
        {
            // In a real implementation, this would generate questions based on the schema
            // For now, we'll return some sample questions
            return new List<string>
            {
                "Show me the top 10 customers by total order value",
                "What is the average order value by product category?",
                "How many orders were placed last month?",
                "List all products with less than 10 items in stock",
                "Which products have the highest customer ratings?"
            };
        }

        private string _apiKey => _config["DeepSeek:ApiKey"];
        private string _endpoint => _config["DeepSeek:Endpoint"];

        /// <summary>
        /// Determines the most appropriate viewing type for the query results
        /// </summary>
        /// <param name="results">The query results</param>
        /// <param name="query">The original SQL query</param>
        /// <returns>Recommended viewing type ID and name</returns>
        private (int ViewingTypeId, string ViewingTypeName, string FormattedResult) DetermineDataViewingType(
            List<Dictionary<string, object>> results,
            string query)
        {
            // If no results, default to table
            if (results == null || results.Count == 0)
            {
                return ((int)DataViewingTypeEnum.Table, "Table", null);
            }

            // Single result with single column might be a label or number
            if (results.Count == 1 && results[0].Count == 1)
            {
                var singleValue = results[0].Values.First();

                // Check for numeric types
                if (singleValue is int intVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(intVal));
                }
                else if (singleValue is decimal decVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(decVal));
                }
                else if (singleValue is double doubleVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(doubleVal));
                }
                else if (singleValue is float floatVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(floatVal));
                }
                else if (singleValue is long longVal)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(longVal));
                }
                else
                {
                    // For other single values, use label
                    return ((int)DataViewingTypeEnum.Label, "Label", singleValue?.ToString());
                }
            }

            // Aggregate queries might need special handling
            if (IsAggregateQuery(query))
            {
                // Check if aggregate result is numeric
                var sampleValue = results[0].Values.First();
                if (sampleValue is int || sampleValue is decimal ||
                    sampleValue is double || sampleValue is float ||
                    sampleValue is long)
                {
                    return ((int)DataViewingTypeEnum.Number, "Number", FormatNumber(Convert.ToDecimal(sampleValue)));
                }
            }

            // Default to table for complex or multi-column results
            return ((int)DataViewingTypeEnum.Table, "Table", null);
        }

        /// <summary>
        /// Checks if the query is an aggregate query
        /// </summary>
        private bool IsAggregateQuery(string query)
        {
            query = query.ToLowerInvariant();
            return query.Contains("count(") ||
                   query.Contains("sum(") ||
                   query.Contains("avg(") ||
                   query.Contains("max(") ||
                   query.Contains("min(");
        }

        /// <summary>
        /// Formats numbers with appropriate culture and precision
        /// </summary>
        private string FormatNumber(object numberValue)
        {
            try
            {
                // Convert to decimal using Convert.ToDecimal which handles multiple numeric types
                decimal number = Convert.ToDecimal(numberValue);

                // Use culture-specific formatting with two decimal places
                return number.ToString("N2", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                // If conversion fails, return the original value as string
                return numberValue?.ToString() ?? string.Empty;
            }
            catch (InvalidCastException)
            {
                // If conversion is not possible, return the original value as string
                return numberValue?.ToString() ?? string.Empty;
            }
        }

    }
}