using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.AsyncTests.MacUI;
using Xamarin.WebTests.Providers;
using Xamarin.WebTests.Resources;

[assembly: DependencyProvider (typeof (Xamarin.WebTests.TestProvider.WebDependencyProvider))]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.MacUI
{
	public class MacMain : ISingletonInstance
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(MacMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<IBuiltinTestServer> (() => new BuiltinTestServer ());

			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

