using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DynamicDashboardCommon.Models;

namespace DynamicDashboardCommon.Helpers
{
    /// <summary>
    /// Helper class for loading and working with dashboard templates
    /// </summary>
    public static class DashboardTemplateHelper
    {
        private static DashboardTemplateCollection _templates;
        private static readonly object _lock = new object();

        /// <summary>
        /// Loads templates from the JSON file
        /// </summary>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>Collection of dashboard templates</returns>
        public static DashboardTemplateCollection LoadTemplates(string templatesFilePath)
        {
            if (_templates != null)
            {
                return _templates;
            }

            lock (_lock)
            {
                if (_templates != null)
                {
                    return _templates;
                }

                try
                {
                    if (!File.Exists(templatesFilePath))
                    {
                        throw new FileNotFoundException($"Template file not found: {templatesFilePath}");
                    }

                    var jsonContent = File.ReadAllText(templatesFilePath);
                    _templates = JsonSerializer.Deserialize<DashboardTemplateCollection>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return _templates ?? new DashboardTemplateCollection();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error loading dashboard templates: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Gets all available templates
        /// </summary>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of templates</returns>
        public static List<DashboardTemplate> GetAllTemplates(string templatesFilePath)
        {
            var collection = LoadTemplates(templatesFilePath);
            return collection.Templates ?? new List<DashboardTemplate>();
        }

        /// <summary>
        /// Gets a template by ID
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>The template if found, null otherwise</returns>
        public static DashboardTemplate GetTemplateById(string templateId, string templatesFilePath)
        {
            var templates = GetAllTemplates(templatesFilePath);
            return templates.FirstOrDefault(t => t.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets templates by category
        /// </summary>
        /// <param name="category">Template category</param>
        /// <param name="templatesFilePath">Path to the templates JSON file</param>
        /// <returns>List of templates in the category</returns>
        public static List<DashboardTemplate> GetTemplatesByCategory(string category, string templatesFilePath)
        {
            var templates = GetAllTemplates(templatesFilePath);
            return templates.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Converts a template component slot to a data viewing type ID
        /// </summary>
        /// <param name="type">Component type string</param>
        /// <returns>DataViewingTypeEnum value</returns>
        public static int GetDataViewingTypeFromSlotType(string type)
        {
            return type?.ToLower() switch
            {
                "kpi" => (int)DataViewingTypeEnum.Number,
                "chart" => (int)DataViewingTypeEnum.Chart,
                "table" => (int)DataViewingTypeEnum.Table,
                "card" => (int)DataViewingTypeEnum.Card,
                "label" => (int)DataViewingTypeEnum.Label,
                _ => (int)DataViewingTypeEnum.Chart // Default to chart
            };
        }

        /// <summary>
        /// Resets the cached templates (useful for testing or reloading)
        /// </summary>
        public static void ResetCache()
        {
            lock (_lock)
            {
                _templates = null;
            }
        }
    }
}
