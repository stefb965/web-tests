//
// ValidationTestRunner.cs
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

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	[ValidationTestRunner]
	public class ValidationTestRunner : ITestInstance, IDisposable
	{
		public ValidationTestParameters Parameters {
			get;
			private set;
		}

		public ValidationTestType Type {
			get { return Parameters.Type; }
		}

		public ValidationTestRunner (ValidationTestParameters parameters)
		{
			Parameters = parameters;
		}

		public static IEnumerable<ValidationTestType> GetTestTypes (TestContext ctx, ValidationTestCategory category)
		{
			switch (category) {
			case ValidationTestCategory.Default:
				yield return ValidationTestType.EmptyHost;
				yield return ValidationTestType.WrongHost;
				yield return ValidationTestType.Success;
				yield return ValidationTestType.RejectSelfSigned;
				yield return ValidationTestType.RejectHamillerTube;
				yield break;

			case ValidationTestCategory.MartinTest:
				yield return ValidationTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unspported validation category: '{0}.", category);
				yield break;
			}
		}

		public static IEnumerable<ValidationTestParameters> GetParameters (TestContext ctx, ValidationTestCategory category)
		{
			return GetTestTypes (ctx, category).Select (t => Create (ctx, category, t));
		}

		static ValidationTestParameters CreateParameters (ValidationTestCategory category, ValidationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new ValidationTestParameters (category, type, name);
		}

		static ValidationTestParameters Create (TestContext ctx, ValidationTestCategory category, ValidationTestType type)
		{
			var parameters = CreateParameters (category, type);

			switch (type) {
			case ValidationTestType.MartinTest:
				goto case ValidationTestType.Success;

			case ValidationTestType.EmptyHost:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.TlsTestXamDev);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = true;
				break;

			case ValidationTestType.WrongHost:
				parameters.Host = "invalid.xamdev-error.com";
				parameters.Add (CertificateResourceType.TlsTestXamDev);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = false;
				break;

			case ValidationTestType.Success:
				parameters.Host = "tlstest-1.xamdev.com";
				parameters.Add (CertificateResourceType.TlsTestXamDev);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = true;
				break;

			case ValidationTestType.RejectSelfSigned:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.SelfSignedServerCertificate);
				parameters.ExpectSuccess = false;
				break;

			case ValidationTestType.RejectHamillerTube:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.Add (CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = false;
				break;;

			default:
				ctx.AssertFail ("Unsupported validation type: '{0}'.", type);
				break;
			}

			return parameters;
		}

		public void Run (TestContext ctx)
		{
			ctx.LogMessage ("RUN: {0}", this);

			var validator = GetValidator ();
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certificates = GetCertificates ();

			var result = validator.ValidateCertificate (Parameters.Host, false, certificates);
			AssertResult (ctx, result);
		}

		ICertificateValidator GetValidator ()
		{
			return CertificateValidationHelper.GetValidator (null);
		}

		X509CertificateCollection GetCertificates ()
		{
			var certs = new X509CertificateCollection ();
			foreach (var type in Parameters.Types)
				certs.Add (new X509Certificate2 (ResourceManager.GetCertificateData (type)));
			return certs;
		}

		void AssertResult (TestContext ctx, ValidationResult result)
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
		}

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		#region ITestInstance implementation

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public virtual Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Dispose ();
			});
		}

		#endregion

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed;

		protected virtual void Dispose (bool disposing)
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[ValidationTestRunner: {0}]", Parameters.Identifier);
		}
	}
}

