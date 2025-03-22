using DynamicDashboardCommon.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDashboardFE.Pages.Admin
{
    public partial class ComparisonAutomation
    {
        private List<QueryComparison> comparisons;
        private IBrowserFile file;
        private bool isLoading = false;
        private string errorMessage;

        // Event handler for file input change
        private void OnInputFileChange(InputFileChangeEventArgs e)
        {
            if (e.File.Size > 10 * 1024 * 1024) // 10 MB limit
            {
                errorMessage = "File size exceeds the limit of 10 MB.";
                file = null;
            }
            else
            {
                errorMessage = null;
                file = e.File;
            }
        }

        // Method to upload the file and process the comparison
        private async Task UploadFile()
        {
            if (file == null)
            {
                errorMessage = "Please select a file to upload.";
                return;
            }

            isLoading = true;
            errorMessage = null;

            try
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(file.OpenReadStream()), "file", file.Name);

                var response = await Http.PostAsync("api/comparison/compare", content);

                if (response.IsSuccessStatusCode)
                {
                    comparisons = await response.Content.ReadFromJsonAsync<List<QueryComparison>>();
                }
                else
                {
                    errorMessage = "An error occurred while processing the file. Please try again.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
