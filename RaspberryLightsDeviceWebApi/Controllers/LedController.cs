using Microsoft.AspNetCore.Mvc;
using RaspberryLightsDeviceWebApi.Interfaces;
using RaspberryLightsDeviceWebApi.Models;

namespace RaspberryLightsDeviceWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LedController : ControllerBase
{
    private readonly ILedStripService _ledService;
    private readonly INgrokService _ngrokService;

    private static CancellationTokenSource _animationCts = new();
    private Task _runningAnimation = Task.CompletedTask;


    public LedController(ILedStripService ledService, IConfiguration configuration, INgrokService ngrokService)
    {
        _ledService = ledService;
        _ngrokService = ngrokService;
    }

    [HttpGet("testConnection")]
    public Task<IActionResult> TestConnection()
    {
        return Task.FromResult<IActionResult>(Ok());
    }

    [HttpGet("startNgrok")]
    public async Task<IActionResult> StartNgrok()
    {
        await _ngrokService.StartNgrokTunnel();
        return Ok();
    }

    [HttpGet("updateIp")]
    public async Task<IActionResult> UpdateDeviceIp()
    {
        await _ngrokService.UpdatePublicUrl();
        return Ok();
    }

    [HttpPost("startAnimation")]
    public async Task<IActionResult> StartAnimation([FromBody] AnimationParameters parameters)
    {
        await StopRunningAnimation();
        _animationCts = new CancellationTokenSource();
        _runningAnimation = _ledService.StartAnimation(parameters, _animationCts.Token);

        Console.WriteLine($@"simplecolor: {parameters.CustomColor.R} {parameters.CustomColor.G} {parameters.CustomColor.B} converted: {parameters.SystemDrawingColor}");
        
        return Ok();
    }

    [HttpGet("stopAnimation")]
    public async Task<IActionResult> EndAnimation()
    {
        await StopRunningAnimation();
        _ledService.ClearLeds();

        return Ok();
    }

    private async Task StopRunningAnimation()
    {
        try
        {
            _animationCts.Cancel();
            await _runningAnimation;
            await Task.Delay(50);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("TaskCanceledException");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error {e.Message}");
        }
    }
}