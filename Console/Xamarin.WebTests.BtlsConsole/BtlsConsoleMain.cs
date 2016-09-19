using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Mono.Btls.Tests;
using Mono.Btls.TestProvider;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (BoringTlsTestFeatures), true)]

namespace Xamarin.WebTests.BtlsConsole
{
	class BtlsConsoleMain
	{
		public static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof (BtlsConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MonoTestFrameworkDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (BoringTlsDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<ITestFrameworkSetup> (() => new BtlsConsoleFrameworkSetup ());

			Program.Run (typeof (BtlsConsoleMain).Assembly, args);
		}
	}
}
