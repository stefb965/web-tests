using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Resources
{
	using ConnectionFramework;

	public static class ResourceManager
	{
		static readonly ICertificateProvider provider;
		static readonly ICertificate cacert;
		static readonly ICertificate serverCertNoKey;
		static readonly ICertificate selfServerCertNoKey;
		static readonly ICertificate serverCert;
		static readonly ICertificate selfServerCert;
		static readonly ICertificate invalidServerCert;
		static readonly ICertificate invalidClientCert;
		static readonly ICertificate invalidClientCaCert;
		static readonly ICertificate invalidClientCertRsa512;
		static readonly ICertificate monkeyCert;
		static readonly ICertificate penguinCert;
		static readonly ICertificate serverCertRsaOnly;
		static readonly ICertificate serverCertDheOnly;
		static readonly ICertificate invalidServerCertRsa512;
		static readonly ICertificate clientCertRsaOnly;
		static readonly ICertificate clientCertDheOnly;

		const string caCertHash = "AAAB625A1F5EA1DBDBB658FB360613BE49E67AEC";
		const string serverCertHash = "68295BFCB5B109738399DFFF86A5BEDE0694F334";
		const string serverSelfHash = "EC732FEEE493A91635E6BDC18377EEB3C11D6E16";

		static ResourceManager ()
		{
			provider = DependencyInjector.Get<ICertificateProvider> ();
			cacert = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.Hamiller-Tube-CA.pem"));
			serverCertNoKey = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.server-cert.pem"));
			selfServerCertNoKey = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.server-self.pem"));
			selfServerCert = provider.GetCertificate (ReadResource ("CA.server-self.pfx"), "monkey");
			serverCert = provider.GetCertificate (ReadResource ("CA.server-cert.pfx"), "monkey");
			invalidServerCert = provider.GetCertificate (ReadResource ("CA.invalid-server-cert.pfx"), "monkey");
			invalidClientCert = provider.GetCertificate (ReadResource ("CA.invalid-client-cert.pfx"), "monkey");
			invalidClientCaCert = provider.GetCertificate (ReadResource ("CA.invalid-client-ca-cert.pfx"), "monkey");
			invalidClientCertRsa512 = provider.GetCertificate (ReadResource ("CA.client-cert-rsa512.pfx"), "monkey");
			monkeyCert = provider.GetCertificate (ReadResource ("CA.monkey.pfx"), "monkey");
			penguinCert = provider.GetCertificate (ReadResource ("CA.penguin.pfx"), "penguin");
			serverCertRsaOnly = provider.GetCertificate (ReadResource ("CA.server-cert-rsaonly.pfx"), "monkey");
			serverCertDheOnly = provider.GetCertificate (ReadResource ("CA.server-cert-dhonly.pfx"), "monkey");
			invalidServerCertRsa512 = provider.GetCertificate (ReadResource ("CA.server-cert-rsa512.pfx"), "monkey");
			clientCertRsaOnly = provider.GetCertificate (ReadResource ("CA.client-cert-rsaonly.pfx"), "monkey");
			clientCertDheOnly = provider.GetCertificate (ReadResource ("CA.client-cert-dheonly.pfx"), "monkey");
		}

		public static ICertificate LocalCACertificate {
			get { return cacert; }
		}

		public static ICertificate InvalidServerCertificateV1 {
			get { return invalidServerCert; }
		}

		public static X509Certificate SelfSignedServerCertificate {
			get { return selfServerCert.Certificate; }
		}

		public static X509Certificate ServerCertificateFromCA {
			get { return serverCert.Certificate; }
		}

		public static ICertificate InvalidClientCertificateV1 {
			get { return invalidClientCert; }
		}

		public static ICertificate InvalidClientCaCertificate {
			get { return invalidClientCaCert; }
		}

		public static ICertificate InvalidClientCertificateRsa512 {
			get { return invalidClientCertRsa512; }
		}

		public static ICertificate MonkeyCertificate {
			get { return monkeyCert; }
		}

		public static ICertificate PenguinCertificate {
			get { return penguinCert; }
		}

		public static ICertificate ServerCertificateRsaOnly {
			get { return serverCertRsaOnly; }
		}

		public static ICertificate ServerCertificateDheOnly {
			get { return serverCertDheOnly; }
		}

		public static ICertificate InvalidServerCertificateRsa512 {
			get { return invalidServerCertRsa512; }
		}

		public static ICertificate ClientCertificateRsaOnly {
			get { return clientCertRsaOnly; }
		}

		public static ICertificate ClientCertificateDheOnly {
			get { return clientCertDheOnly; }
		}

		public static ICertificate GetCertificateWithKey (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCert;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCert;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static ICertificate GetCertificate (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return cacert;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertNoKey;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCertNoKey;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static string GetCertificateHash (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return caCertHash;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertHash;
			case CertificateResourceType.SelfSignedServerCertificate:
				return serverSelfHash;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static bool TryLookupByHash (string hash, out CertificateResourceType type)
		{
			switch (hash.ToUpperInvariant ()) {
			case caCertHash:
				type = CertificateResourceType.HamillerTubeCA;
				return true;
			case serverCertHash:
				type = CertificateResourceType.ServerCertificateFromLocalCA;
				return true;
			case serverSelfHash:
				type = CertificateResourceType.SelfSignedServerCertificate;
				return true;
			default:
				type = CertificateResourceType.Invalid;
				return false;
			}
		}

		internal static byte[] ReadResource (string name)
		{
			var assembly = typeof(ResourceManager).GetTypeInfo ().Assembly;
			using (var stream = assembly.GetManifestResourceStream (assembly.GetName ().Name + "." + name)) {
				var data = new byte [stream.Length];
				var ret = stream.Read (data, 0, data.Length);
				if (ret != data.Length)
					throw new IOException ();
				return data;
			}
		}
	}
}

