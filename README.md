# RaspberryLightsDeviceWebApi

## Description

RaspberryLightsDeviceWebApi is an ASP.NET 6 Web API designed to run on Raspberry Pi devices. It controls WS2812B LED strips directly and interfaces with the RaspberryLights web application. This API manages configuration and animation commands received from the RaspberryLights web interface, executing them in real-time on connected LED devices.It uses the [`rpi_ws281x-csharp`](https://github.com/rpi-ws281x/rpi-ws281x-csharp) library, a C# wapper a of C++ library to control the LED strips. It also features synchronization with automotive data via the OBD2 interface to animate ligths based on vehicle speed or engine RPM.

### Remote Connectivity with Ngrok

The API currently utilizes ngrok for remote connectivity, creating a secure tunnel to the Raspberry Pi. This is a temporary measure, using ngrok's free service tier for easy setup without a public IP on internet connection via GSM. Future plans involve transitioning to a more permanent solution such as Azure IoT Hub or a VPS.

## Features

- **Direct LED Control**: Directly manages WS2812B LED strips.
- **API Integration**: Interacts with the RaspberryLights web application to receive commands.
- **Automotive Data Synchronization**: Utilizes vehicle data to adapt lighting animations.

## Installation

### Prerequisites

- Raspberry Pi 4 with Raspberry Pi OS
- .NET 6 SDK
- Root access on Raspberry Pi
- WS2812B LED strips connected to Raspberry Pi

### Setup

1. Clone the repository:
    ```bash
    git clone https://github.com/your-username/RaspberryLightsDeviceWebApi.git
    ```
2. Navigate to the project directory:
    ```bash
    cd RaspberryLightsDeviceWebApi
    ```
3. Install dependencies:
    ```bash
    dotnet restore
    ```
4. Build the application:
    ```bash
    dotnet build
    ```

### Configuring as a Linux Service

To run RaspberryLightsDeviceWebApi as a service on Linux, create a service file named `RaspberryLightDeviceWebApi.service` and configure it as shown below:

```ini
[Unit]
Description=RaspberryLights ASP.NET Web API

[Service]
ExecStart=/home/pi/.dotnet/dotnet /home/pi/Desktop/RaspberryLightsDeviceWebApi/RaspberryLightsDeviceWebApi/bin/Debug/net6.0/RaspberryLightsDeviceWebApi.dll
WorkingDirectory=/home/pi/Desktop/RaspberryLightsDeviceWebApi/RaspberryLightsDeviceWebApi/bin/Debug/net6.0
Restart=always
RestartSec=10  # Restart service after 10 seconds if dotnet service crashes
SyslogIdentifier=dotnet-raspberry-lights
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=https://localhost:5252
Environment=NgrokAuthToken=<Your Ngrok Auth Token>

[Install]
WantedBy=multi-user.target
```
Move this file to /etc/systemd/system/.
Enable the service to start at boot:

```bash
sudo systemctl enable RaspberryLightDeviceWebApi.service
```

Start the service:
```bash
sudo systemctl start RaspberryLightDeviceWebApi.service
```

### Usage

Once the service is running, the API will automatically start with the system and run in the background, listening for commands from the RaspberryLights web application and controlling the LED strips accordingly.

### License

This project is made available for personal, non-commercial use only. Redistribution or commercial use is not permitted. All rights reserved.
