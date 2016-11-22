//
// TrustedRootTestParameters.cs
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
using System.Collections.Generic;
using Xamarin.WebTests.MonoTestFramework;
using Mono.Btls.Interface;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Resources;

namespace Mono.Btls.TestFramework
{
	[TrustedRootTestParameters]
	public class TrustedRootTestParameters : ValidationTestParameters
	{
		List<Tuple<TrustedCertificateType,CertificateResourceType>> installCertificates;

		public TrustedRootTestType Type {
			get;
			private set;
		}

		public TrustedRootTestParameters (ValidationTestCategory category, TrustedRootTestType type, string identifier)
			: base (category, identifier)
		{
			Type = type;
		}

		new public bool ExpectSuccess {
			get; set;
		}

		public bool UseTestRunnerCallback {
			get; set;
		}

		public string CertificateSearchPaths {
			get; set;
		}

		public IReadOnlyCollection<Tuple<TrustedCertificateType,CertificateResourceType>> InstallCertificates {
			get {
				return installCertificates;
			}
		}

		public void Install (TrustedCertificateType type, CertificateResourceType resource)
		{
			if (installCertificates == null)
				installCertificates = new List<Tuple<TrustedCertificateType,CertificateResourceType>> ();
			installCertificates.Add (new Tuple<TrustedCertificateType,CertificateResourceType> (type, resource));
		}

		protected TrustedRootTestParameters (TrustedRootTestParameters other)
			: base (other)
		{
			Type = other.Type;
			ExpectSuccess = other.ExpectSuccess;
			UseTestRunnerCallback = other.UseTestRunnerCallback;
			CertificateSearchPaths = other.CertificateSearchPaths;
			if (other.InstallCertificates != null)
				installCertificates = new List<Tuple<TrustedCertificateType,CertificateResourceType>> (other.InstallCertificates);
		}

		public override ValidationParameters DeepClone ()
		{
			return new TrustedRootTestParameters (this);
		}
	}
}

