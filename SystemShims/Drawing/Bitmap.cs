using Bridge.Html5;

namespace System.Drawing
{
	public sealed class Bitmap : IDisposable
	{
		private readonly HTMLCanvasElement _canvas;
		private readonly CanvasRenderingContext2D _context;
		private bool _disposed;
		public Bitmap(int width, int height)
		{
			if (width < 0)
				throw new ArgumentOutOfRangeException(nameof(width));
			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height));

			_canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			_canvas.Width = width;
			_canvas.Height = height;
			_context = _canvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			_disposed = false;
		}
		public Bitmap(HTMLImageElement image)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));
			if (string.IsNullOrWhiteSpace(image.Src))
				throw new ArgumentException($"Specified {nameof(image)} must have a src property set");
			if (!image.Complete)
				throw new ArgumentException($"Specified {nameof(image)} must have fully loaded its content");

			_canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			_canvas.Width = image.Width;
			_canvas.Height = image.Height;
			_context = _canvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			_context.DrawImage(image, 0, 0, image.Width, image.Height);
			_disposed = false;
		}
		public Bitmap(Bitmap source, Size resizeTo)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if ((resizeTo.Width < 0) || (resizeTo.Height < 0))
				throw new ArgumentOutOfRangeException(nameof(resizeTo));

			_canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			_canvas.Width = resizeTo.Width;
			_canvas.Height = resizeTo.Height;
			_context = _canvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			_context.DrawImage(source._canvas, 0, 0, resizeTo.Width, resizeTo.Height);
			_disposed = false;
		}
		private Bitmap(HTMLCanvasElement canvas, CanvasRenderingContext2D context)
		{
			if (canvas == null)
				throw new ArgumentNullException(nameof(canvas));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			_canvas = canvas;
			_context = context;
			_disposed = false;
		}

		public int Width { get { return _canvas.Width; } }
		public int Height { get { return _canvas.Height; } }
		public Size Size { get { return new Size(Width, Height); } }

		public Bitmap ExtractSection(Rectangle area)
		{
			if ((area.Left < 0) || (area.Top < 0) || (area.Right > Width) || (area.Bottom > Height))
				throw new ArgumentOutOfRangeException(nameof(area));

			var canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			canvas.Width = area.Width;
			canvas.Height = area.Height;
			var context = canvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			context.PutImageData(
				_context.GetImageData(area.Left, area.Top, area.Width, area.Height),
				0,
				0
			);
			return new Bitmap(canvas, context);
		}

		public Bitmap ExpandCanvas(Size size)
		{
			if ((size.Width < Width) || (size.Height < Height))
				throw new ArgumentOutOfRangeException(nameof(size));

			var canvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			canvas.Width = size.Width;
			canvas.Height = size.Height;
			var context = canvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			context.BeginPath();
			context.Rect(0, 0, size.Width, size.Height);
			context.FillStyle = "black";
			context.Fill();
			context.PutImageData(
				_context.GetImageData(0, 0, Width, Height),
				(int)((size.Width - Width) / 2),
				(int)((size.Height - Height) / 2)
			);
			return new Bitmap(canvas, context);
		}

		public Color[,] GetColourData()
		{
			var data = _context.GetImageData(0, 0, Width, Height).Data;
			var colours = new Color[Width, Height];
			for (var x = 0; x < Width; x++)
			{
				for (var y = 0; y < Height; y++)
				{
					var startIndex = (y * (Width * 4)) + (x * 4);
					colours[x, y] = new Color(
						(byte)data[startIndex],
						(byte)data[startIndex + 1],
						(byte)data[startIndex + 2]
					);
				}
			}
			return colours;
		}

		public void SetColourData(Color[,] colours)
		{
			if (colours == null)
				throw new ArgumentNullException(nameof(colours));
			if ((colours.GetLowerBound(0) != 0) || (colours.GetLowerBound(0) >= Width) || (colours.GetLowerBound(1) != 0) || (colours.GetLowerBound(1) >= Height))
				throw new ArgumentOutOfRangeException(nameof(colours));

			var imageData = _context.GetImageData(0, 0, Width, Height);
			var data = imageData.Data;
			for (var x = 0; x < Width; x++)
			{
				for (var y = 0; y < Height; y++)
				{
					var colour = colours[x, y];
					var startIndex = (y * (Width * 4)) + (x * 4);
					data[startIndex] = colour.R;
					data[startIndex + 1] = colour.G;
					data[startIndex + 2] = colour.B;
					data[startIndex + 3] = 255; // alpha
				}
			}
			_context.PutImageData(imageData, 0, 0);
		}
		public void SetPixel(int x, int y, Color colour)
		{
			if ((x < 0) || (x >= Width))
				throw new ArgumentOutOfRangeException(nameof(x));
			if ((y < 0) || (y >= Width))
				throw new ArgumentOutOfRangeException(nameof(y));

			var imageData = _context.GetImageData(x, y, Width, Height);
			var data = imageData.Data;
			data[0] = colour.R;
			data[1] = colour.G;
			data[2] = colour.B;
			data[3] = colour.Alpha;
			_context.PutImageData(imageData, x, y);
		}

		public void DrawImage(Bitmap image, PointF offset)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			_context.DrawImage(image._canvas, offset.X, offset.Y);
		}

		public void DrawLine(Pen pen, float left, float top, float right, float bottom)
		{
			if (pen == null)
				throw new ArgumentNullException(nameof(pen));

			_context.BeginPath();
			_context.MoveTo(left, top);
			_context.LineTo(right, bottom);
			_context.StrokeStyle = GetStrokeStyle(pen);
			_context.LineWidth = pen.Width;
			_context.Stroke();
		}

		public void DrawRectangle(Pen pen, float x, float y, float width, float height)
		{
			if (pen == null)
				throw new ArgumentNullException(nameof(pen));

			_context.Rect(x, y, width, height);
			_context.StrokeStyle = GetStrokeStyle(pen);
			_context.LineWidth = pen.Width;
			_context.Stroke();
		}

		private static string GetStrokeStyle(Pen pen)
		{
			if (pen == null)
				throw new ArgumentNullException(nameof(pen));

			return "#" + pen.Colour.R.ToString("X2") + pen.Colour.G.ToString("X2") + pen.Colour.B.ToString("X2");
		}

		public void Save(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException($"Null/blank {nameof(path)} specified");

			var clonedCanvas = Document.CreateElement<HTMLCanvasElement>("canvas");
			clonedCanvas.Width = Width;
			clonedCanvas.Height = Height;
			var clonedContext = clonedCanvas.GetContext(CanvasTypes.CanvasContext2DType.CanvasRenderingContext2D);
			clonedContext.DrawImage(_canvas, 0, 0, Width, Height);

			var header = Document.CreateElement("h2");
			header.TextContent = path;

			var container = Document.CreateElement("div");
			container.AppendChild(header);
			container.AppendChild(clonedCanvas);

			Document.Body.AppendChild(container);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_canvas.Remove();
			_disposed = true;
		}
	}
}
