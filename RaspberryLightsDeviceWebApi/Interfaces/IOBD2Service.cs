namespace RaspberryLightsDeviceWebApi.Interfaces;

public interface IOBD2Service
{
    Task<int> GetCurrentSpeed();
    Task<int> GetCurrentRpm();
    void StartReading();
    Task StopReading();
}