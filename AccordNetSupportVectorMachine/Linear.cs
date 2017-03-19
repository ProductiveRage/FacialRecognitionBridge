// Accord Statistics Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Statistics.Kernels
{
	/// <summary>
	///   Linear Kernel.
	/// </summary>
	/// 
	public struct Linear : IDoubleArrayKernel // TODO: Temporarily using "IDoubleArrayKernel" instead of "IKernel" because of https://forums.bridge.net/forum/bridge-net-pro/bugs/3669
	{
		private double constant;

		/// <summary>
		///   Constructs a new Linear kernel.
		/// </summary>
		/// 
		/// <param name="constant">A constant intercept term. Default is 0.</param>
		/// 
		public Linear(double constant)
		{
			this.constant = constant;
		}

		/// <summary>
		///   Gets or sets the kernel's intercept term. Default is 0.
		/// </summary>
		/// 
		public double Constant
		{
			get { return constant; }
			set { constant = value; }
		}

		/// <summary>
		///   Linear kernel function.
		/// </summary>
		/// 
		/// <param name="x">Vector <c>x</c> in input space.</param>
		/// <param name="y">Vector <c>y</c> in input space.</param>
		/// 
		/// <returns>Dot product in feature (kernel) space.</returns>
		/// 
#if NET45 || NET46
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public double Function(double[] x, double[] y)
		{
			double sum = constant;
			for (int i = 0; i < y.Length; i++)
				sum += x[i] * y[i];

			return sum;
		}
	}
}