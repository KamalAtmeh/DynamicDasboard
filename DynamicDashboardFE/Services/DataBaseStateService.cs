using DynamicDashboardCommon.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DynamicDashboardFE.Services
{
    public class DatabaseStateService
    {
        private List<Database> _databases;
        private readonly HttpClient _httpClient;

        public DatabaseStateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Database>> GetDatabasesAsync()
        {
            if (_databases == null)
            {
                _databases = await _httpClient.GetFromJsonAsync<List<Database>>("api/databases");
            }
            return _databases;
        }

        public async Task RefreshDatabasesAsync()
        {
            _databases = await _httpClient.GetFromJsonAsync<List<Database>>("api/databases");
        }
    }
}