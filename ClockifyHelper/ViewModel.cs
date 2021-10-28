using ClockifyHelper.ClockifyModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClockifyHelper
{
    public class ViewModel : ViewModelBase
    {
        private string hiddenCharacter = " *";

        private IdleTimeService idleTimeService;

        private ClockifyService clockifyService;
        private string apiKeyTextBox;
        private string savedApiKey;
        private string username;
        private string workspace;

        private string workspaceId;
        private string userId;

        private Project[] projects;
        private Project selectedProject;

        private bool isApiKeySaved;

        private bool isStarted;

        private bool isWorkStarted;
        private int idleThresholdMinutes = 15;

        public ViewModel()
        {
            idleTimeService = new IdleTimeService(TimeSpan.FromMinutes(idleThresholdMinutes));
            idleTimeService.UserIdled += IdleTimeService_UserIdled;
            idleTimeService.UserReactivated += IdleTimeService_UserReactivated;

            SaveCommand = new ObservableCommand<ViewModel>(
                this,
                execute: async (x) =>
                {
                    try
                    {
                        clockifyService?.Dispose();
                        clockifyService = new ClockifyService(apiKeyTextBox);

                        var user = await clockifyService.GetUserAsync();
                        Username = user.Name;
                        userId = user.Id;

                        var workspace = await clockifyService.GetWorkspaceAsync();
                        Workspace = workspace.Name;
                        workspaceId = workspace.Id;

                        Projects = await clockifyService.GetProjectsAsync(workspaceId);

                        SavedApiKey = apiKeyTextBox;
                        IsApiKeySaved = true;
                        ApiKeyTextBox = hiddenCharacter + string.Join("", Enumerable.Range(0, apiKeyTextBox.Length).Select(a => "*"));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Invalid Key", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                        IsApiKeySaved = false;
                    }
                },
                canExecute: (x) =>
                {
                    var canExecute = ApiKeyTextBox != SavedApiKey && !ApiKeyTextBox.StartsWith(hiddenCharacter);
                    return canExecute;
                });

            RestoreApiTextCommand = new BasicCommand((x) =>
            {
                if (IsApiKeySaved)
                {
                    ApiKeyTextBox = SavedApiKey;
                }
            });

            HideApiTextCommand = new BasicCommand((x) =>
            {
                if (IsApiKeySaved)
                {
                    ApiKeyTextBox = hiddenCharacter + string.Join("", Enumerable.Range(0, apiKeyTextBox.Length).Select(a => "*"));
                }
            });

            StartCommand = new ObservableCommand<ViewModel>(this,
                execute: async (x) =>
                {
                    if (!isStarted)
                    {
                        idleTimeService.Start();
                        await StartWorkAsync();
                    }
                    else
                    {
                        idleTimeService.Stop();
                        await StopWorkAsync();
                    }
                    IsStarted = !IsStarted;
                },
                canExecute: (x) =>
                {
                    return IsApiKeySaved && SelectedProject != null;
                });
        }

        private async void IdleTimeService_UserReactivated(object sender, EventArgs e)
        {
            await StartWorkAsync();
        }

        private async void IdleTimeService_UserIdled(object sender, EventArgs e)
        {
            await StopWorkAsync();
        }

        private async Task StartWorkAsync()
        {
            await clockifyService.StartTimerAsync(workspaceId, selectedProject.Id).ConfigureAwait(true);
            Application.Current.Dispatcher.Invoke(() => IsWorkStarted = true);
        }

        private async Task StopWorkAsync()
        {
            await clockifyService.StopTimerAsync(userId, workspaceId).ConfigureAwait(true);
            Application.Current.Dispatcher.Invoke(() => IsWorkStarted = false);
        }

        public int IdleThresholdMinutes
        {
            get => idleThresholdMinutes;
            set
            {
                SetProperty(ref idleThresholdMinutes, value);
                idleTimeService.ChangeIdleTimeThreshold(TimeSpan.FromMinutes(value));
            }
        }

        public string ApiKeyTextBox
        {
            get => apiKeyTextBox;
            set
            {
                SetProperty(ref apiKeyTextBox, value);
            }
        }

        private string SavedApiKey
        {
            get => savedApiKey;
            set
            {
                SetProperty(ref savedApiKey, value);
            }
        }

        public string SaveButtonName
        {
            get
            {
                return IsApiKeySaved ? "Saved" : "Save";
            }
        }

        public string StartButtonName
        {
            get
            {
                return IsStarted ? "Stop" : "Activate";
            }
        }

        public bool IsApiKeySaved
        {
            get => isApiKeySaved;
            set
            {
                SetProperty(ref isApiKeySaved, value);
                NotifyPropertyChanged(nameof(SaveButtonName));
                NotifyPropertyChanged(nameof(ProjectsEnabled));
            }
        }

        public bool IsStarted
        {
            get => isStarted;
            set
            {
                SetProperty(ref isStarted, value);
                NotifyPropertyChanged(nameof(ProjectsEnabled));
                NotifyPropertyChanged(nameof(StartButtonName));
                NotifyPropertyChanged(nameof(ApiKeyTextBoxEnabled));
            }
        }

        public bool IsWorkStarted
        {
            get => isWorkStarted;
            set
            {
                SetProperty(ref isWorkStarted, value);
            }
        }

        public string Username
        {
            get => username;
            set
            {
                SetProperty(ref username, value);
            }
        }

        public string Workspace
        {
            get => workspace;
            set
            {
                SetProperty(ref workspace, value);
            }
        }

        public Project[] Projects
        {
            get => projects;
            set
            {
                SetProperty(ref projects, value);
            }
        }

        public Project SelectedProject
        {
            get => selectedProject;
            set
            {
                SetProperty(ref selectedProject, value);
            }
        }

        public bool ProjectsEnabled
        {
            get
            {
                return isApiKeySaved && !IsStarted;
            }
        }

        public bool ApiKeyTextBoxEnabled
        {
            get
            {
                return !IsStarted;
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand RestoreApiTextCommand { get; private set; }
        public ICommand HideApiTextCommand { get; private set; }
        public ICommand StartCommand { get; private set; }
    }
}
