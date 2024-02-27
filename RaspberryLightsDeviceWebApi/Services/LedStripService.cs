using System.Drawing;
using RaspberryLightsDeviceWebApi.Enums;
using RaspberryLightsDeviceWebApi.Interfaces;
using RaspberryLightsDeviceWebApi.Models;
using rpi_ws281x;

namespace RaspberryLightsDeviceWebApi.Services;

public class LedStripService : ILedStripService
{
    private const int LedCount = 108;
    private readonly IOBD2Service _obd2Service;
    private static AnimationParameters _currentAnimationParameters = new AnimationParameters();

    public LedStripService(IOBD2Service obd2Service)
    {
        _obd2Service = obd2Service;
    }

    public AnimationParameters GetCurrentAnimationParameters()
    {
        return _currentAnimationParameters;
    }
    
    public async Task StartAnimation(AnimationParameters parameters, CancellationToken cancellationToken)
    {
        _currentAnimationParameters = parameters;
        
        var settings = Settings.CreateDefaultSettings();
        var controller = settings.AddController(LedCount, Pin.Gpio18, StripType.WS2812_STRIP, brightness: parameters.Brightness);

        using var device = new WS281x(settings);
        Func<int, Color> colorWheelFunc;

        switch (parameters.Animation)
        {
            case Animation.Rainbow:
                colorWheelFunc = RainbowWheel;
                break;
            case Animation.Fire:
                colorWheelFunc = FireWheel;
                break;
            case Animation.Ocean:
                colorWheelFunc = OceanWheel;
                break;
            case Animation.Forest:
                colorWheelFunc = ForestWheel;
                break;
            case Animation.ColorPulse:
                _obd2Service.StartReading();
                colorWheelFunc = index => ColorPulse(parameters.SystemDrawingColor, index);
                break;
            case Animation.ColorWave:
                _obd2Service.StartReading();
                colorWheelFunc = index => ColorWave(parameters.SystemDrawingColor, index);
                break;
            case Animation.Off:
            default:
                colorWheelFunc = _ => Color.Black;
                break;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            for (var i = 0; i < 256; i++)
            {
                for (var j = 0; j < LedCount; j++)
                {
                    if (parameters.Animation == Animation.ColorPulse)
                        controller.SetLED(j, ColorPulse(parameters.SystemDrawingColor, i));
                    else
                        controller.SetLED(j, colorWheelFunc((j * 256 / LedCount + i) % 255));
                }

                device.Render();

                if (cancellationToken.IsCancellationRequested)
                    break;

                await Delay(parameters, cancellationToken);
            }
        }

        if (parameters.SpeedType is SpeedType.VehicleSpeed or SpeedType.EngineRpm)
        {
            await _obd2Service.StopReading();
        }

        device.Reset();
        device.Dispose();
    }

    private async Task Delay(AnimationParameters parameters, CancellationToken cancellationToken)
    {
        byte scaledSpeed;

        switch (parameters.SpeedType)
        {
            case SpeedType.UserDefined:
                scaledSpeed = (byte)parameters.UserDefinedSpeed;
                break;
            case SpeedType.VehicleSpeed:
                var vehicleSpeed = await _obd2Service.GetCurrentSpeed();
                scaledSpeed = (byte)(vehicleSpeed / 140 * 255);
                break;
            case SpeedType.EngineRpm:
                var engineRpm = await _obd2Service.GetCurrentRpm();
                scaledSpeed = (byte)(engineRpm / 6000 * 255);
                break;
            default:
                throw new InvalidOperationException("Invalid Speed Type.");
        }

        var delay = 256 - scaledSpeed;
        await Task.Delay(delay, cancellationToken);
    }

    public void ClearLeds()
    {
        var settings = Settings.CreateDefaultSettings();
        settings.AddController(LedCount, Pin.Gpio18, StripType.WS2812_STRIP);

        using var device = new WS281x(settings);

        device.Reset();
        device.Dispose();
    }

    private static Color RainbowWheel(int wheelPosition)
    {
        int r, g, b;
        switch (wheelPosition)
        {
            case < 85:
                r = wheelPosition * 3;
                g = 255 - wheelPosition * 3;
                b = 0;
                break;
            case < 170:
                wheelPosition -= 85;
                r = 255 - wheelPosition * 3;
                g = 0;
                b = wheelPosition * 3;
                break;
            default:
                wheelPosition -= 170;
                r = 0;
                g = wheelPosition * 3;
                b = 255 - wheelPosition * 3;
                break;
        }

        // Normalize RGB values
        var maxValue = Math.Max(r, Math.Max(g, b));

        if (maxValue <= 0)
            return Color.FromArgb(r, g, b);

        var factor = 255.0 / maxValue;

        r = (int)(r * factor);
        g = (int)(g * factor);
        b = (int)(b * factor);

        return Color.FromArgb(r, g, b);
    }

    private static Color FireWheel(int wheelPosition)
    {
        const int r = 255;
        int g;

        if (wheelPosition < 170)
        {
            g = (int)(wheelPosition * (150.0 / 170));
        }
        else
        {
            wheelPosition -= 170;
            g = 150 - (int)(wheelPosition * (150.0 / 85));
        }

        return Color.FromArgb(r, g, 0);
    }


    private static Color ForestWheel(int wheelPosition)
    {
        int g, b;
        switch (wheelPosition)
        {
            case < 85:
                g = wheelPosition * 3;
                b = 255;
                break;
            case < 170:
                wheelPosition -= 85;
                g = 255;
                b = 255 - wheelPosition * 3;
                break;
            default:
                wheelPosition -= 170;
                g = 255 - wheelPosition * 3;
                b = wheelPosition * 3;
                break;
        }

        return Color.FromArgb(0, g, b);
    }

    private static Color OceanWheel(int wheelPosition)
    {
        int g, b;
        switch (wheelPosition)
        {
            case < 64:
                g = wheelPosition * 2;
                b = 255;
                break;
            case < 128:
                wheelPosition -= 64;
                g = 128 + wheelPosition * 2;
                b = 255;
                break;
            case < 192:
                wheelPosition -= 128;
                g = 255 - wheelPosition * 2;
                b = 255;
                break;
            default:
                wheelPosition -= 192;
                g = 128 - wheelPosition * 2;
                b = 255;
                break;
        }

        return Color.FromArgb(0, g, b);
    }

    private static Color ColorPulse(Color baseColor, int index)
    {
        var factor = (Math.Cos((double)index / 255 * 2 * Math.PI) + 1) / 2;

        return Color.FromArgb((int)(baseColor.R * factor), (int)(baseColor.G * factor), (int)(baseColor.B * factor));
    }

    private static Color ColorWave(Color baseColor, int index)
    {
        var factor = (Math.Sin((double)index / 255 * 2 * Math.PI) + 1) / 2;

        return Color.FromArgb((int)(baseColor.R * factor), (int)(baseColor.G * factor), (int)(baseColor.B * factor));
    }
}