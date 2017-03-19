using System;

namespace App.FaceClassifierDeserialisation
{
	public sealed class BinaryArrayReader
	{
		private readonly byte[] _data;
		private int _position;
		public BinaryArrayReader(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			_data = data;
			_position = 0;
		}

		public int ReadInt()
		{
			var total = 0;
			var multiplier = 1;
			for (var i = 0; i < 4; i++)
			{
				total += _data[_position] * multiplier;
				_position++;
				multiplier = multiplier << 8;
			}
			return total;
		}
	}
}