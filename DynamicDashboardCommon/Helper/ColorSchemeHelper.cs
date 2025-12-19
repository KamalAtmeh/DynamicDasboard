using System;
using System.Collections.Generic;
using DynamicDashboardCommon.Models;

namespace DynamicDashboardCommon.Helper
{
    /// <summary>
    /// Helper class for color scheme operations.
    /// </summary>
    public static class ColorSchemeHelper
    {
        /// <summary>
        /// Gets all available color schemes.
        /// </summary>
        /// <returns>List of all color schemes.</returns>
        public static List<ColorScheme> GetAllSchemes()
        {
            return new List<ColorScheme>
            {
                new ColorScheme
                {
                    Key = "default",
                    Name = "Default",
                    Colors = new List<string> { "#667eea", "#764ba2", "#f093fb", "#f5576c", "#4facfe", "#00f2fe" }
                },
                new ColorScheme
                {
                    Key = "ocean",
                    Name = "Ocean",
                    Colors = new List<string> { "#4facfe", "#00f2fe", "#43e97b", "#38f9d7", "#667eea", "#764ba2" }
                },
                new ColorScheme
                {
                    Key = "sunset",
                    Name = "Sunset",
                    Colors = new List<string> { "#fa709a", "#fee140", "#f7971e", "#ffd200", "#ff6b6b", "#feca57" }
                },
                new ColorScheme
                {
                    Key = "forest",
                    Name = "Forest",
                    Colors = new List<string> { "#11998e", "#38ef7d", "#56ab2f", "#a8e063", "#134e5e", "#71b280" }
                },
                new ColorScheme
                {
                    Key = "berry",
                    Name = "Berry",
                    Colors = new List<string> { "#8e2de2", "#4a00e0", "#bc4e9c", "#f80759", "#ee0979", "#ff6a00" }
                },
                new ColorScheme
                {
                    Key = "monochrome",
                    Name = "Monochrome",
                    Colors = new List<string> { "#2d3748", "#4a5568", "#718096", "#a0aec0", "#cbd5e0", "#e2e8f0" }
                },
                new ColorScheme
                {
                    Key = "pastel",
                    Name = "Pastel",
                    Colors = new List<string> { "#a8d8ea", "#aa96da", "#fcbad3", "#ffffd2", "#b5eaea", "#f3d9dc" }
                }
            };
        }

        /// <summary>
        /// Gets a color scheme by its key.
        /// </summary>
        /// <param name="key">The scheme key.</param>
        /// <returns>The color scheme or default if not found.</returns>
        public static ColorScheme GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return GetDefaultScheme();
            }

            var schemes = GetAllSchemes();
            return schemes.Find(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? GetDefaultScheme();
        }

        /// <summary>
        /// Gets the default color scheme.
        /// </summary>
        /// <returns>The default color scheme.</returns>
        public static ColorScheme GetDefaultScheme()
        {
            return new ColorScheme
            {
                Key = "default",
                Name = "Default",
                Colors = new List<string> { "#667eea", "#764ba2", "#f093fb", "#f5576c", "#4facfe", "#00f2fe" }
            };
        }

        /// <summary>
        /// Gets a color from a scheme by index, cycling through colors if index exceeds count.
        /// </summary>
        /// <param name="schemeKey">The scheme key.</param>
        /// <param name="index">The color index.</param>
        /// <returns>The color hex value.</returns>
        public static string GetColorByIndex(string schemeKey, int index)
        {
            var scheme = GetByKey(schemeKey);
            if (scheme.Colors == null || scheme.Colors.Count == 0)
            {
                return "#667eea"; // Fallback color
            }

            return scheme.Colors[index % scheme.Colors.Count];
        }

        /// <summary>
        /// Generates a random color from a scheme.
        /// </summary>
        /// <param name="schemeKey">The scheme key.</param>
        /// <returns>A random color from the scheme.</returns>
        public static string GetRandomColor(string schemeKey)
        {
            var scheme = GetByKey(schemeKey);
            if (scheme.Colors == null || scheme.Colors.Count == 0)
            {
                return "#667eea";
            }

            var random = new Random();
            return scheme.Colors[random.Next(scheme.Colors.Count)];
        }

        /// <summary>
        /// Gets avatar color based on a string (for consistent user avatars).
        /// </summary>
        /// <param name="input">The input string (e.g., email or name).</param>
        /// <returns>A consistent color for the input.</returns>
        public static string GetAvatarColor(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "#667eea";
            }

            var colors = new[]
            {
                "#667eea", "#f093fb", "#4facfe", "#43e97b", "#fa709a",
                "#fee140", "#11998e", "#8e2de2", "#f5576c", "#38ef7d"
            };

            var hash = Math.Abs(input.GetHashCode());
            return colors[hash % colors.Length];
        }
    }
}