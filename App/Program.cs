using System;
using System.Drawing;
using System.Linq;
using App.FaceClassifierDeserialisation;
using Bridge.Html5;
using Common;
using FaceDetection;

namespace App
{
	public static class Program
	{
		private static void Main()
		{
			var pleaseWaitMessage = Document.CreateElement("p");
			pleaseWaitMessage.TextContent = "Please wait..";
			Document.Body.AppendChild(pleaseWaitMessage);

			var startTime = DateTime.UtcNow;
			var faceDetector = new FaceDetector(
				logger: message =>
				{
					var timeSinceStart = DateTime.UtcNow.Subtract(startTime);
					Console.WriteLine($"[{(int)timeSinceStart.TotalMilliseconds}]ms: {message}");
				}
			);

			var faceClassifier = FaceClassifierLoader.Get();
			const int sampleWidth = 128;
			const int sampleHeight = 128;

			var img = Document.CreateElement<HTMLImageElement>("img");
			img.Src = "/TigerWoods.gif";
			img.AddEventListener("load", () => {
				Window.SetTimeout(() => { // Add a time out in case the image has been browser-cached (we need to give the browser a chance to show the please-wait before starting work)
					using (var bitmap = new Bitmap(img))
					{
						var possibleRegions = faceDetector.GetPossibleFaceRegions(bitmap);
						using (var g = Graphics.FromImage(bitmap))
						{
							foreach (var indexedPossibleRegion in possibleRegions.Select((r, i) => new { Index = i, Region = r }))
							{
								using (var possibleFaceSubImage = bitmap.ExtractImageSectionAndResize(indexedPossibleRegion.Region, new Size(sampleWidth, sampleHeight)))
								{
									using (var windowedImageForFeatureExtraction = possibleFaceSubImage.ExtractImageSectionAndResize(new Rectangle(new Point(0, 0), possibleFaceSubImage.Size), new Size(128, 128)))
									{
										if (faceClassifier.IsFace(possibleFaceSubImage))
											g.DrawRectangle(new Pen(Color.GreenYellow, 2), indexedPossibleRegion.Region.X, indexedPossibleRegion.Region.Y, indexedPossibleRegion.Region.Width, indexedPossibleRegion.Region.Height);
									}
								}
							}
						}
						bitmap.Save("IdentifiedFaces.png");
					}
					pleaseWaitMessage.Remove();
				});
			});
		}
	}
}
