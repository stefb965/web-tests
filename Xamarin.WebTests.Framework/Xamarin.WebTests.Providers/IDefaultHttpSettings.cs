using System;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Providers
{
	public interface IDefaultHttpSettings : ITestDefaults
	{
		bool InstallDefaultCertificateValidator {
			get;
		}

		ISslStreamProvider DefaultSslStreamProvider {
			get;
		}
	}
}

