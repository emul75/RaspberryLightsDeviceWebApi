#region

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using rpi_ws281x.Native;

#endregion

namespace rpi_ws281x;

/// <summary>
///     Wrapper class to control WS281x LEDs
/// </summary>
public class WS281x : IDisposable {
	private readonly Dictionary<int, Controller> _controllers;
	private bool _isDisposingAllowed;
	private ws2811_t _ws2811;
	private GCHandle _ws2811Handle;

	/// <summary>
	///     Initialize the wrapper
	/// </summary>
	/// <param name="settings">Settings used for initialization</param>
	public WS281x(Settings settings) {
		_ws2811 = new ws2811_t {
			dmanum = settings.DMAChannel,
			freq = settings.Frequency,
			channel_0 = InitChannel(0, settings.Controllers),
			channel_1 = InitChannel(1, settings.Controllers)
		};

		//Pin the object in memory. Otherwise GC will probably move the object to another memory location.
		//This would cause errors because the native library has a pointer on the memory location of the object.
		_ws2811Handle = GCHandle.Alloc(_ws2811, GCHandleType.Pinned);

		var initResult = PInvoke.ws2811_init(ref _ws2811);
		if (initResult != ws2811_return_t.WS2811_SUCCESS) {
			throw WS281xException.Create(initResult, "initializing");
		}

		// save a copy of the controllers - used to update LEDs
		_controllers = new Dictionary<int, Controller>(settings.Controllers);

		// if specified, apply gamma correction
		if (settings.GammaCorrection != null) {
			if (settings.Controllers.ContainsKey(0)) {
				Marshal.Copy(settings.GammaCorrection.ToArray(), 0, _ws2811.channel_0.gamma,
					settings.GammaCorrection.Count);
			}

			if (settings.Controllers.ContainsKey(1)) {
				Marshal.Copy(settings.GammaCorrection.ToArray(), 0, _ws2811.channel_1.gamma,
					settings.GammaCorrection.Count);
			}
		}

		//Disposing is only allowed if the init was successful.
		//Otherwise the native cleanup function throws an error.
		_isDisposingAllowed = true;
	}

	/// <summary>
	///     Renders the content of the channels
	/// </summary>
	/// <param name="force">Force LEDs to updated - default only updates if when a change is done</param>
	public void Render(bool force = false) {
		var shouldRender = false;

		if (_controllers.ContainsKey(0) && (force || _controllers[0].IsDirty)) {
			var ledColor = _controllers[0].GetColors(true);
			Marshal.Copy(ledColor, 0, _ws2811.channel_0.leds, ledColor.Length);
			shouldRender = true;
		}

		if (_controllers.ContainsKey(1) && (force || _controllers[1].IsDirty)) {
			var ledColor = _controllers[1].GetColors(true);
			Marshal.Copy(ledColor, 0, _ws2811.channel_1.leds, ledColor.Length);
			shouldRender = true;
		}

		if (shouldRender) {
			var result = PInvoke.ws2811_render(ref _ws2811);
			if (result != ws2811_return_t.WS2811_SUCCESS) {
				throw WS281xException.Create(result, "rendering");
			}
		}
	}


	/// <summary>
	///     Get the brightness of a controller
	/// </summary>
	/// <param name="controllerId">The ID of the controller (0 or 1)</param>
	/// <returns></returns>
	public int GetBrightness(int controllerId = 0) {
		if (!_controllers.ContainsKey(controllerId)) {
			return 0;
		}

		var controller = _controllers[controllerId];
		return controller.Brightness;
	}

	/// <summary>
	///     Update the strip's brightness
	/// </summary>
	/// <param name="brightness">New brightness (0-255)</param>
	/// ///
	/// <param name="controllerId">The ID of the controller (0 or 1)</param>
	public void SetBrightness(int brightness, int controllerId = 0) {
		if (!_controllers.ContainsKey(controllerId)) {
			return;
		}

		var controller = _controllers[controllerId];

		controller.Brightness = (byte)brightness;
		if (controller.ControllerType == ControllerType.PWM1) {
			_ws2811.channel_1.brightness = (byte)brightness;
		} else {
			_ws2811.channel_0.brightness = (byte)brightness;
		}
	}

	/// <summary>
	///     Clear all LEDs
	/// </summary>
	public void Reset() {
		foreach (var controller in _controllers.Values) {
			controller.Reset();
			controller.IsDirty = false;
		}

		Render(true);
	}

	public Controller GetController(ControllerType controllerType = ControllerType.PWM0) {
		var channelNumber = controllerType == ControllerType.PWM1 ? 1 : 0;
		if (_controllers.ContainsKey(channelNumber) &&
		    _controllers[channelNumber].ControllerType == controllerType) {
			return _controllers[channelNumber];
		}

		return null;
	}

	/// <summary>
	///     Initialize the channel properties
	/// </summary>
	/// <param name="channelIndex">Index of the channel tu initialize</param>
	/// <param name="controllers">Controller Settings</param>
	private static ws2811_channel_t InitChannel(int channelIndex, IReadOnlyDictionary<int, Controller> controllers) {
		var channel = new ws2811_channel_t();

		if (!controllers.ContainsKey(channelIndex)) {
			return channel;
		}

		channel.count = controllers[channelIndex].LEDCount;
		channel.gpionum = controllers[channelIndex].GPIOPin;
		channel.brightness = controllers[channelIndex].Brightness;
		channel.invert = Convert.ToInt32(controllers[channelIndex].Invert);

		if (controllers[channelIndex].StripType != StripType.Unknown) {
			//Strip type is set by the native assembly if not explicitly set.
			//This type defines the ordering of the colors e. g. RGB or GRB, ...
			channel.strip_type = (int)controllers[channelIndex].StripType;
		}

		return channel;
	}


	#region IDisposable Support

	private bool disposedValue; // To detect redundant calls

	protected virtual void Dispose(bool disposing) {
		if (disposedValue) {
			return;
		}

		if (disposing) {
			// Nothing to do.
		}

		if (_isDisposingAllowed) {
			PInvoke.ws2811_fini(ref _ws2811);
			_ws2811Handle.Free();

			_isDisposingAllowed = false;
		}

		disposedValue = true;
	}

	~WS281x() {
		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		Dispose(false);
	}

	// This code added to correctly implement the disposable pattern.
	public void Dispose() {
		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion
}