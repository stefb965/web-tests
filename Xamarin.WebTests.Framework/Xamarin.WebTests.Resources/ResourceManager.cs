using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Resources
{
	using ConnectionFramework;

	public static class ResourceManager
	{
		static readonly ICertificateProvider provider;

		static readonly CertificateData serverCert;
		static readonly CertificateData selfServerCert;

		static readonly CertificateData invalidServerCert;
		static readonly CertificateData invalidClientCert;
		static readonly CertificateData invalidClientCaCert;
		static readonly CertificateData invalidClientCertRsa512;

		static readonly CertificateData monkeyCert;
		static readonly CertificateData penguinCert;
		static readonly CertificateData serverCertRsaOnly;
		static readonly CertificateData serverCertDheOnly;
		static readonly CertificateData invalidServerCertRsa512;
		static readonly CertificateData clientCertRsaOnly;
		static readonly CertificateData clientCertDheOnly;

		static readonly CertificateDataFromPEM hamillerTubeCA;
		static readonly CertificateDataFromPEM serverSelf;

		static readonly CertificateDataFromPEM tlsTestXamDevExpired;
		static readonly CertificateDataFromPEM tlsTestXamDevNew;
		static readonly CertificateDataFromPEM tlsTestXamDevCA;

		static readonly CertificateDataFromPEM intermediateCA;

		static readonly CertificateDataFromPFXandPEM intermediateServer;

		static readonly HamillerTubeCAData hamillerTubeCAInfo;
		static readonly TlsTestXamDevNewData tlsTestXamDevNewInfo;
		static readonly TlsTestXamDevExpiredData tlsTestXamDevExpiredInfo;
		static readonly TlsTestXamDevCAData tlsTestXamDevCAInfo;
		static readonly SelfSignedServerData selfSignedServerInfo;
		static readonly IntermediateCAData intermediateCAInfo;
		static readonly IntermediateServerData intermediateServerInfo;

		static readonly CertificateDataFromPFX serverCertWithCA;

		static readonly CertificateDataFromPEM trustedIMCA;

		static readonly byte[] serverCertTrustedIMBareData;
		static readonly X509Certificate serverCertTrustedIMBare;
		static readonly byte[] serverCertTrustedIMData;
		static readonly X509Certificate serverCertTrustedIM;

		static List<CertificateData> registeredCertificates;

		const string caCertHash = "AAAB625A1F5EA1DBDBB658FB360613BE49E67AEC";
		const string serverCertHash = "68295BFCB5B109738399DFFF86A5BEDE0694F334";
		const string serverSelfHash = "EC732FEEE493A91635E6BDC18377EEB3C11D6E16";

		static ResourceManager ()
		{
			registeredCertificates = new List<CertificateData> ();

			provider = DependencyInjector.Get<ICertificateProvider> ();

			hamillerTubeCA = Register (new CertificateDataFromPEM ("Hamiller-Tube-CA", CertificateResourceType.HamillerTubeCA));
			Register (new CertificateDataFromPEM ("Hamiller-Tube-IM", CertificateResourceType.HamillerTubeIM));
			Register (new CertificateDataFromPEM ("server-cert", CertificateResourceType.ServerCertificateFromLocalCA));
			serverSelf = Register (new CertificateDataFromPEM ("server-self", CertificateResourceType.SelfSignedServerCertificate));

			serverCert = Register (new CertificateDataFromPFX ("server-cert", "monkey", CertificateResourceType.ServerCertificateFromLocalCA));
			selfServerCert = Register (new CertificateDataFromPFX ("server-self", "monkey", CertificateResourceType.SelfSignedServerCertificate));

			invalidServerCert = Register (new CertificateDataFromPFX ("invalid-server-cert", "monkey", CertificateResourceType.InvalidServerCertificateV1));
			invalidClientCert = Register (new CertificateDataFromPFX ("invalid-client-cert", "monkey", CertificateResourceType.InvalidClientCertificateV1));
			invalidClientCaCert = Register (new CertificateDataFromPFX ("invalid-client-ca-cert", "monkey", CertificateResourceType.InvalidClientCaCertificate));
			invalidClientCertRsa512 = Register (new CertificateDataFromPFX ("client-cert-rsa512", "monkey", CertificateResourceType.InvalidClientCertificateRsa512));

			monkeyCert = Register (new CertificateDataFromPFX ("monkey", "monkey", CertificateResourceType.MonkeyCertificate));
			penguinCert = Register (new CertificateDataFromPFX ("penguin", "penguin", CertificateResourceType.PenguinCertificate));
			serverCertRsaOnly = Register (new CertificateDataFromPFX ("server-cert-rsaonly", "monkey", CertificateResourceType.ServerCertificateRsaOnly));
			serverCertDheOnly = Register (new CertificateDataFromPFX ("server-cert-dhonly", "monkey", CertificateResourceType.ServerCertificateDheOnly));
			invalidServerCertRsa512 = Register (new CertificateDataFromPFX ("server-cert-rsa512", "monkey", CertificateResourceType.InvalidServerCertificateRsa512));
			clientCertRsaOnly = Register (new CertificateDataFromPFX ("client-cert-rsaonly", "monkey", CertificateResourceType.ClientCertificateRsaOnly));
			clientCertDheOnly = Register (new CertificateDataFromPFX ("client-cert-dheonly", "monkey", CertificateResourceType.ClientCertificateDheOnly));

			tlsTestXamDevExpired = Register (new CertificateDataFromPEM ("tlstest-xamdev-expired", CertificateResourceType.TlsTestXamDevExpired));
			tlsTestXamDevNew = Register (new CertificateDataFromPEM ("tlstest-xamdev-new", CertificateResourceType.TlsTestXamDevNew));
			tlsTestXamDevCA = Register (new CertificateDataFromPEM ("tlstest-xamdev-ca", CertificateResourceType.TlsTestXamDevCA));

			intermediateCA = Register (new CertificateDataFromPEM ("intermediate-ca", CertificateResourceType.IntermediateCA));

			intermediateServer = Register (new CertificateDataFromPFXandPEM ("intermediate-server", "monkey", CertificateResourceType.IntermediateServer));

			hamillerTubeCAInfo = new HamillerTubeCAData (hamillerTubeCA);
			selfSignedServerInfo = new SelfSignedServerData (serverSelf);
			tlsTestXamDevNewInfo = new TlsTestXamDevNewData (tlsTestXamDevNew);
			tlsTestXamDevExpiredInfo = new TlsTestXamDevExpiredData (tlsTestXamDevExpired);
			tlsTestXamDevCAInfo = new TlsTestXamDevCAData (tlsTestXamDevCA);
			intermediateCAInfo = new IntermediateCAData (intermediateCA);
			intermediateServerInfo = new IntermediateServerData (intermediateServer);

			serverCertWithCA = Register (new CertificateDataFromPFX ("server-cert-with-ca", "monkey", CertificateResourceType.ServerCertificateWithCA));

			Register (new CertificateDataWithIntermediate (
				"server-cert-im", "monkey", CertificateResourceType.IntermediateServerCertificate,
				CertificateResourceType.IntermediateServerCertificateBare, CertificateResourceType.IntermediateServerCertificateFull,
				CertificateResourceType.IntermediateServerCertificateNoKey));

			Register (new CertificateDataWithIntermediate (
				"wildcard-server", "monkey", CertificateResourceType.WildcardServerCertificate,
				CertificateResourceType.WildcardServerCertificateBare, CertificateResourceType.WildcardServerCertificateFull,
				CertificateResourceType.WildcardServerCertificateNoKey));

			trustedIMCA = Register (new CertificateDataFromPEM ("trusted-im-ca", CertificateResourceType.TrustedIntermediateCA));

			serverCertTrustedIMBareData = ResourceManager.ReadResource ("CA.server-cert-trusted-im-bare.pfx");
			serverCertTrustedIMBare = provider.GetCertificateWithKey (serverCertTrustedIMBareData, "monkey");
			serverCertTrustedIMData = ResourceManager.ReadResource ("CA.server-cert-trusted-im.pfx");
			serverCertTrustedIM = provider.GetCertificateWithKey (serverCertTrustedIMData, "monkey");
		}

		static T Register<T> (T data)
			where T : CertificateData
		{
			registeredCertificates.Add (data);
			return data;
		}

		public static X509Certificate LocalCACertificate {
			get { return hamillerTubeCA.Certificate; }
		}

		public static X509Certificate InvalidServerCertificateV1 {
			get { return invalidServerCert.Certificate; }
		}

		public static X509Certificate SelfSignedServerCertificate {
			get { return selfServerCert.Certificate; }
		}

		public static X509Certificate ServerCertificateFromCA {
			get { return serverCert.Certificate; }
		}

		public static X509Certificate InvalidClientCertificateV1 {
			get { return invalidClientCert.Certificate; }
		}

		public static X509Certificate InvalidClientCaCertificate {
			get { return invalidClientCaCert.Certificate; }
		}

		public static X509Certificate InvalidClientCertificateRsa512 {
			get { return invalidClientCertRsa512.Certificate; }
		}

		public static X509Certificate MonkeyCertificate {
			get { return monkeyCert.Certificate; }
		}

		public static X509Certificate PenguinCertificate {
			get { return penguinCert.Certificate; }
		}

		public static X509Certificate ServerCertificateRsaOnly {
			get { return serverCertRsaOnly.Certificate; }
		}

		public static X509Certificate ServerCertificateDheOnly {
			get { return serverCertDheOnly.Certificate; }
		}

		public static X509Certificate InvalidServerCertificateRsa512 {
			get { return invalidServerCertRsa512.Certificate; }
		}

		public static X509Certificate ClientCertificateRsaOnly {
			get { return clientCertRsaOnly.Certificate; }
		}

		public static X509Certificate ClientCertificateDheOnly {
			get { return clientCertDheOnly.Certificate; }
		}

		public static X509Certificate ServerCertificateWithCA {
			get { return serverCertWithCA.Certificate; }
		}

		public static X509Certificate GetCertificateWithKey (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCert.Certificate;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCert.Certificate;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static X509Certificate GetCertificate (CertificateResourceType type)
		{
			foreach (var registered in registeredCertificates) {
				X509Certificate certificate;
				if (registered.GetCertificate (type, out certificate))
					return certificate;
			}

			switch (type) {
			case CertificateResourceType.ServerFromTrustedIntermediataCA:
				return serverCertTrustedIM;
			case CertificateResourceType.ServerFromTrustedIntermediateCABare:
				return serverCertTrustedIMBare;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static byte[] GetCertificateData (CertificateResourceType type)
		{
			foreach (var registered in registeredCertificates) {
				byte[] data;
				if (registered.GetCertificateData (type, out data))
					return data;
			}

			switch (type) {
			case CertificateResourceType.ServerFromTrustedIntermediataCA:
				return serverCertTrustedIMData;
			case CertificateResourceType.ServerFromTrustedIntermediateCABare:
				return serverCertTrustedIMBareData;
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

		public static CertificateInfo GetCertificateInfo (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return hamillerTubeCAInfo;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfSignedServerInfo;
			case CertificateResourceType.TlsTestXamDevExpired:
				return tlsTestXamDevExpiredInfo;
			case CertificateResourceType.TlsTestXamDevNew:
				return tlsTestXamDevNewInfo;
			case CertificateResourceType.TlsTestXamDevCA:
				return tlsTestXamDevCAInfo;
			case CertificateResourceType.IntermediateCA:
				return intermediateCAInfo;
			case CertificateResourceType.IntermediateServer:
				return intermediateServerInfo;
			default:
				throw new InvalidOperationException ();
			}
		}

		[Obsolete]
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

