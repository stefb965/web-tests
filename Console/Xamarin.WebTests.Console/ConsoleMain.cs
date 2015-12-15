using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.MonoTests;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.MonoTests.MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.Console
{
	public class ConsoleMain
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
			Program.Run (typeof (ConsoleMain).Assembly, args);
		}
	}
}

