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
	public class ConsoleMain : WebDependencyProvider
	{
		static void Main (string[] args)
		{
			Program.Run (typeof (WebDependencyProvider).Assembly, args);
		}

		public override void Initialize ()
		{
			base.Initialize ();

			DependencyInjector.RegisterDependency<WebTestFeatures> (() => new WebTestFeatures ());

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

