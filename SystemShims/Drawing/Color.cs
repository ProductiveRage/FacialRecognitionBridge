namespace System.Drawing
{
	public struct Color
	{
		public static Color Red { get { return new Color(255, 0, 0); } } // Note: Auto-initialiser not used due to http://forums.bridge.net/forum/bridge-net-pro/bugs/3648
		public static Color GreenYellow { get { return new Color(173, 255, 47); } } // Note: Auto-initialiser not used due to http://forums.bridge.net/forum/bridge-net-pro/bugs/3648
		public static Color FromArgb(byte alpha, byte r, byte g, byte b) { return new Drawing.Color(r, g, b, alpha); }
		public static Color FromArgb(byte alpha, Color colour) { return new Drawing.Color(colour.R, colour.G, colour.B, alpha); }

		public Color(byte r, byte g, byte b, byte alpha = 255)
		{
			R = r;
			G = g;
			B = b;
			Alpha = alpha;
		}
		public byte R { get; }
		public byte G { get; }
		public byte B { get; }
		public byte Alpha { get; }
	}
}
