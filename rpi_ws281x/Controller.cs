#region

using System.Drawing;

#endregion

namespace rpi_ws281x;

/// <summary>
///     Represents the controller which drives the LEDs
/// </summary>
public class Controller {
	/// <summary>
	///     Returns a value which indicates if the signal needs to be inverted.
	///     Set to true to invert the signal (when using NPN transistor level shift).
	/// </summary>
	public bool Invert { get; }

	/// <summary>
	///     Gets or sets the brightness of the LEDs
	///     0 = darkest, 255 = brightest
	/// </summary>
	public byte Brightness { get; set; }

	/// <summary>
	///     The type of controller (i.e. PWM, PCM, SPI  )
	/// </summary>
	/// <value></value>
	public ControllerType ControllerType { get; }

	/// <summary>
	///     The number of LEDs in the strip
	/// </summary>
	public int LEDCount {
		get => LedColors.Length;
		set {
			LedColors = new int[value];
			for (var i = 0; i < value; i++) {
				LedColors[i] = 0;
			}

			IsDirty = true;
		}
	}

	/// <summary>
	///     Returns the type of the channel.
	///     The type defines the ordering of the colors.
	/// </summary>
	public StripType StripType { get; }
	
	public bool IsRGBW { get; }

	/// <summary>
	///     Indicates if the colors assigned to the LED has changed and the LED should be updated.
	/// </summary>
	internal bool IsDirty { get; set; }

	/// <summary>
	///     Returns the GPIO pin which is connected to the LED strip
	/// </summary>
	internal int GPIOPin { get; }
	
	/// <summary>
	///		Keep an empty array in memory of our LED colors and use it for our rendering sequence.
	/// </summary>

	private int[] LedColors { get; set; }

	
	internal Controller(int ledCount, Pin pin, byte brightness, bool invert, StripType stripType,
		ControllerType controllerType) {
		IsDirty = false;

		GPIOPin = (int)pin;
		Invert = invert;
		Brightness = brightness;
		StripType = stripType;
		var cName = StripType.ToString();
		IsRGBW = cName.Contains("W") && cName.Contains("SK");
		ControllerType = controllerType;
		LedColors = new int[ledCount];
		for (var i = 0; i < ledCount; i++) {
			LedColors[i] = 0;
		}
	}

	/// <summary>
	///     Set LED to a Color
	/// </summary>
	/// <param name="ledID">LED to set (0 based)</param>
	/// <param name="color">Color to use</param>
	public void SetLED(int ledID, Color color) {
		if (IsRGBW) color = ColorClamp.ClampAlpha(color);
		LedColors[ledID] = color.ToArgb();
		IsDirty = true;
	}

	public void SetLEDS(Color[] color) {
		for (var i = 0; i < color.Length; i++) {
			if (IsRGBW) color[i] = ColorClamp.ClampAlpha(color[i]);
			LedColors[i] = color[i].ToArgb();
		}
		IsDirty = true;
	}

	/// <summary>
	///     Set all the LEDs in the strip to same color
	/// </summary>
	/// <param name="color">color to set all the LEDs</param>
	public void SetAll(Color color) {
		if (IsRGBW) color = ColorClamp.ClampAlpha(color);
		var na = new int[LEDCount];
		for (var i = 0; i < LEDCount; i++) {
			na[i] = color.ToArgb();
		}
		IsDirty = true;
	}


	/// <summary>
	///     Turn off all the LEDs in the strip
	/// </summary>
	public void Reset() {
		var na = new int[LEDCount];
		for (var i = 0; i < LEDCount; i++) {
			na[i] = 0;
		}

		LedColors = na;
		IsDirty = true;
	}

	/// <summary>
	///     array of LEDs with numeric color values
	/// </summary>
	/// <param name="clearDirty">reset dirty flag</param>
	internal int[] GetColors(bool clearDirty = false) {
		if (clearDirty) {
			IsDirty = false;
		}

		return LedColors;
	}
}