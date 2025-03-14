using Blazored.Toast.Services;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DynamicDashboardFE.Utilities
{
    public class Notifications
    {
        private readonly IToastService _toastService;
        private readonly Queue<string> _activeNotifications = new Queue<string>();
        private readonly SemaphoreSlim _notificationLock = new SemaphoreSlim(1, 1);
        private const int MAX_CONCURRENT_NOTIFICATIONS = 2;

        public Notifications(IToastService toastService)
        {
            _toastService = toastService;
        }

        public async void ShowSuccess(string message, int timeout = 3000)
        {
            await _notificationLock.WaitAsync();
            try
            {
                // Manage notification queue
                ManageNotificationQueue(message);

                _toastService.ShowSuccess(message, options =>
                {
                    options.Timeout = timeout;
                    options.ShowCloseButton = true;

                });
            }
            finally
            {
                _notificationLock.Release();
            }
        }

        public async void ShowError(string message, int timeout = 4000)
        {
            await _notificationLock.WaitAsync();
            try
            {
                // Manage notification queue
                //ManageNotificationQueue(message);

                _toastService.ShowError(message, options =>
                {
                    options.Timeout = timeout;
                    options.ShowCloseButton = true;

                });
            }
            finally
            {
                _notificationLock.Release();
            }
        }

        public async void ShowInfo(string message, int timeout = 3000)
        {
            await _notificationLock.WaitAsync();
            try
            {
                // Manage notification queue
                //ManageNotificationQueue(message);

                _toastService.ShowInfo(message, options =>
                {
                    options.Timeout = timeout;
                    options.ShowCloseButton = true;

                });
            }
            finally
            {
                _notificationLock.Release();
            }
        }

        public async void ShowWarning(string message, int timeout = 4000)
        {
            await _notificationLock.WaitAsync();
            try
            {
                // Manage notification queue
                //ManageNotificationQueue(message);

                _toastService.ShowWarning(message, options =>
                {
                    options.Timeout = timeout;
                    options.ShowCloseButton = true;
                });
            }
            finally
            {
                _notificationLock.Release();
            }
        }

        private void ManageNotificationQueue(string message)
        {
            // If we already have max notifications showing, remove the oldest one
            if (_activeNotifications.Count >= MAX_CONCURRENT_NOTIFICATIONS)
            {
                string oldestMessage = _activeNotifications.Dequeue();
                // We don't need to explicitly close the toast as it will auto-hide
            }

            // Add new notification to the queue
            _activeNotifications.Enqueue(message);
        }

        private void RemoveNotification(string message)
        {
            _notificationLock.Wait();
            try
            {
                // Remove the message from active notifications if it exists
                if (_activeNotifications.Contains(message))
                {
                    var tempList = new List<string>(_activeNotifications);
                    tempList.Remove(message);
                    _activeNotifications.Clear();
                    foreach (var item in tempList)
                    {
                        _activeNotifications.Enqueue(item);
                    }
                }
            }
            finally
            {
                _notificationLock.Release();
            }
        }
    }
}