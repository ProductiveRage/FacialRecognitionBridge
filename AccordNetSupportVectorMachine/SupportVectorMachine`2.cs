// Accord Machine Learning Library
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

namespace Accord.MachineLearning.VectorMachines
{
	using Statistics;
	using Statistics.Kernels;

	/// <summary>
	///  Sparse Kernel Support Vector Machine (kSVM)
	/// </summary>
	/// 
	/// <remarks>
	/// <para>
	///   The original optimal hyperplane algorithm (SVM) proposed by Vladimir Vapnik in 1963 was a
	///   linear classifier. However, in 1992, Bernhard Boser, Isabelle Guyon and Vapnik suggested
	///   a way to create non-linear classifiers by applying the kernel trick (originally proposed
	///   by Aizerman et al.) to maximum-margin hyperplanes. The resulting algorithm is formally
	///   similar, except that every dot product is replaced by a non-linear kernel function.</para>
	/// <para>
	///   This allows the algorithm to fit the maximum-margin hyperplane in a transformed feature space.
	///   The transformation may be non-linear and the transformed space high dimensional; thus though
	///   the classifier is a hyperplane in the high-dimensional feature space, it may be non-linear in
	///   the original input space.</para> 
	///   
	/// <para>
	///   The machines are also able to learn sequence classification problems in which the input vectors
	///   can have arbitrary length. For an example on how to do that, please see the documentation page 
	///   for the <see cref="DynamicTimeWarping">DynamicTimeWarping kernel</see>.</para>
	///   
	/// <para>
	///   References:
	///   <list type="bullet">
	///     <item><description><a href="http://en.wikipedia.org/wiki/Support_vector_machine">
	///       http://en.wikipedia.org/wiki/Support_vector_machine </a></description></item>
	///     <item><description><a href="http://www.kernel-machines.org/">
	///       http://www.kernel-machines.org/ </a></description></item>
	///   </list></para>  
	/// </remarks>
	/// 
	/// <example>
	///   <para>
	///   The first example shows how to learn an SVM using a 
	///   standard kernel that operates on vectors of doubles.</para>
	///   <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs" region="doc_xor_normal" />
	///   
	///   <para>
	///   The second example shows how to learn an SVM using a 
	///   Sparse kernel that operates on sparse vectors.</para>
	///   <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\SequentialMinimalOptimizationTest.cs" region="doc_xor_sparse" />
	/// </example>
	/// 
	/// <seealso cref="Accord.Statistics.Kernels"/>
	/// <seealso cref="KernelSupportVectorMachine"/>
	/// <seealso cref="MulticlassSupportVectorMachine"/>
	/// <seealso cref="MultilabelSupportVectorMachine"/>
	/// 
	/// <seealso cref="Accord.MachineLearning.VectorMachines.Learning.SequentialMinimalOptimization"/>
	/// 
	public class SupportVectorMachine<TKernel, TInput> : BinaryLikelihoodClassifierBase<TInput>
		where TKernel : IDoubleArrayKernel // TODO: Temporarily using "IDoubleArrayKernel" instead of "IKernel<TInput>" because of https://forums.bridge.net/forum/bridge-net-pro/bugs/3669
	{
		private TKernel kernel;
		private TInput[] supportVectors;
		private double[] weights;
		private double threshold;

		/// <summary>
		///   Gets or sets the kernel used by this machine.
		/// </summary>
		/// 
		public TKernel Kernel
		{
			get { return kernel; }
			set { kernel = value; }
		}

		/// <summary>
		///   Gets whether this machine has been calibrated to
		///   produce probabilistic outputs (through the Probability(TInput)
		///   method).
		/// </summary>
		/// 
		public bool IsProbabilistic { get; set; }

		/// <summary>
		///   Gets or sets the collection of support vectors used by this machine.
		/// </summary>
		/// 
		public TInput[] SupportVectors
		{
			get { return supportVectors; }
			set { supportVectors = value; }
		}

		/// <summary>
		///   Gets or sets the collection of weights used by this machine.
		/// </summary>
		/// 
		public double[] Weights
		{
			get { return weights; }
			set { weights = value; }
		}

		/// <summary>
		///   Gets or sets the threshold (bias) term for this machine.
		/// </summary>
		/// 
		public double Threshold
		{
			get { return threshold; }
			set { threshold = value; }
		}


		/// <summary>
		///   Initializes a new instance of the <see cref="SupportVectorMachine{TKernel, TInput}"/> class.
		/// </summary>
		/// 
		/// <param name="inputs">The length of the input vectors expected by the machine.</param>
		/// <param name="kernel">The kernel function to be used.</param>
		/// 
		public SupportVectorMachine(int inputs, TKernel kernel)
		{
			this.NumberOfInputs = inputs;
			this.NumberOfOutputs = 2;
			this.Kernel = kernel;
		}


		/// <summary>
		/// Computes a class-label decision for a given <paramref name="input" />.
		/// </summary>
		/// <param name="input">The input vector that should be classified into
		/// one of the <see cref="Accord.MachineLearning.ITransform.NumberOfOutputs" /> possible classes.</param>
		/// <returns>
		/// A class-label that best described <paramref name="input" /> according
		/// to this classifier.
		/// </returns>
		public override bool Decide(TInput input)
		{
			double sum = threshold;
			for (int j = 0; j < supportVectors.Length; j++)
			{
				// TODO: Temporarily forcing values to double[] while implementing "IDoubleArrayKernel" instead of "IKernel<TInput>" (due to https://forums.bridge.net/forum/bridge-net-pro/bugs/3669)
				sum += weights[j] * kernel.Function(Bridge.Script.Write<double[]>("{0}", supportVectors[j]), Bridge.Script.Write<double[]>("{0}", input));
			}
			return Classes.Decide(sum);
		}
	}
}