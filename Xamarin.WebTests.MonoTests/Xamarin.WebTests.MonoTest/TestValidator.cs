//
// TestValidator.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.MonoTestFeatures;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.Resources;

namespace Xamarin.WebTests.MonoTests
{
	[AsyncTestFixture]
	public class TestValidator
	{
		[Martin]
		[AsyncTest]
		[ValidationTestCategory (ValidationTestCategory.Default)]
		public void MartinTest (TestContext ctx,
		                       	ValidationTestParameters parameters,
		                        ValidationTestRunner runner)
		{
			runner.Run (ctx);
		}

		[AsyncTest]
		public void TestEmptyHost (TestContext ctx, CancellationToken cancellationToken)
		{
			var validator = CertificateValidationHelper.GetValidator (null);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certs = GetCertificates (CertificateResourceType.TlsTestXamDev, CertificateResourceType.TlsTestXamDevCA);

			var result = validator.ValidateCertificate (string.Empty, false, certs);
			AssertSuccess (ctx, result);
		}

		[AsyncTest]
		public void TestWrongHost (TestContext ctx, CancellationToken cancellationToken)
		{
			var validator = CertificateValidationHelper.GetValidator (null);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certs = GetCertificates (CertificateResourceType.TlsTestXamDev, CertificateResourceType.TlsTestXamDevCA);

			var result = validator.ValidateCertificate ("invalid.xamdev-error.com", false, certs);
			AssertError (ctx, result);
		}

		[AsyncTest]
		public void TestSuccess (TestContext ctx, CancellationToken cancellationToken)
		{
			var validator = CertificateValidationHelper.GetValidator (null);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certs = GetCertificates (CertificateResourceType.TlsTestXamDev, CertificateResourceType.TlsTestXamDevCA);

			var result = validator.ValidateCertificate ("tlstest-1.xamdev.com", false, certs);
			AssertSuccess (ctx, result);
		}

		[AsyncTest]
		public void TestSelfSigned (TestContext ctx, CancellationToken cancellationToken)
		{
			var validator = CertificateValidationHelper.GetValidator (null);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certs = GetCertificates (CertificateResourceType.SelfSignedServerCertificate);

			var result = validator.ValidateCertificate (string.Empty, false, certs);
			AssertError (ctx, result);
		}

		[AsyncTest]
		public void TestHamillerTube (TestContext ctx, CancellationToken cancellationToken)
		{
			var validator = CertificateValidationHelper.GetValidator (null);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certs = GetCertificates (CertificateResourceType.ServerCertificateFromLocalCA, CertificateResourceType.HamillerTubeCA);

			var result = validator.ValidateCertificate (string.Empty, false, certs);
			AssertError (ctx, result);
		}

		X509CertificateCollection GetCertificates (params CertificateResourceType[] types)
		{
			var certs = new X509CertificateCollection ();
			foreach (var type in types)
				certs.Add (new X509Certificate2 (ResourceManager.GetCertificateData (type)));
			return certs;
		}

		void AssertSuccess (TestContext ctx, ValidationResult result)
		{
			ctx.Assert (result, Is.Not.Null, "has result");
			ctx.Assert (result.Trusted, Is.True, "trusted");
			ctx.Assert (result.UserDenied, Is.False, "not user denied");
			ctx.Assert (result.ErrorCode, Is.EqualTo (0), "error code");
		}

		void AssertError (TestContext ctx, ValidationResult result, int? expectedError = null)
		{
			ctx.Assert (result, Is.Not.Null, "has result");
			ctx.Assert (result.Trusted, Is.False, "not trusted");
			ctx.Assert (result.UserDenied, Is.False, "not user denied");
			if (expectedError != null)
				ctx.Assert (result.ErrorCode, Is.EqualTo (expectedError.Value), "error code");
			else
				ctx.Assert (result.ErrorCode, Is.Not.EqualTo (0), "error code");
		}
	}
}

