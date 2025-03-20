using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace DynamicDasboardWebAPI.Services.LLM
{
    /// <summary>
    /// Factory for creating LLM service instances based on configuration
    /// </summary>
    public class LLMServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public LLMServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration
            )
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Creates an LLM service based on the configuration
        /// </summary>
        /// <returns>An implementation of ILlmService</returns>
        public ILLMService CreateLlmService()
        {
            try
            {
                var providerName = _configuration["LlmService:Provider"]?.ToLowerInvariant() ?? "claude";
                return providerName switch
                {
                    "claude" => CreateClaudeService(),
                    "deepseek" => CreateDeepSeekService(),
                    "sqlcoder" => CreateSQLCoderService(),
                    "databricks" => CreateDatabricksService(), // Add this case for Databricks
                    _ => throw new NotSupportedException($"LLM provider '{providerName}' is not supported")
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ILLMService CreateClaudeService()
        {
            try
            {
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                return new ClaudeLLMService(httpClient, _configuration);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ILLMService CreateDeepSeekService()
        {
            try
            {
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                return new DeepSeekLLMService(httpClient, _configuration);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ILLMService CreateSQLCoderService()
        {
            try
            {
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                return new SQLCoderLLMService(httpClient, _configuration);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private ILLMService CreateDatabricksService()
        {
            try
            {
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
                return new DatabricksLLMService(httpClient, _configuration);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}