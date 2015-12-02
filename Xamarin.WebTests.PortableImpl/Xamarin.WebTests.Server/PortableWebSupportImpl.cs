//
// PortableSupport.cs
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
using System.Net;
using Http = System.Net.Http;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Server
{
	using HttpClient;
	using Portable;
	using Providers;
	using HttpFramework;
	using ConnectionFramework;
	using Resources;
	using Server;

	class PortableWebSupportImpl : IPortableWebSupport
	{
		#region Misc

		static PortableWebSupportImpl ()
		{
			try {
				address = LookupAddress ();
				hasNetwork = !IPAddress.IsLoopback (address);
			} catch {
				address = IPAddress.Loopback;
				hasNetwork = false;
			}
			defaultHttpProvider = new DefaultHttpProvider (null);
		}

		static readonly bool hasNetwork;
		static readonly IPAddress address;
		static readonly IHttpProvider defaultHttpProvider;

		#endregion

		#region Networking

		internal static IPAddress Address {
			get { return address; }
		}

		public bool HasNetwork {
			get { return hasNetwork; }
		}

		static IPAddress LookupAddress ()
		{
			try {
				#if __IOS__
				var interfaces = NetworkInterface.GetAllNetworkInterfaces ();
				foreach (var iface in interfaces) {
					if (iface.NetworkInterfaceType != NetworkInterfaceType.Ethernet && iface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
						continue;
					foreach (var address in iface.GetIPProperties ().UnicastAddresses) {
						if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address.Address))
							return address.Address;
					}
				}
				#else
				var hostname = Dns.GetHostName ();
				var hostent = Dns.GetHostEntry (hostname);
				foreach (var address in hostent.AddressList) {
					if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address))
						return address;
				}
				#endif
			} catch {
				;
			}

			return IPAddress.Loopback;
		}

		public IHttpProvider DefaultHttpProvider {
			get { return defaultHttpProvider; }
		}

		#endregion

		#region Listeners

		IServerCertificate IPortableWebSupport.GetDefaultServerCertificate ()
		{
			return ResourceManager.SelfSignedServerCertificate;
		}

		#endregion

		#region Certificate Validation

		public bool SupportsPerRequestCertificateValidator {
			get { return HttpWebRequestExtension.SupportsCertificateValidator; }
		}

		#endregion
	}
}

