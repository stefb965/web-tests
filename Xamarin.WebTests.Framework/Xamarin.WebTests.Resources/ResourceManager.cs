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
		static readonly CertificateDataWithKey selfServerCert;

		static readonly CertificateDataFromPEM hamillerTubeCA;

		static readonly CertificateDataFromPEM tlsTestXamDevExpired;
		static readonly CertificateDataFromPEM tlsTestXamDevNew;
		static readonly CertificateDataFromPEM tlsTestXamDevCA;

		static readonly CertificateDataFromPEM intermediateCA;

		static readonly CertificateDataWithKey intermediateServer;

		static readonly HamillerTubeCAData hamillerTubeCAInfo;
		static readonly TlsTestXamDevNewData tlsTestXamDevNewInfo;
		static readonly TlsTestXamDevExpiredData tlsTestXamDevExpiredInfo;
		static readonly TlsTestXamDevCAData tlsTestXamDevCAInfo;
		static readonly SelfSignedServerData selfSignedServerInfo;
		static readonly IntermediateCAData intermediateCAInfo;
		static readonly IntermediateServerData intermediateServerInfo;

		static List<CertificateData> registeredCertificates;

		const string caCertHash = "AAAB625A1F5EA1DBDBB658FB360613BE49E67AEC";
		const string serverCertHash = "68295BFCB5B109738399DFFF86A5BEDE0694F334";
		const string serverSelfHash = "EC732FEEE493A91635E6BDC18377EEB3C11D6E16";

		static ResourceManager ()
		{
			registeredCertificates = new List<CertificateData> ();

			hamillerTubeCA = Register (new CertificateDataFromPEM ("Hamiller-Tube-CA", CertificateResourceType.HamillerTubeCA));
			Register (new CertificateDataFromPEM ("Hamiller-Tube-IM", CertificateResourceType.HamillerTubeIM));
			Register (new CertificateDataWithKey (
				"server-cert", "monkey", CertificateResourceType.ServerCertificate2FromLocalCA, CertificateResourceType.ServerCertificateFromLocalCA));
			selfServerCert = Register (new CertificateDataWithKey (
				"server-self", "monkey", CertificateResourceType.SelfSignedServerCertificate2, CertificateResourceType.SelfSignedServerCertificate));

			Register (new CertificateDataFromPFX ("invalid-server-cert", "monkey", CertificateResourceType.InvalidServerCertificateV1));
			Register (new CertificateDataFromPFX ("invalid-client-cert", "monkey", CertificateResourceType.InvalidClientCertificateV1));
			Register (new CertificateDataFromPFX ("invalid-client-ca-cert", "monkey", CertificateResourceType.InvalidClientCaCertificate));
			Register (new CertificateDataFromPFX ("client-cert-rsa512", "monkey", CertificateResourceType.InvalidClientCertificateRsa512));

			Register (new CertificateDataFromPFX ("monkey", "monkey", CertificateResourceType.MonkeyCertificate));
			Register (new CertificateDataFromPFX ("penguin", "penguin", CertificateResourceType.PenguinCertificate));
			Register (new CertificateDataFromPFX ("server-cert-rsaonly", "monkey", CertificateResourceType.ServerCertificateRsaOnly));
			Register (new CertificateDataFromPFX ("server-cert-dhonly", "monkey", CertificateResourceType.ServerCertificateDheOnly));
			Register (new CertificateDataFromPFX ("server-cert-rsa512", "monkey", CertificateResourceType.InvalidServerCertificateRsa512));
			Register (new CertificateDataFromPFX ("client-cert-rsaonly", "monkey", CertificateResourceType.ClientCertificateRsaOnly));
			Register (new CertificateDataFromPFX ("client-cert-dheonly", "monkey", CertificateResourceType.ClientCertificateDheOnly));

			tlsTestXamDevExpired = Register (new CertificateDataFromPEM ("tlstest-xamdev-expired", CertificateResourceType.TlsTestXamDevExpired));
			tlsTestXamDevNew = Register (new CertificateDataFromPEM ("tlstest-xamdev-new", CertificateResourceType.TlsTestXamDevNew));
			tlsTestXamDevCA = Register (new CertificateDataFromPEM ("tlstest-xamdev-ca", CertificateResourceType.TlsTestXamDevCA));

			intermediateCA = Register (new CertificateDataFromPEM ("intermediate-ca", CertificateResourceType.IntermediateCA));

			intermediateServer = Register (new CertificateDataWithKey (
				"intermediate-server", "monkey", CertificateResourceType.IntermediateServerWithKey, CertificateResourceType.IntermediateServer));

			hamillerTubeCAInfo = new HamillerTubeCAData (hamillerTubeCA);
			selfSignedServerInfo = new SelfSignedServerData (selfServerCert);
			tlsTestXamDevNewInfo = new TlsTestXamDevNewData (tlsTestXamDevNew);
			tlsTestXamDevExpiredInfo = new TlsTestXamDevExpiredData (tlsTestXamDevExpired);
			tlsTestXamDevCAInfo = new TlsTestXamDevCAData (tlsTestXamDevCA);
			intermediateCAInfo = new IntermediateCAData (intermediateCA);
			intermediateServerInfo = new IntermediateServerData (intermediateServer);

			Register (new CertificateDataFromPFX ("server-cert-with-ca", "monkey", CertificateResourceType.ServerCertificateWithCA));

			Register (new CertificateDataWithKey (
				"server-cert-im", "monkey", CertificateResourceType.IntermediateServerCertificate,
				CertificateResourceType.IntermediateServerCertificateNoKey, CertificateResourceType.IntermediateServerCertificateBare,
				CertificateResourceType.IntermediateServerCertificateFull));

			Register (new CertificateDataWithKey (
				"wildcard-server", "monkey", CertificateResourceType.WildcardServerCertificate,
				CertificateResourceType.WildcardServerCertificateNoKey, CertificateResourceType.WildcardServerCertificateBare,
				CertificateResourceType.WildcardServerCertificateFull));

			Register (new CertificateDataFromPEM ("trusted-im-ca", CertificateResourceType.TrustedIntermediateCA));

			Register (new CertificateDataWithKey (
				"server-cert-trusted-im", "monkey", CertificateResourceType.ServerFromTrustedIntermediataCA,
				CertificateResourceType.ServerFromTrustedIntermediataCANoKey,
				CertificateResourceType.IntermediateServerCertificateBare, null));

			Register (new CertificateDataFromPEM ("duplicate-hash.duplicate-hash-ca", CertificateResourceType.DuplicateHashCA));
			Register (new CertificateDataFromPEM ("duplicate-hash.duplicate-hash-invalid-ca", CertificateResourceType.DuplicateHashInvalidCA));
			Register (new CertificateDataWithKey (
				"duplicate-hash.duplicate-hash-server", "monkey", CertificateResourceType.DuplicateHashServer,
				CertificateResourceType.DuplicateHashServerNoKey, null, CertificateResourceType.DuplicateHashServerFull));
		}

		static T Register<T> (T data)
			where T : CertificateData
		{
			registeredCertificates.Add (data);
			return data;
		}

		public static X509Certificate GetCertificate (CertificateResourceType type)
		{
			foreach (var registered in registeredCertificates) {
				X509Certificate certificate;
				if (registered.GetCertificate (type, out certificate))
					return certificate;
			}

			throw new InvalidOperationException ();
		}

		public static byte[] GetCertificateData (CertificateResourceType type)
		{
			foreach (var registered in registeredCertificates) {
				byte[] data;
				if (registered.GetCertificateData (type, out data))
					return data;
			}

			throw new InvalidOperationException ();
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

