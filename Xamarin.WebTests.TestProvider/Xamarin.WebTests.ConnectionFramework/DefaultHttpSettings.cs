using System;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	class DefaultHttpSettings : IDefaultHttpSettings
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
	}
}

