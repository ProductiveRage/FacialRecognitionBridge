namespace System.Drawing
{
	public struct Rectangle
	{
		public static Rectangle FromLTRB(int left, int top, int right, int bottom)
		{
			return new Rectangle(x: left, y: top, width: right - left, height: bottom - top);
		}

		public Rectangle(int x, int y, int width, int height)
		{
			if (width < 0)
				throw new ArgumentOutOfRangeException(nameof(width));
			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height));

			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		public Rectangle(Point topLeft, Size size) : this(topLeft.X, topLeft.Y, size.Width, size.Height) { }

		public int X { get; private set; }
		public int Y { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public int Top { get { return Y; } }
		public int Bottom { get { return Top + Height; } }
		public int Left { get { return X; } }
		public int Right { get { return Left + Width; } }

		public bool Contains(Point point)
		{
			return 
				(point.X >= Left) && (point.X < Right) &&
				(point.Y >= Top) && (point.Y < Bottom);
		}

		public void Inflate(int width, int height)
		{
			if (width < 0)
				throw new ArgumentOutOfRangeException(nameof(width));
			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height));

			X = X - (int)(width / 2);
			Y = Y - (int)(height / 2);
			Width = Width + width;
			Height = Height + height;
		}

		public void Intersect(Rectangle other)
		{
			var left = Math.Max(Left, other.Left);
			var top = Math.Max(Top, other.Top);
			var right = Math.Min(Right, other.Right);
			var bottom = Math.Min(Bottom, other.Bottom);
			if ((left >= right) || (top >= bottom))
			{
				// The .NET library will set the rectangle to Empty if there is no intersection (it doesn't throw)
				X = 0;
				Y = 0;
				Width = 0;
				Height = 0;
			}
			else
			{
				X = left;
				Y = top;
				Width = right - left;
				Height = bottom - top;
			}
		}

		public override string ToString()
		{
			return $"(X: {Left}-{Right}, Y: {Top}-{Bottom})";
		}
	}
}
