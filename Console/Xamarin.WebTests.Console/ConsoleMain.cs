using System;
using Xamarin.AsyncTests.Console;
using Xamarin.WebTests.TestProvider;

namespace Xamarin.WebTests.Console
{
	static class ConsoleMain
	{
		static void Main (string[] args)
		{
			Program.Run (typeof (WebDependencyProvider).Assembly, args);
		}
	}
}

