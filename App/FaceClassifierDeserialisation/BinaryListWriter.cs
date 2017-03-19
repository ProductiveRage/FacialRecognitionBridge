using System.Collections.Generic;

namespace App.FaceClassifierDeserialisation
{
	public sealed class BinaryListWriter
	{
		private readonly List<byte> _data;
		public BinaryListWriter()
		{
			_data = new List<byte>();
		}

		public void WriteInt(int value)
		{
			// This will store the data with least significant byte, second least significant byte, third most significant byte, most significant byte
			for (var i = 0; i < 4; i++)
			{
				_data.Add((byte)(value & 255));
				value = value >> 8;
			}
		}

		public byte[] ToArray()
		{
			return _data.ToArray();
		}
	}
}