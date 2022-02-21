# Introduction
A windows application to automatically start/stop time tracking on [Clockify](https://clockify.me/) based on user activity.

# Getting started
This application is built using WPF and dotnet 6.0. To build, you can use either Visual Studio 2022 or the Dotnet CLI.

# Configuration
The appsettings.json file holds the configurable options of the application. 
> Note: If left empty or to their default value, you will have to enter the values directly in the app at every restart.
* ApiKey: Your API Key to authenticate with the Clockify API. You can retrieve this from your account settings.
* DefaultProjectName: The default project name to use when creating a Clockify time tracking
* IdleThresholdMinutes: The time it takes for the application to deem the user inactive if no mouse or keyboard activity has been detected.
* MinimizeOnClose: Choose whether to minimize the application instead of closing it when the "x" button is pressed
* ShowInSystemTray: Whether to show the application in the system tray or not
* EnableNotifications: Enable notifications for start/stop activities (requires the "ShowInSystemTray" to be active)