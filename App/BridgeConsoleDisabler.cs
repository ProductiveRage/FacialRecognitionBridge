using Bridge;

namespace App
{
	public static class BridgeConsoleDisabler
	{
		[Init(InitPosition.Top)]
		private static void DisableConsole()
		{
			/*@
			Bridge.Console.log = function(message) { console.log(message); };
			Bridge.Console.error = function(message) { console.error(message); };
			Bridge.Console.debug = function(message) { console.debug(message); };
			*/
		}
	}
}
