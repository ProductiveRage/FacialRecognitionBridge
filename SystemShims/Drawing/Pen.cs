namespace System.Drawing
{
	public sealed class Pen : IDisposable
	{
		public Pen(Color colour, int width)
		{
			if (width <= 0)
				throw new ArgumentOutOfRangeException(nameof(width));

			Colour = colour;
			Width = width;
		}

		public Color Colour { get; }
		public int Width { get; }

		public void Dispose() { }
	}
}
