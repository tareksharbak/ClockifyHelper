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
        private Win32LastInputInfo lastInputBuffer = new Win32LastInputInfo();

        private string hiddenCharacter = " *";

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

        private const double timerInterval = 10 * 1000; //60 seconds
        private uint? baseLine;
        private uint timeSinceBaseLine;

        private bool isWorkStarted;

        private TimeSpan idleTimeThreshold = TimeSpan.FromMinutes(30);

        private Timer timer;

        public ViewModel()
        {
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
                        timer = new Timer(timerInterval);
                        timer.Elapsed += Timer_Elapsed;
                        timer.AutoReset = true;
                        timer.Start();
                        await StartWorkAsync();
                    }
                    else
                    {
                        timer.Dispose();
                        timer = null;
                        await StopWorkAsync();
                    }
                    IsStarted = !IsStarted;
                },
                canExecute: (x) =>
                {
                    return IsApiKeySaved && SelectedProject != null;
                });

            lastInputBuffer.cbSize = (uint)Win32LastInputInfo.SizeOf;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Win32.GetLastInputInfo(out lastInputBuffer);

            if (lastInputBuffer.dwTime != baseLine)
            {
                baseLine = lastInputBuffer.dwTime;
                timeSinceBaseLine = baseLine.Value;
            }
            else
            {
                timeSinceBaseLine += (uint)timerInterval;
            }

            var idleTime = timeSinceBaseLine - baseLine.Value;
            var idleTimeSpan = TimeSpan.FromMilliseconds(idleTime);

            if (idleTimeSpan >= idleTimeThreshold)
            {
                if (IsWorkStarted)
                {
                    await StopWorkAsync();
                }
            }
            else
            {
                if (!IsWorkStarted)
                {
                    await StartWorkAsync();
                }
            }

            Debug.WriteLine($"User been idle for {Math.Round(idleTimeSpan.TotalSeconds, 2)} seconds");
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
                return IsStarted ? "Stop" : "Start";
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
