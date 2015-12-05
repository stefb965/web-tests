using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.AsyncTests.MacUI;
using Xamarin.WebTests.Resources;

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
			DependencyInjector.RegisterDependency<WebTestFeatures> (() => new WebTestFeatures ());

			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

