//
// CertificateInfoTestRunner.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;

namespace Xamarin.WebTests.Framework
{
	public static class CertificateInfoTestRunner
	{
		public static void TestManagedCertificate (TestContext ctx, X509Certificate2 cert, CertificateInfo expected)
		{
			var subject = cert.SubjectName;
			if (ctx.Expect (subject, Is.Not.Null, "SubjectName")) {
				ctx.Expect (subject.Name, Is.EqualTo (expected.ManagedSubjectName), "SubjectName.Name");
			}

			var issuer = cert.IssuerName;
			if (ctx.Expect (issuer, Is.Not.Null, "IssuerName")) {
				ctx.Expect (issuer.Name, Is.EqualTo (expected.ManagedIssuerName), "IssuerName.Name");
			}

			ctx.LogMessage (cert.Subject);
			ctx.LogMessage (cert.Issuer);

			ctx.Expect (cert.Subject, Is.EqualTo (expected.ManagedSubjectName), "Subject");
			ctx.Expect (cert.Issuer, Is.EqualTo (expected.ManagedIssuerName), "Issue");
			ctx.Expect (cert.NotBefore.ToUniversalTime (), Is.EqualTo (expected.NotBefore), "NotBefore");
			ctx.Expect (cert.NotAfter.ToUniversalTime (), Is.EqualTo (expected.NotAfter), "NotAfter");
			ctx.Expect (cert.GetCertHash (), Is.EqualTo (expected.Hash), "GetCertHash()");

			ctx.Expect (cert.GetSerialNumber (), Is.EqualTo (expected.SerialNumberMono), "GetSerialNumber()");

			ctx.Expect (cert.Version, Is.EqualTo (expected.Version), "Version");

			ctx.Expect (cert.GetPublicKey (), Is.EqualTo (expected.PublicKeyData), "GetPublicKey()");

			var signatureAlgorithm = cert.SignatureAlgorithm;
			if (ctx.Expect (signatureAlgorithm, Is.Not.Null, "SignatureAlgorithm"))
				ctx.Expect (signatureAlgorithm.Value, Is.EqualTo (expected.SignatureAlgorithmOid), "SignatureAlgorithm.Value");

			var publicKey = cert.PublicKey;
			if (ctx.Expect (publicKey, Is.Not.Null, "PublicKey")) {
				if (ctx.Expect (publicKey.Oid, Is.Not.Null, "PublicKey.Oid"))
					ctx.Expect (publicKey.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.Oid.Value");

				var value = publicKey.EncodedKeyValue;
				if (ctx.Expect (value, Is.Not.Null, "PublicKey.EncodedKeyValue")) {
					if (ctx.Expect (value.Oid, Is.Not.Null, "PublicKey.Oid"))
						ctx.Expect (value.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.Oid.Value");

					ctx.Expect (value.RawData, Is.EqualTo (expected.PublicKeyData), "PublicKey.RawData");
				}

				var publicKeyParams = publicKey.EncodedParameters;
				if (ctx.Expect (publicKeyParams, Is.Not.Null, "PublicKey.EncodedParameters")) {
					if (ctx.Expect (publicKeyParams.Oid, Is.Not.Null, "PublicKey.EncodedParameters.Oid"))
						ctx.Expect (publicKeyParams.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.EncodedParameters.Oid.Value");
					ctx.Expect (publicKeyParams.RawData, Is.EqualTo (expected.PublicKeyParameters), "PublicKey.EncodedParameters.RawData");
				}
			}
		}
	}
}

