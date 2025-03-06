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
        private readonly ILogger<LLMServiceFactory> _logger;

        public LLMServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<LLMServiceFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates an LLM service based on the configuration
        /// </summary>
        /// <returns>An implementation of ILlmService</returns>
        public ILLMService CreateLlmService()
        {
            var providerName = _configuration["LlmService:Provider"]?.ToLowerInvariant() ?? "claude";

            _logger.LogInformation("Creating LLM service with provider: {Provider}", providerName);

            return providerName switch
            {
                "claude" => CreateClaudeService(),
                "deepseek" => CreateDeepSeekService(),
                _ => throw new NotSupportedException($"LLM provider '{providerName}' is not supported")
            };
        }

        private ILLMService CreateClaudeService()
        {
            var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
            return new ClaudeLLMService(httpClient, _configuration,
                _serviceProvider.GetRequiredService<ILogger<ClaudeLLMService>>());
        }

        private ILLMService CreateDeepSeekService()
        {
            var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
            return new DeepSeekLLMService(httpClient, _configuration,
                _serviceProvider.GetRequiredService<ILogger<DeepSeekLLMService>>());
        }
    }
}