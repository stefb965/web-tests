using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Mono.Btls.Tests;
using Mono.Btls.TestProvider;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (BoringTlsTestFeatures), true)]

namespace Xamarin.WebTests.BtlsConsole
{
	class ConsoleMain
	{
		public static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof (ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MonoTestFrameworkDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (BoringTlsDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<IMonoFrameworkSetup> (() => new ConsoleFrameworkSetup ());

			Program.Run (typeof (ConsoleMain).Assembly, args);
		}
	}
}
