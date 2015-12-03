using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.Providers;
using Xamarin.WebTests.Resources;

[assembly: DependencyProvider (typeof (Xamarin.WebTests.Console.ConsoleMain))]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.Console
{
	public class ConsoleMain
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
			Program.Run (typeof (WebTestFeatures).Assembly, args);
		}
	}
}

