using ClockifyHelper.ClockifyModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
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
        private const string HiddenCharacter = " *";
        private const int BufferEndTimeMinutes = 2;

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

        private Timer trackingUpdateTimer;


        public ViewModel(ApplicationSettings applicationSettings)
        {
            idleTimeService = new IdleTimeService(TimeSpan.FromMinutes(applicationSettings.IdleThresholdMinutes));
            idleTimeService.UserIdled += IdleTimeService_UserIdled;
            idleTimeService.UserReactivated += IdleTimeService_UserReactivated;

            trackingUpdateTimer = new Timer(TimeSpan.FromMinutes(applicationSettings.IdleThresholdMinutes).TotalMilliseconds);
            trackingUpdateTimer.Elapsed += TrackingUpdateTimer_Elapsed;
            trackingUpdateTimer.AutoReset = true;

            ApiKeyTextBox = applicationSettings.ApiKey;

            ConfigureCommands(applicationSettings);

            if (!string.IsNullOrWhiteSpace(apiKeyTextBox))
            {
                SaveCommand.Execute(true);
            }
        }

        private void ConfigureCommands(ApplicationSettings applicationSettings)
        {
            SaveCommand = new ObservableCommand<ViewModel>(
                this,
                execute: async (x) =>
                {
                    var isInInitializationPhase = (x as bool?) ?? false;
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

                        SelectedProject = Projects.SingleOrDefault(a => a.Name == applicationSettings.DefaultProjectName);

                        SavedApiKey = apiKeyTextBox;
                        IsApiKeySaved = true;
                        ApiKeyTextBox = HiddenCharacter + string.Join("", Enumerable.Range(0, apiKeyTextBox.Length).Select(a => "*"));
                    }
                    catch (Exception)
                    {
                        if (!isInInitializationPhase)
                        {
                            MessageBox.Show("Invalid Key", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        IsApiKeySaved = false;
                    }
                },
                canExecute: (x) =>
                {
                    var canExecute = ApiKeyTextBox != SavedApiKey && !ApiKeyTextBox.StartsWith(HiddenCharacter);
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
                    ApiKeyTextBox = HiddenCharacter + string.Join("", Enumerable.Range(0, apiKeyTextBox.Length).Select(a => "*"));
                }
            });

            StartCommand = new ObservableCommand<ViewModel>(this,
                execute: async (x) =>
                {
                    if (!isStarted)
                    {
                        await StartOrUpdateActiveTimeTrackingAsync();
                        idleTimeService.Start();
                    }
                    else
                    {
                        await StopActiveTimeTrackingAsync();
                        idleTimeService.Stop();
                    }
                    IsStarted = !IsStarted;
                },
                canExecute: (x) =>
                {
                    return IsApiKeySaved && SelectedProject != null;
                });
        }

        private async Task StartOrUpdateActiveTimeTrackingAsync()
        {
            var activeTimeTracking = await clockifyService.GetCurrentlyActiveTime(userId, workspaceId);

            if (activeTimeTracking == null)
            {
                Debug.WriteLine("Starting active time tracking");
                await clockifyService.CreateTimeAsync(workspaceId, DateTime.UtcNow, GetPotentialEndTime(), selectedProject.Id);
            }
            else
            {
                Debug.WriteLine("Updating active time tracking");
                await clockifyService.UpdateEndTimeAsync(workspaceId, activeTimeTracking, GetPotentialEndTime());
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsWorkStarted = true;
            });
            trackingUpdateTimer.Start();
        }

        private async Task StopActiveTimeTrackingAsync()
        {
            Debug.WriteLine("Stopping active time tracking");
            var activeTimeTracking = await clockifyService.GetCurrentlyActiveTime(userId, workspaceId);
            if (activeTimeTracking != null)
            {
                await clockifyService.UpdateEndTimeAsync(workspaceId, activeTimeTracking, DateTime.UtcNow);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsWorkStarted = false;
            });
            trackingUpdateTimer.Stop();
        }

        private DateTime GetPotentialEndTime()
        {
            return DateTime.UtcNow.AddMinutes(IdleThresholdMinutes + BufferEndTimeMinutes);
        }

        private async void TrackingUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await StartOrUpdateActiveTimeTrackingAsync();
        }

        private async void IdleTimeService_UserReactivated(object sender, EventArgs e)
        {
            //SystemSounds.Beep.Play();
            await StartOrUpdateActiveTimeTrackingAsync();
        }

        private async void IdleTimeService_UserIdled(object sender, EventArgs e)
        {
            //SystemSounds.Exclamation.Play();
            await StopActiveTimeTrackingAsync();
        }

        public int IdleThresholdMinutes
        {
            get => idleThresholdMinutes;
            set
            {
                SetProperty(ref idleThresholdMinutes, value);
                idleTimeService.ChangeIdleTimeThreshold(TimeSpan.FromMinutes(value));
                trackingUpdateTimer.Interval = TimeSpan.FromMinutes(value).TotalMilliseconds;
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
