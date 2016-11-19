//
// CertificateDataWithKey.cs
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

	public class CertificateDataWithKey : CertificateData
	{
		CertificateDataFromPFX pfxData;
		CertificateDataFromPFX barePfxData;
		CertificateDataFromPFX fullPfxData;
		CertificateDataFromPEM pemData;

		public CertificateDataWithKey (
			string name, string password,
			CertificateResourceType type, CertificateResourceType? noKeyType = null,
			CertificateResourceType? bareType = null, CertificateResourceType? fullType = null)
			: base (name)
		{
			Type = type;
			NoKeyType = noKeyType;
			BareType = bareType;
			FullType = fullType;

			pfxData = new CertificateDataFromPFX (name, password, type);
			if (bareType != null)
				barePfxData = new CertificateDataFromPFX (name + "-bare", password, bareType.Value);
			if (fullType != null)
				fullPfxData = new CertificateDataFromPFX (name + "-full", password, fullType.Value);
			if (noKeyType != null)
				pemData = new CertificateDataFromPEM (name, noKeyType.Value);
		}

		public CertificateResourceType Type {
			get;
			private set;
		}

		public CertificateResourceType? NoKeyType {
			get;
			private set;
		}

		public CertificateResourceType? BareType {
			get;
			private set;
		}

		public CertificateResourceType? FullType {
			get;
			private set;
		}

		public override byte[] Data {
			get { return pemData.Data; }
		}

		public override X509Certificate Certificate {
			get { return pemData.Certificate; }
		}

		public override bool GetCertificateWithKey (CertificateResourceType type, out X509Certificate certificate)
		{
			if (type == Type) {
				certificate = pfxData.Certificate;
				return true;
			} else if (BareType != null && type == BareType.Value) {
				certificate = barePfxData.Certificate;
				return true;
			} else if (FullType != null && type == FullType.Value) {
				certificate = fullPfxData.Certificate;
				return true;
			} else {
				certificate = null;
				return false;
			}
		}

		public override bool GetCertificate (CertificateResourceType type, out X509Certificate certificate)
		{
			if (NoKeyType != null && type == NoKeyType.Value) {
				certificate = pemData.Certificate;
				return true;
			} else if (type == Type) {
				certificate = pfxData.Certificate;
				return true;
			} else if (BareType != null && type == BareType.Value) {
				certificate = barePfxData.Certificate;
				return true;
			} else if (FullType != null && type == FullType.Value) {
				certificate = fullPfxData.Certificate;
				return true;
			} else {
				certificate = null;
				return false;
			}
		}

		public override bool GetCertificateData (CertificateResourceType type, out byte[] data)
		{
			if (NoKeyType != null && type == NoKeyType.Value) {
				data = pemData.Data;
				return true;
			} else if (type == Type) {
				data = pfxData.Data;
				return true;
			} else if (BareType != null && type == BareType.Value) {
				data = barePfxData.Data;
				return true;
			} else if (FullType != null && type == FullType.Value) {
				data = fullPfxData.Data;
				return true;
			} else {
				data = null;
				return false;
			}
		}
	}
}
