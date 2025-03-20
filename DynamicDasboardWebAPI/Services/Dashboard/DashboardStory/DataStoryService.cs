using DynamicDashboardCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDasboardWebAPI.Services
{
    /// <summary>
    /// Service for analyzing query results and generating interactive data stories
    /// </summary>
    public class DataStoryService
    {
        /// <summary>
        /// Generates a data story from query results
        /// </summary>
        /// <param name="results">The query results to analyze</param>
        /// <param name="question">The original question that generated the results</param>
        /// <returns>A structured data story with scenes and insights</returns>
        public async Task<DataStory> GenerateDataStoryAsync(List<Dictionary<string, object>> results, string question)
        {
            if (results == null || results.Count == 0)
            {
                return new DataStory
                {
                    Success = false,
                    ErrorMessage = "No data available to analyze"
                };
            }

            try
            {
                // Analyze the data
                var columnTypes = InferColumnTypes(results);
                var insights = ExtractInsights(results, columnTypes);
                var scenes = GenerateStoryScenes(results, question, columnTypes, insights);

                return new DataStory
                {
                    Success = true,
                    Question = question,
                    Scenes = scenes,
                    Insights = insights
                };
            }
            catch (Exception ex)
            {
                return new DataStory
                {
                    Success = false,
                    ErrorMessage = $"Error generating data story: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Infers the data types of each column in the results
        /// </summary>
        /// <param name="data">The query results</param>
        /// <returns>Dictionary mapping column names to their inferred types</returns>
        private Dictionary<string, string> InferColumnTypes(List<Dictionary<string, object>> data)
        {
            if (data == null || data.Count == 0) return new Dictionary<string, string>();

            var sample = data[0];
            var types = new Dictionary<string, string>();

            // Analyze each column to determine its type
            foreach (var column in sample.Keys)
            {
                // Get all values for this column
                var values = data.Select(row => row.ContainsKey(column) ? row[column] : null).ToList();
                var nonEmptyValues = values.Where(v => v != null && v.ToString() != string.Empty).ToList();

                if (nonEmptyValues.Count == 0)
                {
                    types[column] = "unknown";
                    continue;
                }

                // Check if values are dates
                int possibleDateCount = 0;
                foreach (var v in nonEmptyValues)
                {
                    if (DateTime.TryParse(v.ToString(), out _))
                    {
                        possibleDateCount++;
                    }
                }

                if ((double)possibleDateCount / nonEmptyValues.Count > 0.7)
                {
                    types[column] = "date";
                    continue;
                }

                // Check if values are numeric
                int numberCount = 0;
                foreach (var v in nonEmptyValues)
                {
                    if (double.TryParse(v.ToString(), out _))
                    {
                        numberCount++;
                    }
                }

                if ((double)numberCount / nonEmptyValues.Count > 0.7)
                {
                    types[column] = "numeric";
                    continue;
                }

                // Check for categorical/text data
                var uniqueValues = new HashSet<string>(nonEmptyValues.Select(v => v.ToString())).Count;
                if ((double)uniqueValues / nonEmptyValues.Count < 0.3 || uniqueValues < 20)
                {
                    types[column] = "category";
                    continue;
                }

                // Default to text
                types[column] = "text";
            }

            return types;
        }

        /// <summary>
        /// Extracts insights from the data
        /// </summary>
        /// <param name="data">The query results</param>
        /// <param name="columnTypes">Dictionary mapping column names to their types</param>
        /// <returns>List of insights extracted from the data</returns>
        private List<DataInsight> ExtractInsights(List<Dictionary<string, object>> data, Dictionary<string, string> columnTypes)
        {
            var insights = new List<DataInsight>();

            // Get numeric and category columns
            var numericColumns = columnTypes.Where(ct => ct.Value == "numeric").Select(ct => ct.Key).ToList();
            var categoryColumns = columnTypes.Where(ct => ct.Value == "category").Select(ct => ct.Key).ToList();

            // For each numeric column, generate insights
            foreach (var numCol in numericColumns)
            {
                // Find maximum value
                var maxValue = data.OrderByDescending(row =>
                {
                    if (row.TryGetValue(numCol, out var val) && double.TryParse(val.ToString(), out var numVal))
                        return numVal;
                    return double.MinValue;
                }).FirstOrDefault();

                if (maxValue != null)
                {
                    string description = $"Highest {numCol}: {maxValue[numCol]}";

                    // Add category context if available
                    if (categoryColumns.Count > 0)
                    {
                        var category = categoryColumns[0];
                        if (maxValue.ContainsKey(category))
                            description += $" ({maxValue[category]})";
                    }

                    insights.Add(new DataInsight
                    {
                        Type = "max",
                        Description = description,
                        Data = JsonSerializer.Serialize(maxValue)
                    });
                }

                // Find minimum value
                var minValue = data.OrderBy(row =>
                {
                    if (row.TryGetValue(numCol, out var val) && double.TryParse(val.ToString(), out var numVal))
                        return numVal;
                    return double.MaxValue;
                }).FirstOrDefault();

                if (minValue != null)
                {
                    string description = $"Lowest {numCol}: {minValue[numCol]}";

                    // Add category context if available
                    if (categoryColumns.Count > 0)
                    {
                        var category = categoryColumns[0];
                        if (minValue.ContainsKey(category))
                            description += $" ({minValue[category]})";
                    }

                    insights.Add(new DataInsight
                    {
                        Type = "min",
                        Description = description,
                        Data = JsonSerializer.Serialize(minValue)
                    });
                }

                // Calculate average
                var validValues = data
                    .Where(row => row.ContainsKey(numCol) && double.TryParse(row[numCol].ToString(), out _))
                    .Select(row => Convert.ToDouble(row[numCol]))
                    .ToList();

                if (validValues.Count > 0)
                {
                    var avgValue = validValues.Average();

                    insights.Add(new DataInsight
                    {
                        Type = "average",
                        Description = $"Average {numCol}: {avgValue:F2}",
                        Data = JsonSerializer.Serialize(new { average = avgValue })
                    });
                }

                // Find potential outliers using IQR method
                if (validValues.Count > 5)
                {
                    var sortedValues = validValues.OrderBy(v => v).ToList();
                    var q1 = sortedValues[(int)(sortedValues.Count * 0.25)];
                    var q3 = sortedValues[(int)(sortedValues.Count * 0.75)];
                    var iqr = q3 - q1;
                    var upperBound = q3 + 1.5 * iqr;
                    var lowerBound = q1 - 1.5 * iqr;

                    var outliers = data.Where(row =>
                    {
                        if (row.TryGetValue(numCol, out var val) && double.TryParse(val.ToString(), out var numVal))
                            return numVal > upperBound || numVal < lowerBound;
                        return false;
                    }).ToList();

                    if (outliers.Count > 0)
                    {
                        insights.Add(new DataInsight
                        {
                            Type = "outlier",
                            Description = $"Found {outliers.Count} unusual values for {numCol}",
                            Data = JsonSerializer.Serialize(outliers)
                        });
                    }
                }
            }

            // Add trend insight if we have enough data and at least one numeric column
            if (data.Count > 3 && numericColumns.Count > 0)
            {
                var numCol = numericColumns[0];

                if (data[0].TryGetValue(numCol, out var firstObj) &&
                    data[data.Count - 1].TryGetValue(numCol, out var lastObj) &&
                    double.TryParse(firstObj.ToString(), out var firstValue) &&
                    double.TryParse(lastObj.ToString(), out var lastValue))
                {
                    var change = Math.Abs(firstValue) < 0.0001 ? 0 : ((lastValue - firstValue) / firstValue) * 100;
                    var trendDescription = change >= 0
                        ? $"Upward trend of {change:F1}%"
                        : $"Downward trend of {Math.Abs(change):F1}%";

                    insights.Add(new DataInsight
                    {
                        Type = "trend",
                        Description = trendDescription,
                        Data = JsonSerializer.Serialize(new { first = firstValue, last = lastValue, change })
                    });
                }
            }

            return insights;
        }

        /// <summary>
        /// Generates story scenes based on the data
        /// </summary>
        /// <param name="data">The query results</param>
        /// <param name="question">The original question</param>
        /// <param name="columnTypes">Dictionary mapping column names to their types</param>
        /// <param name="insights">List of insights extracted from the data</param>
        /// <returns>List of scenes making up the data story</returns>
        private List<StoryScene> GenerateStoryScenes(
            List<Dictionary<string, object>> data,
            string question,
            Dictionary<string, string> columnTypes,
            List<DataInsight> insights)
        {
            var scenes = new List<StoryScene>();

            // Get column types
            var numericColumns = columnTypes.Where(ct => ct.Value == "numeric").Select(ct => ct.Key).ToList();
            var categoryColumns = columnTypes.Where(ct => ct.Value == "category").Select(ct => ct.Key).ToList();
            var dateColumns = columnTypes.Where(ct => ct.Value == "date").Select(ct => ct.Key).ToList();

            // Introduction scene
            scenes.Add(new StoryScene
            {
                Type = "intro",
                Title = "Data Story",
                Content = $"Let's explore what we found for \"{question}\"",
                Visualization = "none"
            });

            // Distribution scene if we have numeric and category data
            if (numericColumns.Count > 0 && categoryColumns.Count > 0)
            {
                scenes.Add(new StoryScene
                {
                    Type = "distribution",
                    Title = "Distribution Overview",
                    Content = $"Here's how {numericColumns[0]} is distributed across different {categoryColumns[0]}",
                    Visualization = "bar",
                    Config = JsonSerializer.Serialize(new
                    {
                        xKey = categoryColumns[0],
                        yKey = numericColumns[0]
                    })
                });
            }

            // Trend scene if we have numeric and date data
            if (numericColumns.Count > 0 && dateColumns.Count > 0)
            {
                scenes.Add(new StoryScene
                {
                    Type = "trend",
                    Title = "Trend Analysis",
                    Content = $"Let's look at how {numericColumns[0]} has changed over time",
                    Visualization = "line",
                    Config = JsonSerializer.Serialize(new
                    {
                        xKey = dateColumns[0],
                        yKey = numericColumns[0]
                    })
                });
            }

            // Comparison scene if we have multiple numeric columns
            if (numericColumns.Count > 1)
            {
                scenes.Add(new StoryScene
                {
                    Type = "comparison",
                    Title = "Comparison",
                    Content = $"Let's compare {numericColumns[0]} with {numericColumns[1]}",
                    Visualization = "scatter",
                    Config = JsonSerializer.Serialize(new
                    {
                        xKey = numericColumns[0],
                        yKey = numericColumns[1]
                    })
                });
            }

            // Top performers scene
            if (numericColumns.Count > 0 && categoryColumns.Count > 0)
            {
                scenes.Add(new StoryScene
                {
                    Type = "topPerformers",
                    Title = "Top Performers",
                    Content = $"Here are the top performers based on {numericColumns[0]}",
                    Visualization = "horizontalBar",
                    Config = JsonSerializer.Serialize(new
                    {
                        xKey = numericColumns[0],
                        yKey = categoryColumns[0],
                        limit = 5,
                        sort = "desc"
                    })
                });
            }

            // Insights scene
            if (insights.Count > 0)
            {
                scenes.Add(new StoryScene
                {
                    Type = "insights",
                    Title = "Key Insights",
                    Content = "Here are some key insights from the data",
                    Visualization = "insights",
                    Config = JsonSerializer.Serialize(insights)
                });
            }

            // Pie chart scene if appropriate
            if (categoryColumns.Count > 0 && numericColumns.Count > 0)
            {
                var categoryGroups = data.GroupBy(row =>
                    row.ContainsKey(categoryColumns[0])
                        ? row[categoryColumns[0]]?.ToString()
                        : "Unknown");

                var pieData = categoryGroups.Select(group => new
                {
                    name = group.Key,
                    value = group.Sum(row =>
                    {
                        if (row.TryGetValue(numericColumns[0], out var val) && double.TryParse(val.ToString(), out var numVal))
                            return numVal;
                        return 0;
                    })
                }).ToList();

                if (pieData.Count >= 2 && pieData.Count <= 8)
                {
                    scenes.Add(new StoryScene
                    {
                        Type = "breakdown",
                        Title = "Breakdown",
                        Content = $"Let's see the breakdown by {categoryColumns[0]}",
                        Visualization = "pie",
                        Config = JsonSerializer.Serialize(new
                        {
                            dataKey = "value",
                            nameKey = "name",
                            data = pieData
                        })
                    });
                }
            }

            // Summary scene
            scenes.Add(new StoryScene
            {
                Type = "summary",
                Title = "Summary",
                Content = "That's the story of your data! Remember you can always explore more with new questions.",
                Visualization = "none"
            });

            return scenes;
        }
    }

    /// <summary>
    /// Data structure representing a complete data story
    /// </summary>
    public class DataStory
    {
        /// <summary>
        /// Flag indicating whether story generation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if story generation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The original question that generated the data
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// List of scenes making up the data story
        /// </summary>
        public List<StoryScene> Scenes { get; set; } = new List<StoryScene>();

        /// <summary>
        /// List of insights extracted from the data
        /// </summary>
        public List<DataInsight> Insights { get; set; } = new List<DataInsight>();
    }

    /// <summary>
    /// Represents a single scene in the data story
    /// </summary>
    public class StoryScene
    {
        /// <summary>
        /// Type of scene (intro, distribution, trend, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Title of the scene
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Descriptive content for the scene
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Type of visualization for the scene (bar, line, pie, etc.)
        /// </summary>
        public string Visualization { get; set; }

        /// <summary>
        /// Configuration for the visualization as JSON
        /// </summary>
        public string Config { get; set; }
    }

    /// <summary>
    /// Represents an insight extracted from the data
    /// </summary>
    public class DataInsight
    {
        /// <summary>
        /// Type of insight (max, min, average, trend, outlier, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the insight
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Additional data for the insight as JSON
        /// </summary>
        public string Data { get; set; }
    }
}