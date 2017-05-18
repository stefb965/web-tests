﻿//
// MonoConnection.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using MSI = Mono.Security.Interface;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using TestFramework;

	public abstract class MonoConnection : DotNetConnection
	{
		public MonoConnection (MonoConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, parameters)
		{
			this.provider = provider;
		}

		MSI.MonoTlsSettings settings;
		MonoConnectionProvider provider;

		public MonoConnectionProvider ConnectionProvider {
			get { return provider; }
		}

		public bool SupportsConnectionInfo {
			get { return provider.SupportsMonoExtensions; }
		}

		public MSI.MonoTlsConnectionInfo GetConnectionInfo ()
		{
			var monoSslStream = MSI.MonoTlsProviderFactory.GetMonoSslStream (SslStream);
			return monoSslStream.GetConnectionInfo ();
		}

		protected abstract void GetSettings (TestContext ctx, MSI.MonoTlsSettings settings);

		public override string ToString ()
		{
			return string.Format ("[{0}: Provider={1}]", GetType ().Name, provider.Type);
		}
	}
}

