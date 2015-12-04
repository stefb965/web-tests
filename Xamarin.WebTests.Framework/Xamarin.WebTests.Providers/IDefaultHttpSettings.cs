using System;
using System.Net;
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

		SecurityProtocolType? SecurityProtocol {
			get;
		}

		Guid? InstallTlsProvider {
			get;
		}
	}
}

