﻿namespace Common
{
	/// <summary>
	/// This is used instead of the .NET Color struct since that does work when each instance is created and, when we're using this, we just want to stash values
	/// </summary>
	public struct RGB
	{
		public RGB(byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
		}
		public byte R { get; }
		public byte G { get; }
		public byte B { get; }

		public override string ToString()
		{
			return $"RGB:{R}:{G}:{B}";
		}

		public double ToGreyScale()
		{
			return (0.2989 * R) + (0.5870 * G) + (0.1140 * B);
		}
	}
}
