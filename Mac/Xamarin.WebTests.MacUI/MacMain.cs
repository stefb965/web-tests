using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;

namespace Xamarin.WebTests.MacUI
{
	static class MacMain
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);

			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

