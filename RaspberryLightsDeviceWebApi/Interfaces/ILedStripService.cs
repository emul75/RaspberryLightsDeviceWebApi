using RaspberryLightsDeviceWebApi.Models;

namespace RaspberryLightsDeviceWebApi.Interfaces;

public interface ILedStripService
{
    Task StartAnimation(AnimationParameters parameters, CancellationToken cancellationToken);
    void ClearLeds();
    AnimationParameters GetCurrentAnimationParameters();
}