using System.Collections.Generic;

namespace System.Threading.Tasks
{
	public static class Parallel
	{
		public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> action)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			foreach (var item in source)
				action(item);
		}
	}
}
