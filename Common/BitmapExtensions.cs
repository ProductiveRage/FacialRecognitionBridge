using System;
using System.Drawing;

namespace Common
{
	public static class BitmapExtensions
	{
		public static DataRectangle<RGB> GetRGB(this Bitmap image)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			var imageColours = image.GetColourData();
			var values = new RGB[image.Width, image.Height];
			for (var x = 0; x < image.Width; x++)
			{
				for (var y = 0; y < image.Height; y++)
				{
					var imageColour = imageColours[x, y];
					values[x, y] = new RGB(imageColour.R, imageColour.G, imageColour.B);
				}
			}
			return DataRectangle.For(values);
		}

		public static void SetRGB(this Bitmap image, DataRectangle<RGB> values)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			if ((values.Width != image.Width) || (values.Height != image.Height))
				throw new ArgumentOutOfRangeException(nameof(values));

			var imageColours = new Color[image.Width, image.Height];
			for (var x = 0; x < image.Width; x++)
			{
				for (var y = 0; y < image.Height; y++)
				{
					var value = values[x, y];
					imageColours[x, y] = new Color(value.R, value.G, value.B);
				}
			}
			image.SetColourData(imageColours);
		}

		/// <summary>
		/// If the aspect ratio of the region does not match of the destinationSize then black bars will appear around the image (either above and below or to the sides, depending
		/// upon in which dimension the aspect ratio must be adjusted)
		/// </summary>
		public static Bitmap ExtractImageSectionAndResize(this Bitmap source, Rectangle region, Size destinationSize)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if ((region.Left < 0) || (region.Top < 0) || (region.Right > source.Width) || (region.Bottom > source.Height))
				throw new ArgumentOutOfRangeException(nameof(destinationSize));
			if ((destinationSize.Width <= 0) || (destinationSize.Height <= 0))
				throw new ArgumentOutOfRangeException(nameof(destinationSize));

			// Try to extract a region around the specified region that is as close to the destination size as possible
			var aspectRatio = (double)region.Width / region.Height;
			var sampleAspectRatio = (double)destinationSize.Width / destinationSize.Height;
			if (aspectRatio >= sampleAspectRatio)
			{
				// The specified region is wider (proportionally) than the desired destination size, so we want to try to increase the region's height to get as close as
				// possible to the destination size aspect ratio
				var idealRegionHeight = (int)Math.Round(region.Width / sampleAspectRatio);
				region.Inflate(0, idealRegionHeight - region.Height);
			}
			else
			{
				var idealRegionWidth = (int)Math.Round(region.Height * sampleAspectRatio);
				region.Inflate(idealRegionWidth - region.Width, 0);
			}
			region.Intersect(new Rectangle(0, 0, source.Width, source.Height)); // Ensure that that region-expanding above doesn't push outside of the available source space

			// Now take that region (which may or may not be the ideal aspect ratio - if the specified region was close to the edge of the image then it might not have been possible
			// to expand it sufficiently to match the desired aspect ratio precisely) and resize it down. If the aspect ratio couldn't be matched perfectly then expand the canvas of
			// the resized region (which means that black bars will appear above/below or left/right).
			using (var extractedRegion = source.ExtractSection(region))
			{
				aspectRatio = (double)region.Width / region.Height;
				int widthToResizeRegionTo, heightToResizeRegionTo;
				if (aspectRatio >= sampleAspectRatio)
				{
					// The image is wider than we want so the width is the limiting factor - shrink that down to fit and then expand the canvas so that we get some padding to
					// bump the height up to the desired size
					widthToResizeRegionTo = destinationSize.Width;
					heightToResizeRegionTo = (int)Math.Round(widthToResizeRegionTo / aspectRatio);
				}
				else
				{
					heightToResizeRegionTo = destinationSize.Height;
					widthToResizeRegionTo = (int)Math.Round(heightToResizeRegionTo * aspectRatio);
				}
				using (var extractedRegionResized = new Bitmap(extractedRegion, new Size(widthToResizeRegionTo, heightToResizeRegionTo)))
				{
					return extractedRegionResized.ExpandCanvas(destinationSize);
				}
			}
		}
	}
}
