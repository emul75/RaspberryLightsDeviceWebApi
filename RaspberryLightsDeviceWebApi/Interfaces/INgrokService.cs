namespace RaspberryLightsDeviceWebApi.Interfaces;

public interface INgrokService
{
    Task StartupSetup();
    Task StartNgrokTunnel();
    Task UpdatePublicUrl();
}