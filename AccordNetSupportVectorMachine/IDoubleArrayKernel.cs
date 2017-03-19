namespace Accord.Statistics.Kernels
{
	// TODO: Temporarily using this instead of "IKernel<double[]>" due https://forums.bridge.net/forum/bridge-net-pro/bugs/3669
	public interface IDoubleArrayKernel
	{
		double Function(double[] x, double[] y);
	}
}