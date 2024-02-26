using OBD.NET.Communication;
using OBD.NET.Devices;
using OBD.NET.Logging;
using OBD.NET.OBDData;
using RaspberryLightsDeviceWebApi.Interfaces;

namespace RaspberryLightsDeviceWebApi.Services
{
    public class OBD2Service : IOBD2Service
    {
        private const string ComPort = "/dev/rfcomm0";
        private readonly Lazy<ELM327> _device;
        private volatile int _currentSpeed = 0;
        private volatile int _currentRpm = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _readSpeedTask;
        private Task _readRpmTask;

        public OBD2Service()
        {
            _device = new Lazy<ELM327>(() =>
            {
                var connection = new SerialConnection(ComPort);
                var device = new ELM327(connection, new OBDConsoleLogger(OBDLogLevel.Debug));
                device.Initialize();
                return device;
            });
        }

        private async Task ReadSpeed(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var speedData = await _device.Value.RequestDataAsync<VehicleSpeed>();
                    _currentSpeed = speedData?.Speed ?? 0;
                }
                catch
                {
                    _currentSpeed = 0;
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        private async Task ReadRpm(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var rpmData = await _device.Value.RequestDataAsync<EngineRPM>();
                    _currentRpm = rpmData?.Rpm ?? 0;
                }
                catch
                {
                    _currentRpm = 0;
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        public Task<int> GetCurrentSpeed()
        {
            return Task.FromResult(_currentSpeed);
        }

        public Task<int> GetCurrentRpm()
        {
            return Task.FromResult(_currentRpm);
        }

        public void StartReading()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _readSpeedTask = Task.Run(() => ReadSpeed(_cancellationTokenSource.Token));
            _readRpmTask = Task.Run(() => ReadRpm(_cancellationTokenSource.Token));
        }

        public async Task StopReading()
        {
            _cancellationTokenSource.Cancel();
            await Task.WhenAll(_readSpeedTask, _readRpmTask);
        }
    }
}
