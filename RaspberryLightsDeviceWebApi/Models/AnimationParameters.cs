using System.Drawing;
using RaspberryLightsDeviceWebApi.Enums;

namespace RaspberryLightsDeviceWebApi.Models;

public class AnimationParameters
{
    public Animation Animation { get; set; } = Animation.Off;
    public SimpleColor CustomColor { get; set; } = new SimpleColor();
    public SpeedType SpeedType { get; set; } = SpeedType.UserDefined;
    public byte UserDefinedSpeed { get; set; } = byte.MaxValue; // todo - test the change of the property type
    public byte Brightness { get; set; } = byte.MaxValue;

    public Color SystemDrawingColor => Color.FromArgb(CustomColor.R, CustomColor.G, CustomColor.B);

    public class SimpleColor
    {
        public byte R { get; set; } = 0; // todo - test this default values
        public byte G { get; set; } = 0;
        public byte B { get; set; } = 0;
    }
}