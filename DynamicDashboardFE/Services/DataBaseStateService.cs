using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DynamicDashboardFE.Services
{
    /// <summary>
    /// Service to manage the state of databases in the application.
    /// Provides methods to retrieve and refresh the list of databases from the server.
    /// </summary>
    public class DatabaseStateService
    {
        private List<Database> _databases;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseStateService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to make requests to the server.</param>
        public DatabaseStateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the list of databases asynchronously. If the list is not already loaded, it fetches it from the server.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of databases.</returns>
        public async Task<List<Database>> GetDatabasesAsync()
        {
            if (_databases == null)
            {
                _databases = await _httpClient.GetFromJsonAsync<List<Database>>("api/databases");
            }
            return _databases;
        }

        /// <summary>
        /// Refreshes the list of databases by fetching the latest data from the server asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RefreshDatabasesAsync()
        {
            _databases = await _httpClient.GetFromJsonAsync<List<Database>>("api/databases");
        }
    }
}