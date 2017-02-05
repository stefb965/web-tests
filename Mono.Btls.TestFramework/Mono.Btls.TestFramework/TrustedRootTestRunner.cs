//
// TrustedRootTestRunner.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.MonoTestFramework;
using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	[TrustedRootTestRunner]
	public class TrustedRootTestRunner : ValidationTestRunner
	{
		new public TrustedRootTestParameters Parameters {
			get {
				return (TrustedRootTestParameters)base.Parameters;
			}
		}

		public TrustedRootTestType Type {
			get { return Parameters.Type; }
		}

		public TrustedRootTestRunner (TrustedRootTestParameters parameters)
			: base (parameters)
		{
		}

		public static IEnumerable<TrustedRootTestType> GetTestTypes (TestContext ctx, ValidationTestCategory category)
		{
			switch (category) {
			case ValidationTestCategory.TrustedRoots:
				yield return TrustedRootTestType.TrustedLocalCA;
				yield return TrustedRootTestType.OldHash;
				yield return TrustedRootTestType.LeadingZero;
				yield return TrustedRootTestType.OldHashLeadingZero;
				yield return TrustedRootTestType.IntermediateCA;
				yield return TrustedRootTestType.OnlyIntermediateCA;
				yield return TrustedRootTestType.MissingIntermediateCA;
				yield return TrustedRootTestType.SelfSignedCertificate;
				yield return TrustedRootTestType.LeafCertificate;
				yield return TrustedRootTestType.DuplicateHashSimple;
				yield return TrustedRootTestType.DuplicateHashInvalidCA;
				yield return TrustedRootTestType.DuplicateHashCorrectOrder;
				yield return TrustedRootTestType.DuplicateHashWrongOrder;
				yield return TrustedRootTestType.DuplicateHashOldAndNew;
				yield return TrustedRootTestType.DuplicateHashNewAndOld;
				yield break;

			case ValidationTestCategory.MartinTest:
				yield return TrustedRootTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unspported validation category: '{0}.", category);
				yield break;
			}
		}

		public static IEnumerable<TrustedRootTestParameters> GetParameters (TestContext ctx, ValidationTestCategory category)
		{
			return GetTestTypes (ctx, category).Select (t => Create (ctx, category, t));
		}

		static TrustedRootTestParameters CreateParameters (ValidationTestCategory category, TrustedRootTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new TrustedRootTestParameters (category, type, name);
		}

		static TrustedRootTestParameters Create (TestContext ctx, ValidationTestCategory category, TrustedRootTestType type)
		{
			var parameters = CreateParameters (category, type);

			switch (type) {
			case TrustedRootTestType.TrustedLocalCA:
				parameters.Host = "Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.OldHash:
				parameters.Host = "Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.Install (TrustedCertificateType.TrustedDirectoryOldHash, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.LeadingZero:
				parameters.Host = "Trusted-IM-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.ServerFromTrustedIntermediataCANoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.TrustedIntermediateCA);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.OldHashLeadingZero:
				parameters.Host = "Trusted-IM-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.ServerFromTrustedIntermediataCANoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectoryOldHash, CertificateResourceType.TrustedIntermediateCA);
				parameters.Install (TrustedCertificateType.TrustedDirectoryOldHash, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.IntermediateCA:
				parameters.Host = "Intermediate-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeIM);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.OnlyIntermediateCA:
				parameters.Host = "Intermediate-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeIM);
				parameters.ExpectSuccess = false;
				break;

			case TrustedRootTestType.MissingIntermediateCA:
				parameters.Host = "Intermediate-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = false;
				break;

			case TrustedRootTestType.LeafCertificate:
				parameters.Host = "Intermediate-Server.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ExpectSuccess = false;
				break;

			case TrustedRootTestType.SelfSignedCertificate:
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.SelfSignedServerCertificate);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.SelfSignedServerCertificate);
				parameters.ExpectSuccess = true;
				break;
			
			case TrustedRootTestType.DuplicateHashSimple:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.DuplicateHashInvalidCA:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashInvalidCA);
				parameters.ExpectSuccess = false;
				break;

			case TrustedRootTestType.DuplicateHashCorrectOrder:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashCA);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashInvalidCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.DuplicateHashWrongOrder:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashInvalidCA);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.DuplicateHashOldAndNew:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectoryOldHash, CertificateResourceType.DuplicateHashInvalidCA);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashCA);
				parameters.ExpectSuccess = true;
				break;

			case TrustedRootTestType.DuplicateHashNewAndOld:
				parameters.Host = "public.Hamiller-Tube.local";
				parameters.CertificateSearchPaths = "@test";
				parameters.Add (CertificateResourceType.DuplicateHashServerNoKey);
				parameters.Install (TrustedCertificateType.TrustedDirectory, CertificateResourceType.DuplicateHashInvalidCA);
				parameters.Install (TrustedCertificateType.TrustedDirectoryOldHash, CertificateResourceType.DuplicateHashCA);
				parameters.ExpectSuccess = false;
				break;

			case TrustedRootTestType.MartinTest:
				goto case TrustedRootTestType.DuplicateHashNewAndOld;

			default:
				ctx.AssertFail ("Unsupported validation type: '{0}'.", type);
				break;
			}

			return parameters;
		}

		protected override void PreRun (TestContext ctx)
		{
			base.PreRun (ctx);

			if (Parameters.InstallCertificates != null) {
				var support = DependencyInjector.Get<ITempDirectorySupport> ();
				trustedDirectory = support.CreateTempDirectory ();
				ctx.LogMessage ("TRUSTED DIR: {0}", trustedDirectory);
				installedCertificates = new List<string> ();

				foreach (var type in Parameters.InstallCertificates) {
					var data = ResourceManager.GetCertificateData (type.Item2);
					using (var x509 = BtlsProvider.CreateNative (data, BtlsX509Format.PEM)) {
						InstallCertificate (ctx, trustedDirectory, type.Item1, x509);
					}
				}
			}
		}

		protected override void PostRun (TestContext ctx)
		{
			if (trustedDirectory != null) {
				var support = DependencyInjector.Get<ITempDirectorySupport> ();
				foreach (var installed in installedCertificates)
					support.DeleteFile (installed);
				support.DeleteTempDirectory (trustedDirectory);
			}

			base.PostRun (ctx);
		}

		void InstallCertificate (TestContext ctx, string path, TrustedCertificateType type, BtlsX509 x509)
		{
			switch (type) {
			case TrustedCertificateType.TrustedDirectory:
				InstallCertificate (ctx, path, false, x509);
				break;
			case TrustedCertificateType.TrustedDirectoryOldHash:
				InstallCertificate (ctx, path, true, x509);
				break;
			default:
				ctx.AssertFail ("Invalid type: `{0}'.", type);
				break;
			}
		}

		void InstallCertificate (TestContext ctx, string path, bool old, BtlsX509 x509)
		{
			var support = DependencyInjector.Get<ITempDirectorySupport> ();

			long hash;
			using (var name = x509.GetSubjectName ())
				hash = old ? name.GetHashOld () : name.GetHash ();

			string fileName;
			int num = 0;
			do {
				fileName = Path.Combine (path, string.Format ("{0:x8}.{1}", hash, num++));
				ctx.LogMessage ("TEST: {0}", fileName);
			} while (support.FileExists (fileName));

			ctx.LogMessage ("DONE: {0}", fileName);

			using (var stream = support.CreateFile (fileName)) {
				x509.ExportAsPEM (stream, true);
			}

			installedCertificates.Add (fileName);
		}

		string trustedDirectory;
		List<string> installedCertificates;
		int validatorInvoked;

		bool ValidationCallback (TestContext ctx, string targetHost, X509Certificate certificate, X509Chain chain, MonoSslPolicyErrors sslPolicyErrors)
		{
			// `targetHost` is only non-null if we're called from `HttpWebRequest`.
			ctx.Assert (targetHost, Is.Null, "target host");
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			if (Parameters.ExpectSuccess)
				ctx.Assert (sslPolicyErrors, Is.EqualTo (MonoSslPolicyErrors.None), "errors");
			else
				ctx.Assert (sslPolicyErrors, Is.Not.EqualTo (MonoSslPolicyErrors.None), "expect error");
			ctx.Assert (chain, Is.Not.Null, "chain");
			++validatorInvoked;

			if (Parameters.ExpectedChain != null) {
				var extraStore = chain.ChainPolicy.ExtraStore;
				ctx.Assert (extraStore, Is.Not.Null, "ChainPolicy.ExtraStore");
				ctx.Assert (extraStore.Count, Is.EqualTo (Parameters.ExpectedChain.Count), "ChainPolicy.ExtraStore.Count");
				var extraStoreCert = extraStore[0];
				ctx.Assert (extraStoreCert, Is.Not.Null, "ChainPolicy.ExtraStore[0]");
				ctx.Assert (extraStoreCert, Is.InstanceOfType (typeof (X509Certificate2)), "ChainPolicy.ExtraStore[0].GetType()");
			}

			return true;
		}

		public override void Run (TestContext ctx)
		{
			ctx.LogMessage ("TRUSTED ROOT TEST RUNNER: {0}", this);

			var setup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
			if (Parameters.CertificateSearchPaths != null && !setup.SupportsCertificateSearchPaths) {
				ctx.IgnoreThisTest ();
				return;
			}

			var validator = GetValidator (ctx);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certificates = GetCertificates ();

			validatorInvoked = 0;

			var result = validator.ValidateCertificate (Parameters.Host, false, certificates);
			AssertResult (ctx, result);
		}

		ICertificateValidator GetValidator (TestContext ctx)
		{
			var setup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
			var settings = MonoTlsSettings.CopyDefaultSettings ();

			if (Parameters.UseTestRunnerCallback) {
				settings.CallbackNeedsCertificateChain = true;
				settings.UseServicePointManagerCallback = false;
				settings.RemoteCertificateValidationCallback = (t, c, ch, e) => ValidationCallback (ctx, t, c, ch, e);
			}

			if (Parameters.CertificateSearchPaths != null) {
				ctx.Assert (setup.SupportsCertificateSearchPaths, "IMonoConnectionFrameworkSetup.SupportsCertificateSearchPaths");
				var searchPaths = new List<string> ();
				var paths = Parameters.CertificateSearchPaths.Split (':');
				foreach (var path in paths) {
					switch (path) {
					case "@default":
					case "@user":
					case "@machine":
					case "@trusted":
						searchPaths.Add (path);
						break;
					case "@test":
						searchPaths.Add ("@pem:" + trustedDirectory);
						break;
					default:
						ctx.AssertFail ("Invalid certificate search path: `{0}'.", path);
						break;
					}
				}
				setup.SetCertificateSearchPaths (settings, searchPaths.ToArray ());
			}

			return setup.GetCertificateValidator (settings);
		}

		protected void AssertResult (TestContext ctx, ValidationResult result)
		{
			if (Parameters.ExpectSuccess) {
				ctx.Assert (result, Is.Not.Null, "has result");
				ctx.Assert (result.Trusted, Is.True, "trusted");
				ctx.Assert (result.UserDenied, Is.False, "not user denied");
				ctx.Assert (result.ErrorCode, Is.EqualTo (0), "error code");
			} else {
				ctx.Assert (result, Is.Not.Null, "has result");
				ctx.Assert (result.Trusted, Is.False, "not trusted");
				ctx.Assert (result.UserDenied, Is.False, "not user denied");
				if (Parameters.ExpectError != null)
					ctx.Assert (result.ErrorCode, Is.EqualTo (Parameters.ExpectError.Value), "error code");
				else
					ctx.Assert (result.ErrorCode, Is.Not.EqualTo (0), "error code");
			}

			if (Parameters.UseTestRunnerCallback)
				ctx.Assert (validatorInvoked, Is.EqualTo (1), "validator invoked");
		}
	}
}
