//
// CertificateDataWithIntermediate.cs
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

	public class CertificateDataWithIntermediate : CertificateDataWithKey
	{
		CertificateDataFromPFX pfxData;
		CertificateDataFromPFX barePfxData;
		CertificateDataFromPFX fullPfxData;
		CertificateDataFromPEM pemData;

		public CertificateDataWithIntermediate (
			string name, string password,
			CertificateResourceType type, CertificateResourceType bareType,
			CertificateResourceType fullType, CertificateResourceType noKeyType)
			: base (name)
		{
			Type = type;
			BareType = bareType;
			FullType = fullType;
			NoKeyType = noKeyType;

			pfxData = new CertificateDataFromPFX (name, password, type);
			barePfxData = new CertificateDataFromPFX (name + "-bare", password, bareType);
			fullPfxData = new CertificateDataFromPFX (name + "-full", password, fullType);
			pemData = new CertificateDataFromPEM (name, noKeyType);
		}

		public CertificateResourceType Type {
			get;
			private set;
		}

		public CertificateResourceType BareType {
			get;
			private set;
		}

		public CertificateResourceType FullType {
			get;
			private set;
		}

		public CertificateResourceType NoKeyType {
			get;
			private set;
		}

		public override byte[] Data {
			get { return pemData.Data; }
		}

		public override X509Certificate Certificate {
			get { return pemData.Certificate; }
		}

		public override CertificateData PublicCertificate {
			get { return pemData; }
		}

		public override bool GetCertificate (CertificateResourceType type, out X509Certificate certificate)
		{
			if (type == Type) {
				certificate = pfxData.Certificate;
				return true;
			} else if (type == BareType) {
				certificate = barePfxData.Certificate;
				return true;
			} else if (type == FullType) {
				certificate = fullPfxData.Certificate;
				return true;
			} else if (type == NoKeyType) {
				certificate = pemData.Certificate;
				return true;
			} else {
				certificate = null;
				return false;
			}
		}

		public override bool GetCertificateData (CertificateResourceType type, out byte[] data)
		{
			if (type == Type) {
				data = pfxData.Data;
				return true;
			} else if (type == BareType) {
				data = barePfxData.Data;
				return true;
			} else if (type == FullType) {
				data = fullPfxData.Data;
				return true;
			} else if (type == NoKeyType) {
				data = pemData.Data;
				return true;
			} else {
				data = null;
				return false;
			}
		}
	}
}
