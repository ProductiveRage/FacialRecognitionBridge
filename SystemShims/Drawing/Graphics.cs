namespace System.Drawing
{
	public sealed class Graphics : IDisposable
	{
		public static Graphics FromImage(Bitmap image) { return new Graphics(image); }

		private readonly Bitmap _image;
		private Graphics(Bitmap image)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			_image = image;
		}

		public void DrawImage(Bitmap image, PointF offset) { _image.DrawImage(image, offset); }
		public void DrawLine(Pen pen, float left, float top, float right, float bottom) { _image.DrawLine(pen, left, top, right, bottom); }
		public void DrawRectangle(Pen pen, float x, float y, float width, float height) { _image.DrawRectangle(pen, x, y, width, height); }

		public void Dispose() { }
	}
}
