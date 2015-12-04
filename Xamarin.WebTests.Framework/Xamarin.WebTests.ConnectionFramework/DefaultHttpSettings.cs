using System;
using System.Net;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	public class DefaultHttpSettings : IDefaultHttpSettings
	{
		DotNetSslStreamProvider dotNetStreamProvider;

		public DefaultHttpSettings ()
		{
			dotNetStreamProvider = new DotNetSslStreamProvider ();
		}

		public bool InstallDefaultCertificateValidator {
			get { return true; }
		}

		public ISslStreamProvider DefaultSslStreamProvider {
			get { return dotNetStreamProvider; }
		}

		public SecurityProtocolType? SecurityProtocol {
			get { return null; }
		}

		public Guid? InstallTlsProvider {
			get { return null; }
		}
	}
}

