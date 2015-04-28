﻿//
// DefaultConnectionProviderFactory.cs
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
using System.Net;
using System.Security.Authentication;

namespace Xamarin.WebTests.ConnectionFramework
{
	class DefaultConnectionProviderFactory : IConnectionProviderFactory
	{
		readonly IConnectionProvider dotNetProvider;

		public bool IsSupported (ConnectionProviderType type)
		{
			return type == ConnectionProviderType.DotNet;
		}

		public IConnectionProvider GetProvider (ConnectionProviderType type)
		{
			if (type == ConnectionProviderType.DotNet)
				return dotNetProvider;
			throw new InvalidOperationException ();
		}

		internal DefaultConnectionProviderFactory ()
		{
			dotNetProvider = new DotNetProvider ();
		}

		class DotNetProvider : IConnectionProvider
		{
			public IClient CreateClient (IClientParameters parameters)
			{
				return new DotNetClient (GetEndPoint (parameters), SslProtocols, parameters);
			}

			public IServer CreateServer (IServerParameters parameters)
			{
				return new DotNetServer (GetEndPoint (parameters), SslProtocols, parameters);
			}
		}

		static SslProtocols SslProtocols {
			get { return SslProtocols.Default; }
		}

		static IPEndPoint GetEndPoint (ICommonConnectionParameters parameters)
		{
			if (parameters.EndPoint != null)
				return new IPEndPoint (IPAddress.Parse (parameters.EndPoint.Address), parameters.EndPoint.Port);
			else
				return new IPEndPoint (IPAddress.Loopback, 4433);
		}
	}
}

