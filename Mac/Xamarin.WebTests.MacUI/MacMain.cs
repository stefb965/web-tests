using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.AsyncTests.MacUI;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.MonoTests;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.MonoTests.MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.MacUI
{
	public class MacMain : ISingletonInstance
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(MacMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<IBuiltinTestServer> (() => new BuiltinTestServer ());
			DependencyInjector.RegisterDependency<WebTestFeatures> (() => new WebTestFeatures ());
			DependencyInjector.RegisterDependency<MonoWebTestFeatures> (() => new MonoWebTestFeatures ());

			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

