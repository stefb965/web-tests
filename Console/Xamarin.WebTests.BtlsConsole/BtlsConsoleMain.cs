using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Mono.Btls.Tests;
using Xamarin.WebTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Mono.Btls.TestFramework;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (BoringTlsTestFeatures), true)]

namespace Xamarin.WebTests.BtlsConsole
{
	class BtlsConsoleMain
	{
		public static void Main (string[] args)
		{
			var setup = new BtlsConsoleFrameworkSetup ();
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<ITempDirectorySupport> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (BtlsConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);

			Program.Run (typeof (BtlsConsoleMain).Assembly, args);
		}
	}
}
