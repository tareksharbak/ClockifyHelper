using ClockifyHelper.ClockifyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClockifyHelper
{
    public class ClockifyService : IDisposable
    {
        private HttpClient httpClient = new HttpClient();

        public ClockifyService(string apiKey)
        {
            httpClient.BaseAddress = new Uri("https://api.clockify.me");
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public async Task<Workspace> GetWorkspaceAsync()
        {
            var response = await httpClient.GetAsync("api/v1/workspaces");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var workspace = JsonSerializer.Deserialize<Workspace[]>(content);

            if (workspace.Length > 0)
            {
                return workspace[0];
            }
            return null;
        }

        public async Task<User> GetUserAsync()
        {
            var response = await httpClient.GetAsync("api/v1/user");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var user = JsonSerializer.Deserialize<User>(content);

            return user;
        }

        public async Task<Project[]> GetProjectsAsync(string workspaceId)
        {
            var response = await httpClient.GetAsync($"api/v1/workspaces/{workspaceId}/projects");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var projects = JsonSerializer.Deserialize<Project[]>(content);

            return projects;
        }

        public async Task StartTimerAsync(string workspaceId, string projectId = null)
        {
            var time = new Time()
            {
                Start = DateTime.UtcNow,
                ProjectId = projectId
            };

            var stringContent = new StringContent(JsonSerializer.Serialize(time), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"api/v1/workspaces/{workspaceId}/time-entries", stringContent);
            response.EnsureSuccessStatusCode();
        }

        public async Task StopTimerAsync(string userId, string workspaceId)
        {
            var stop = new
            {
                end = DateTime.UtcNow
            };

            var stringContent = new StringContent(JsonSerializer.Serialize(stop), Encoding.UTF8, "application/json");

            var response = await httpClient.PatchAsync($"api/v1/workspaces/{workspaceId}/user/{userId}/time-entries", stringContent);
            response.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    httpClient?.Dispose();
                }
                catch { }
            }
        }
    }
}
