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

1. **Install and Configure on you esp32/esp8266 module**
   
https://github.com/user-attachments/assets/0967acd1-1a92-423c-a835-a51deebda3a0

3. **Download the zip file with the application files:** [OnAir.zip](https://github.com/t0mer/OnAir/raw/refs/heads/main/OnAir.zip)

4. **Exctract the files from the downloaded zip** and edit OnAir.exe.config file**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
    </startup>
	<appSettings>
		<!-- Camera and URLs -->
		<add key="CameraHardwareId" value="" />
		<add key="PostUrl" value="http://[]/json" />
		<add key="GetUrl" value="http://[]/win" />
		<!-- Preset values -->
		<add key="ActiveMeetingWithCameraPreset" value="" />
		<add key="ActiveMeetingWithoutCameraPreset" value="" />
		<add key="NoMeetingWithCameraPreset" value="" />
		<add key="NoMeetingWithoutCameraPreset" value="" />
	</appSettings>
</configuration>

```

* Under Post and Get URLs, insert the wled device ip.
* Update the values of the presets from the WLED configuration:
  * ActiveMeetingWithCameraPreset
  * ActiveMeetingWithoutCameraPreset
  * NoMeetingWithCameraPreset
  * NoMeetingWithoutCameraPreset
* Set the camera hardware id. you can get the value from the device manager
  
![image](https://github.com/user-attachments/assets/adbb19dc-c162-4794-ae9a-1bd8e402a605)

The Hardware id is the part that starts with "VID" and ends with the "PID" value.
In the example, the hadware id will be: "VID_5986&amp;PID_2113".
Remember to replace the **&** with **&amp;** due to the xml config file limitations. 


