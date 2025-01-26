using System.Threading.Tasks;
using DynamicDasboardWebAPI.Repositories;


namespace DynamicDasboardWebAPI.Services
{
    public interface ILogsService
    {
        Task LogExceptionAsync(int? userId, string eventType, string eventDescription);
    }
}
