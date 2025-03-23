# OnAir

**OnAir** is a lightweight Windows desktop application that monitors your active meetings (e.g., Zoom, Microsoft Teams) and camera  activity. Based on your current status, it sends HTTP notifications to a WLED device—triggering visual presets that you can configure for different scenarios.

## Features

- **Active Meeting Detection:**  
  Automatically detects when you are in an active meeting by scanning for running processes (Zoom/Teams) and inspecting window titles for keywords like "Meeting" or "Call."

- **Device Monitoring:**  
  Checks you are currently in a meeting (Teams and Zoom are the only supported applications at the moment). It also checks if the Camera is in use.

- **HTTP Notifications:**  
  Sends HTTP GET requests on startup and shutdown, and sends HTTP POST requests with a JSON payload when the monitored status changes and changes
  the color of the "On Air" sgin.

- **Configurable Presets:**  
  Change the visual feedback on your WLED device based on your status using four preset values:
  - **Preset 1:** Active meeting with camera in use.
  - **Preset 2:** Active meeting without camera in use.
  - **Preset 5:** No active meeting, but camera in use.
  - **Preset 3:** No meeting and no camera usage.

- **System Tray Integration:**  
  Runs in the background with a tray icon and context menu to start/stop monitoring or exit the application.

## Requirements

- **Windows OS**
- **.NET Framework / .NET (version used to build the application)**
- **WLED Device** (an ESP8266/ESP32 flashed with the [WLED firmware](https://github.com/Aircoookie/WLED))
- NuGet packages:
  - AForge.Video
  - AForge.Video.DirectShow
  - NAudio
  - System.Configuration (if needed)

## Installation

1. **Download the zip file with the application files:** [OnAir.zip](https://github.com/t0mer/OnAir/raw/refs/heads/main/OnAir.zip)


