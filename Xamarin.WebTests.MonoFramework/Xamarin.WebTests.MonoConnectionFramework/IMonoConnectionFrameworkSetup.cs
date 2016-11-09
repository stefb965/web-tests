﻿//
// IMonoConnectionFrameworkSetup.cs
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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Mono.Security.Interface;
using Xamarin.WebTests.MonoConnectionFramework;

[assembly: RequireDependency (typeof (IMonoConnectionFrameworkSetup))]

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using ConnectionFramework;

	public interface IMonoConnectionFrameworkSetup : IConnectionFrameworkSetup
	{
		bool UsingBtls {
			get;
		}

		bool UsingAppleTls {
			get;
		}

		MonoTlsProvider GetDefaultProvider ();

		HttpWebRequest CreateHttpsRequest (Uri requestUri, MonoTlsProvider provider, MonoTlsSettings settings);

		HttpListener CreateHttpListener (X509Certificate certificate, MonoTlsProvider provider, MonoTlsSettings settings);

		ICertificateValidator GetCertificateValidator (MonoTlsSettings settings);

		IMonoConnectionInfo GetConnectionInfo (IMonoSslStream stream);
	}
}