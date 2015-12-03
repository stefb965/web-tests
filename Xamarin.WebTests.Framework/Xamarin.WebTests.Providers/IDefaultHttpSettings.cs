using System;

namespace Xamarin.WebTests.Providers
{
	public interface IDefaultHttpSettings
	{
		bool InstallDefaultCertificateValidator {
			get;
		}

		ISslStreamProvider DefaultSslStreamProvider {
			get;
		}
	}
}

