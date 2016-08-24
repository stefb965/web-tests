//
// BoringTlsDependencyProvider.cs
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
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Mono.Security.Interface;
using Mono.Btls.TestFramework;
using Mono.Btls.TestProvider;
using Mono.Btls.Interface;
using Mono.Btls;
using System.Net;

[assembly: DependencyProvider (typeof (BoringTlsDependencyProvider))]

namespace Mono.Btls.TestProvider
{
	public class BoringTlsDependencyProvider : IDefaultConnectionSettings, IDependencyProvider
	{
		const ConnectionProviderFlags DefaultFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
		const ConnectionProviderFlags BoringTlsFlags = DefaultFlags | ConnectionProviderFlags.SupportsTls12 |
			ConnectionProviderFlags.SupportsAeadCiphers | ConnectionProviderFlags.SupportsEcDheCiphers |
			ConnectionProviderFlags.SupportsClientCertificates | ConnectionProviderFlags.SupportsTrustedRoots;
		
		public void Initialize ()
		{
			var factory = DependencyInjector.Get<MonoConnectionProviderFactory> ();

			var boringTls = BtlsProvider.GetProvider ();
			factory.RegisterProvider ("BoringTLS", boringTls, ConnectionProviderType.BoringTLS, BoringTlsFlags);

			DependencyInjector.RegisterDefaults<IDefaultConnectionSettings> (3, () => this);
		}

		public bool InstallDefaultCertificateValidator {
			get { return true; }
		}

		public ISslStreamProvider DefaultSslStreamProvider {
			get { return null; }
		}

		public SecurityProtocolType? SecurityProtocol {
			get { return (SecurityProtocolType)0xfc0; }
		}

		public Guid? InstallTlsProvider {
			get {
#if BTLS
				return MonoConnectionProviderFactory.BoringTlsGuid;
#else
				return null;
#endif
			}
		}
	}
}

