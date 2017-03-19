﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Common;

namespace FaceDetection
{
	public sealed class FaceDetector : ILookForPossibleFaceRegions
	{
		private readonly IExposeConfigurationOptions _config;
		private readonly Action<string> _logger;
		public FaceDetector(IExposeConfigurationOptions config, Action<string> logger)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			_config = config;
			_logger = logger;
		}
		public FaceDetector(Action<string> logger) : this(DefaultConfiguration.Instance, logger) { }

		public IEnumerable<Rectangle> GetPossibleFaceRegions(Bitmap source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var timer = Stopwatch.StartNew();
			var largestDimension = Math.Max(source.Width, source.Height);
			var scaleDown = (largestDimension > _config.MaximumImageDimension) ? ((double)largestDimension / _config.MaximumImageDimension) : 1;
			var colourData = (scaleDown > 1) ? GetResizedBitmapData(source, scaleDown) : source.GetRGB();
			_logger("Loaded pixel colour data" + ((scaleDown > 1) ? " (scale down: " + scaleDown + ")" : ""));
			var faceRegions = GetPossibleFaceRegionsFromColourData(colourData);
			if (scaleDown > 1)
				faceRegions = faceRegions.Select(region => Scale(region, scaleDown, source.Size));
			_logger($"Complete - {faceRegions.Count()} region(s) identified by skin tone face detector [total time: {timer.ElapsedMilliseconds}ms]");
			return faceRegions;
		}

		private IEnumerable<Rectangle> GetPossibleFaceRegionsFromColourData(DataRectangle<RGB> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var scale = _config.CalculateScale(source.Width, source.Height);
			_logger($"Loaded file - Dimensions: {source.Width}x{source.Height}, Skin Tone Filter Scale: {scale}");

			var colourData = CorrectZeroResponse(source);
			_logger("Corrected zero response");

			var rgByIValues = _config.IRgByCalculator(colourData);
			_logger("Calculated I/RgBy values");

			// To compute texture amplitude -
			//  1. The intensity image was smoothed with a median filter of radius 4 * SCALE (8 for Jay Kapur method)
			//  2. The result was subtracted from the original image
			//  3. The absolute values of these differences are then run through a second median filter of radius 6 * SCALE (12 for Jay Kapur method)
			var smoothedIntensity = rgByIValues.MedianFilter(value => value.I, _config.TextureAmplitudeFirstPassSmoothenMultiplier * scale);
			var differenceBetweenOriginalIntensityAndSmoothIntensity = rgByIValues.CombineWith(smoothedIntensity, (x, y) => Math.Abs(x.I - y));
			var textureAmplitude = differenceBetweenOriginalIntensityAndSmoothIntensity.MedianFilter(value => value, _config.TextureAmplitudeSecondPassSmoothenMultiplier * scale);
			_logger("Calculated texture amplitude");

			// The Rg and By arrays are smoothed with a median filter of radius 2 * SCALE, to reduce noise.
			var smoothedRg = rgByIValues.MedianFilter(value => value.Rg, _config.RgBySmoothenMultiplier * scale);
			var smoothedBy = rgByIValues.MedianFilter(value => value.By, _config.RgBySmoothenMultiplier * scale);
			var smoothedHues = smoothedRg.CombineWith(
				smoothedBy,
				(rg, by, coordinates) =>
				{
					var hue = RadianToDegree(Math.Atan2(rg, by));
					var saturation = Math.Sqrt((rg * rg) + (by * by));
					return new HueSaturation(hue, saturation, textureAmplitude[coordinates.X, coordinates.Y]);
				}
			);
			_logger("Calculated hue data");

			if (_config.SaveProgressImages)
			{
				using (var hueSaturationPreview = new Bitmap(smoothedHues.Width, smoothedHues.Height))
				{
					hueSaturationPreview.SetRGB(
						smoothedHues.Transform(hueSaturation =>
						{
							var intentity = (byte)((hueSaturation.Hue + 180) / 2); // Hue is a value from -180 to 180 so ((x+180/3) gets us within 0-255
							return new RGB(intentity, intentity, intentity);
						})
					);
					hueSaturationPreview.Save("SkinMaskGeneration-Hue.png");
				}
				using (var saturationPreview = new Bitmap(smoothedHues.Width, smoothedHues.Height))
				{
					saturationPreview.SetRGB(
						smoothedHues.Transform(hueSaturation =>
						{
							var intensity = (byte)hueSaturation.Saturation;
							return new RGB(intensity, intensity, intensity);
						})
					);
					saturationPreview.Save("SkinMaskGeneration-Saturation.png");
				}
				using (var textureAmplitudePreview = new Bitmap(textureAmplitude.Width, textureAmplitude.Height))
				{
					textureAmplitudePreview.SetRGB(
						textureAmplitude.Transform(amplitude =>
						{
							var intensity = (byte)(amplitude * 16);
							return new RGB(intensity, intensity, intensity);
						})
					);
					textureAmplitudePreview.Save("SkinMaskGeneration-TextureAmplitude.png");
				}
			}

			// Generate a mask of pixels identified as skin
			var skinMask = smoothedHues.Transform(transformer: _config.SkinFilter);
			_logger("Built initial skin mask");

			if (_config.SaveProgressImages)
			{
				using (var skinMaskPreviewImage = new Bitmap(skinMask.Width, skinMask.Height))
				{
					skinMaskPreviewImage.SetRGB(
						skinMask.Transform(isSkin => isSkin ? new RGB(255, 255, 255) : new RGB(0, 0, 0))
					);
					skinMaskPreviewImage.Save("SkinMask1.png");
				}
			}

			// Now expand the mask to include any adjacent points that match a less strict filter (which "helps to enlarge the skin map regions to include skin/background
			// border pixels, regions near hair or other features, or desaturated areas" - as per Jay Kapur, though he recommends five iterations and I think that a slightly
			// higher value may provide better results)
			for (var i = 0; i < _config.NumberOfSkinMaskRelaxedExpansions; i++)
			{
				skinMask = skinMask.CombineWith(
					smoothedHues,
					(mask, hue, coordinates) =>
					{
						if (mask)
							return true;
						if (!_config.RelaxedSkinFilter(hue))
							return false;
						var surroundingArea = smoothedHues.GetRectangleAround(coordinates, distanceToExpandLeftAndUp: 1, distanceToExpandRightAndDown: 1);
						return skinMask.AnyValuesMatch(surroundingArea, adjacentMask => adjacentMask);
					}
				);
			}
			_logger($"Expanded initial skin mask (fixed loop count of {_config.NumberOfSkinMaskRelaxedExpansions})");

			if (_config.SaveProgressImages)
			{
				using (var skinMaskPreviewImage = new Bitmap(skinMask.Width, skinMask.Height))
				{
					skinMaskPreviewImage.SetRGB(
						skinMask.Transform(isSkin => isSkin ? new RGB(255, 255, 255) : new RGB(0, 0, 0))
					);
					skinMaskPreviewImage.Save("SkinMask2.png");
				}
			}

			// Jay Kapur takes the skin map and multiplies by a greyscale conversion of the original image, then stretches the histogram to improve contrast, finally taking a
			// threshold of 95-240 to mark regions that show skin areas. This is approximated here by combining the skin map with greyscale'd pixels from the original data and
			// using a slightly different threshold range.
			skinMask = colourData.CombineWith(
				skinMask,
				(colour, mask) =>
				{
					if (!mask)
						return false;
					var intensity = colour.ToGreyScale();
					return (intensity >= 90) && (intensity <= 240);
				}
			);
			_logger("Completed final skin mask");

			if (_config.SaveProgressImages)
			{
				using (var skinMaskPreviewImage = new Bitmap(skinMask.Width, skinMask.Height))
				{
					skinMaskPreviewImage.SetRGB(
						skinMask.Transform(isSkin => isSkin ? new RGB(255, 255, 255) : new RGB(0, 0, 0))
					);
					skinMaskPreviewImage.Save("SkinMask3.png");
				}
			}

			var faceRegions = _config.FaceRegionAspectRatioFilter(
					IdentifyFacesFromSkinMask(skinMask)
				)
				.Select(faceRegion => ExpandRectangle(faceRegion, _config.PercentToExpandFinalFaceRegionBy, new Size(source.Width, source.Height)))
				.ToArray();
			_logger("Identified face regions");
			return faceRegions;
		}

		private static Rectangle ExpandRectangle(Rectangle area, double percentageToAdd, Size imageSize)
		{
			if ((area.Left < 0) || (area.Top < 0) || (area.Right > imageSize.Width) || (area.Bottom > imageSize.Height))
				throw new ArgumentOutOfRangeException(nameof(area));
			if (percentageToAdd < 0)
				throw new ArgumentOutOfRangeException(nameof(percentageToAdd));
			if ((imageSize.Width <= 0) || (imageSize.Height <= 0))
				throw new ArgumentOutOfRangeException(nameof(imageSize));
			 
			area.Inflate((int)Math.Round(area.Width * percentageToAdd), (int)Math.Round(area.Height * percentageToAdd)); // Rectangle is a struct so we're not messing with the caller's Rectangle reference
			area.Intersect(new Rectangle(new Point(0, 0), imageSize));
			return area;
		}

		private IEnumerable<Rectangle> IdentifyFacesFromSkinMask(DataRectangle<bool> skinMask)
		{
			if (skinMask == null)
				throw new ArgumentNullException(nameof(skinMask));

			// Identify potential objects from positive image (build a list of all skin points, take the first one and flood fill from it - recording the results as one object
			// and remove all points from the list, then do the same for the next skin point until there are none left)
			var skinPoints = new HashSet<Point>(
				skinMask.Enumerate((point, isMasked) => isMasked).Select(point => point.Item1)
			);
			var scale = _config.CalculateScale(skinMask.Width, skinMask.Height);
			var skinObjects = new List<Point[]>();
			while (skinPoints.Any())
			{
				var currentPoint = skinPoints.First();
				var pointsInObject = TryToGetPointsInObject(skinMask, currentPoint, new Rectangle(0, 0, skinMask.Width, skinMask.Height)).ToArray();
				foreach (var point in pointsInObject)
					skinPoints.Remove(point);
				skinObjects.Add(pointsInObject);
			}
			skinObjects = skinObjects.Where(skinObject => skinObject.Length >= (64 * scale)).ToList(); // Ignore any very small regions

			if (_config.SaveProgressImages)
			{
				var skinMaskPreviewImage = new Bitmap(skinMask.Width, skinMask.Height);
				var skinObjectPreviewColours = new[] { new RGB(255, 0, 0), new RGB(0, 255, 0), new RGB(0, 0, 255), new RGB(128, 128, 0), new RGB(0, 128, 128), new RGB(128, 0, 128) };
				var allSkinObjectPoints = skinObjects.Select((o, i) => new { Points = new HashSet<Point>(o), Colour = skinObjectPreviewColours[i % skinObjectPreviewColours.Length] }).ToArray();
				skinMaskPreviewImage.SetRGB(
					skinMask.Transform((isSkin, point) =>
					{
						var firstObject = allSkinObjectPoints.FirstOrDefault(o => o.Points.Contains(point));
						return (firstObject == null) ? new RGB(0, 0, 0) : firstObject.Colour;
					})
				);
				skinMaskPreviewImage.Save("SkinObjects.png");
			}

			// Look for any fully enclosed holes in each skin object (do this by flood filling from negative points and ignoring any where the fill gets to the edges of object)
			var boundsForSkinObjects = new List<Rectangle>();
			foreach (var skinObject in skinObjects)
			{
				var xValues = skinObject.Select(p => p.X).ToArray();
				var yValues = skinObject.Select(p => p.Y).ToArray();
				var left = xValues.Min();
				var top = yValues.Min();
				var skinObjectBounds = new Rectangle(left, top, width: (xValues.Max() - left) + 1, height: (yValues.Max() - top) + 1);
				var negativePointsInObject = new HashSet<Point>(
					skinMask.Enumerate((point, isMasked) => !isMasked && skinObjectBounds.Contains(point)).Select(point => point.Item1)
				);
				while (negativePointsInObject.Any())
				{
					var currentPoint = negativePointsInObject.First();
					var pointsInFilledNegativeSpace = TryToGetPointsInObject(skinMask, currentPoint, skinObjectBounds).ToArray();
					foreach (var point in pointsInFilledNegativeSpace)
						negativePointsInObject.Remove(point);

					if (pointsInFilledNegativeSpace.Any(p => (p.X == skinObjectBounds.Left) || (p.X == (skinObjectBounds.Right - 1)) || (p.Y == skinObjectBounds.Top) || (p.Y == (skinObjectBounds.Bottom - 1))))
						continue; // Ignore any negative regions that are not fully enclosed within the skin mask
					if (pointsInFilledNegativeSpace.Length <= scale)
						continue; // Ignore any very small regions (likely anomalies)
					boundsForSkinObjects.Add(skinObjectBounds); // Found a non-negligible fully-enclosed hole
					break;
				}
			}
			return boundsForSkinObjects;
		}

		// Based on code from https://simpledevcode.wordpress.com/2015/12/29/flood-fill-algorithm-using-c-net/
		private static IEnumerable<Point> TryToGetPointsInObject(DataRectangle<bool> mask, Point startAt, Rectangle limitTo)
		{
			if (mask == null)
				throw new ArgumentNullException(nameof(mask));
			if ((limitTo.Left < 0) || (limitTo.Right > mask.Width) || (limitTo.Top < 0) || (limitTo.Bottom > mask.Height))
				throw new ArgumentOutOfRangeException(nameof(limitTo));
			if ((startAt.X < limitTo.Left) || (startAt.X > limitTo.Right) || (startAt.Y < limitTo.Top) || (startAt.Y > limitTo.Bottom))
				throw new ArgumentOutOfRangeException(nameof(startAt));

			var valueAtOriginPoint = mask[startAt.X, startAt.Y];

			var pixels = new Stack<Point>();
			pixels.Push(startAt);

			var filledPixels = new HashSet<Point>();
			while (pixels.Count > 0)
			{
				var currentPoint = pixels.Pop();
				if ((currentPoint.X < limitTo.Left) || (currentPoint.X >= limitTo.Right) || (currentPoint.Y < limitTo.Top) || (currentPoint.Y >= limitTo.Bottom)) // make sure we stay within bounds
					continue;

				if ((mask[currentPoint.X, currentPoint.Y] == valueAtOriginPoint) && !filledPixels.Contains(currentPoint))
				{
					filledPixels.Add(new Point(currentPoint.X, currentPoint.Y));
					pixels.Push(new Point(currentPoint.X - 1, currentPoint.Y));
					pixels.Push(new Point(currentPoint.X + 1, currentPoint.Y));
					pixels.Push(new Point(currentPoint.X, currentPoint.Y - 1));
					pixels.Push(new Point(currentPoint.X, currentPoint.Y + 1));
				}
			}
			return filledPixels;
		}


		private static double RadianToDegree(double angle)
		{
			return angle * (180d / Math.PI);
		}

		private static DataRectangle<RGB> CorrectZeroResponse(DataRectangle<RGB> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			// Get the smallest value of any RGB component
			var smallestValue = values
				.Enumerate()
				.Select(point => point.Item2)
				.SelectMany(colour => new[] { colour.R, colour.G, colour.B })
				.Min();

			// Subtract this from every RGB component
			return values.Transform(value => new RGB((byte)(value.R - smallestValue), (byte)(value.G - smallestValue), (byte)(value.B - smallestValue)));
		}

		private static DataRectangle<RGB> GetResizedBitmapData(Bitmap source, double divideDimensionsBy)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (divideDimensionsBy <= 0)
				throw new ArgumentOutOfRangeException(nameof(divideDimensionsBy));

			var resizeTo = new Size((int)Math.Round(source.Width / divideDimensionsBy), (int)Math.Round(source.Height / divideDimensionsBy));
			using (var resizedSource = new Bitmap(source, resizeTo))
			{
				return resizedSource.GetRGB();
			}
		}

		private static Rectangle Scale(Rectangle region, double scale, Size limits)
		{
			if (scale <= 0)
				throw new ArgumentOutOfRangeException(nameof(scale));
			if ((limits.Width <= 0) || (limits.Height <= 0))
				throw new ArgumentOutOfRangeException(nameof(limits));

			// Need to ensure that we don't exceed the limits of the original image when scaling regions back up (there could be rounding errors that result in invalid
			// regions when scaling up that we need to be careful of)
			var left = (int)Math.Round(region.X * scale);
			var top = (int)Math.Round(region.Y * scale);
			var width = (int)Math.Round(region.Width * scale);
			var height = (int)Math.Round(region.Height * scale);
			return Rectangle.FromLTRB(
				left: left,
				top: top,
				right: Math.Min(left + width, limits.Width),
				bottom: Math.Min(top + height, limits.Height)
			);
		}
	}
}
