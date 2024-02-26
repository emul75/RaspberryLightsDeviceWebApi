using System.Drawing;
using RaspberryLightsDeviceWebApi.Enums;

namespace RaspberryLightsDeviceWebApi.Models;

public class AnimationParameters
{
    public Animation Animation { get; set; }
    public SimpleColor CustomColor { get; set; }
    public SpeedType SpeedType { get; set; }
    public int UserDefinedSpeed { get; set; }
    public byte Brightness { get; set; }

    public Color SystemDrawingColor => Color.FromArgb(CustomColor.R, CustomColor.G, CustomColor.B);

    public class SimpleColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }
}