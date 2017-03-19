using System;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;

namespace App.FaceClassifierDeserialisation
{
	public static class SvmSerialiser
	{
		private const double _doubleToIntMultiplier = 1000000000d;
		public static byte[] Serialise(SupportVectorMachine<Linear> svm)
		{
			if (svm == null)
				throw new ArgumentNullException(nameof(svm));

			var writer = new BinaryListWriter();
			writer.WriteInt(svm.NumberOfInputs);
			writer.WriteInt(svm.SupportVectors.Length);
			foreach (var supportVector in svm.SupportVectors)
			{
				writer.WriteInt(supportVector.Length);
				foreach (var value in supportVector)
					WriteDoubleWithMagnitudeNoLargerThanOne(writer, value);
			}
			writer.WriteInt(svm.Weights.Length);
			foreach (var weight in svm.Weights)
				WriteDoubleWithMagnitudeNoLargerThanOne(writer, weight);
			WriteDoubleWithMagnitudeNoLargerThanOne(writer, svm.Threshold);
			return writer.ToArray();
		}

		private static void WriteDoubleWithMagnitudeNoLargerThanOne(BinaryListWriter writer, double value)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (Math.Abs(value) > 1)
				throw new ArgumentOutOfRangeException(nameof(value));

			writer.WriteInt((int)(value * _doubleToIntMultiplier));
		}

		public static SupportVectorMachine<Linear> Deserialise(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var reader = new BinaryArrayReader(data);
			var numberOfInputs = reader.ReadInt();
			var numberOfSupportVectors = reader.ReadInt();
			var supportVectors = new double[numberOfSupportVectors][];
			for (var supportVectorIndex = 0; supportVectorIndex < numberOfSupportVectors; supportVectorIndex++)
			{
				var numberOfValues = reader.ReadInt();
				supportVectors[supportVectorIndex] = new double[numberOfValues];
				for (var valueIndex = 0; valueIndex < numberOfValues; valueIndex++)
					supportVectors[supportVectorIndex][valueIndex] = WriteDoubleWithMagnitudeNoLargerThanOne(reader);
			}
			var numberOfWeights = reader.ReadInt();
			var weights = new double[numberOfWeights];
			for (var weightIndex = 0; weightIndex < numberOfWeights; weightIndex++)
				weights[weightIndex] = WriteDoubleWithMagnitudeNoLargerThanOne(reader);
			var threshold = WriteDoubleWithMagnitudeNoLargerThanOne(reader);
			return new SupportVectorMachine<Linear>(numberOfInputs, new Linear())
			{
				SupportVectors = supportVectors,
				Weights = weights,
				Threshold = threshold
			};
		}

		private static double WriteDoubleWithMagnitudeNoLargerThanOne(BinaryArrayReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			return reader.ReadInt() / _doubleToIntMultiplier;
		}
	}
}
 