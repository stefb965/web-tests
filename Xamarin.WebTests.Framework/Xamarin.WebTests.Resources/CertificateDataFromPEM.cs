//
// CertificateDataFromPEM.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc.
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

namespace Xamarin.WebTests.Resources
{
	using ConnectionFramework;

	public class CertificateDataFromPEM : CertificateData
	{
		byte[] data;
		X509Certificate certificate;

		public CertificateDataFromPEM (string name, CertificateResourceType type)
			: base (name)
		{
			Type = type;
			var provider = DependencyInjector.Get<ICertificateProvider> ();
			data = ResourceManager.ReadResource ("CA." + name + ".pem");
			certificate = provider.GetCertificateFromData (data);
		}

		public CertificateResourceType Type {
			get;
			private set;
		}

		public override byte[] Data {
			get { return data; }
		}

		public override X509Certificate Certificate {
			get { return certificate; }
		}

		public override bool GetCertificate (CertificateResourceType type, out X509Certificate certificate)
		{
			if (type == Type) {
				certificate = Certificate;
				return true;
			} else {
				certificate = null;
				return false;
			}
		}

		public override bool GetCertificateData (CertificateResourceType type, out byte[] data)
		{
			if (type == Type) {
				data = Data;
				return true;
			} else {
				data = null;
				return false;
			}
		}
	}
}
