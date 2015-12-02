//
// ICertificateProvider.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.Providers
{
	using Portable;
	using ConnectionFramework;

	public delegate bool CertificateValidationDelegate (ICertificate certificate);

	public delegate IClientCertificate CertificateSelectionDelegate (
		string targetHost, ICertificate[] localCertificates, ICertificate remoteCertificate, string[] acceptableIssuers);

	public interface ICertificateProvider
	{
		CertificateValidator GetDefault ();

		CertificateValidator AcceptThisCertificate (IServerCertificate certificate);

		CertificateValidator AcceptFromCA (ICertificate certificate);

		CertificateValidator AcceptNull ();

		CertificateValidator RejectAll ();

		CertificateValidator AcceptAll ();

		void InstallDefaultValidator (CertificateValidator validator);

		ICertificate GetCertificate (X509Certificate certificate);

		ICertificate GetCertificateFromData (byte[] data);

		IServerCertificate GetServerCertificate (byte[] data, string password);

		IClientCertificate GetClientCertificate (byte[] data, string password);

		bool AreEqual (ICertificate a, ICertificate b);

		bool AreEqual (X509Certificate a, ICertificate b);

		CertificateValidator GetCustomCertificateValidator (RemoteCertificateValidationCallback callback);

		CertificateValidator GetCustomCertificateValidator (CertificateValidationDelegate func);

		CertificateSelector GetCustomCertificateSelector (LocalCertificateSelectionCallback callback);

		CertificateSelector GetCustomCertificateSelector (CertificateSelectionDelegate func);
	}
}

