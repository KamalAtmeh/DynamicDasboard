using Blazored.Toast.Services;

namespace DynamicDashboardFE.Utilities
{
    public class Notifications
    {
        private readonly IToastService _toastService;

        public Notifications(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void ShowSuccess(string message, int timeout = 3000)
        {
            _toastService.ShowSuccess(message, options =>
            {
                options.Timeout = timeout;
                options.ShowCloseButton = true;
            });
        }

        public void ShowError(string message, int timeout = 4000)
        {
            _toastService.ShowError(message, options =>
            {
                options.Timeout = timeout;
                options.ShowCloseButton = true;
            });
        }

        public void ShowInfo(string message, int timeout = 3000)
        {
            _toastService.ShowInfo(message, options =>
            {
                options.Timeout = timeout;
                options.ShowCloseButton = true;
            });
        }

        public void ShowWarning(string message, int timeout = 4000)
        {
            _toastService.ShowWarning(message, options =>
            {
                options.Timeout = timeout;
                options.ShowCloseButton = true;
            });
        }
    }
}
