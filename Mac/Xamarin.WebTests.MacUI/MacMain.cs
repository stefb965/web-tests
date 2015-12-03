using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.AsyncTests.MacUI;
using Xamarin.WebTests.Providers;
using Xamarin.WebTests.Resources;

[assembly: DependencyProvider (typeof (Xamarin.WebTests.MacUI.MacMain))]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.MacUI
{
	public class MacMain : WebDependencyProvider
	{
		static void Main (string[] args)
		{
			DependencyInjector.RegisterAssembly (typeof(MacMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);

			NSApplication.Init ();
			NSApplication.Main (args);
		}

		public override void Initialize ()
		{
			base.Initialize ();

			DependencyInjector.RegisterDependency<IBuiltinTestServer> (() => new BuiltinTestServer ());

			InstallDefaultCertificateValidator ();
		}

		void InstallDefaultCertificateValidator ()
		{
			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var defaultValidator = provider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			provider.InstallDefaultValidator (defaultValidator);
		}
	}
}

