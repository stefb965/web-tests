using System;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.Providers;
using Xamarin.WebTests.Resources;

[assembly: DependencyProvider (typeof (Xamarin.WebTests.Android.AndroidDependencyProvider))]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.Android
{
	public class AndroidDependencyProvider : WebDependencyProvider
	{
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

