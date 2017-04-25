﻿//
// ClientAndServerProvider.cs
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
//
using System;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ClientAndServerProvider : ITestParameter
	{
		public ConnectionProvider Client {
			get;
		}

		public ConnectionProvider Server {
			get;
		}

		public virtual bool IsManual {
			get { return Client.Type == ConnectionProviderType.Manual || Server.Type == ConnectionProviderType.Manual; }
		}

		public string Value {
			get;
		}

		string ITestParameter.FriendlyValue => Value;

		public virtual IClient CreateClient (ConnectionParameters parameters)
		{
			return Client.CreateClient (parameters);
		}

		public virtual IServer CreateServer (ConnectionParameters parameters)
		{
			return Server.CreateServer (parameters);
		}

		public ClientAndServerProvider (ConnectionProvider client, ConnectionProvider server, string value = null)
		{
			Client = client;
			Server = server;
			Value = value ?? string.Format ("{0}:{1}", client.Name, server.Name);
		}

		public override string ToString ()
		{
			return string.Format ("[ClientAndServerProvider {0}]", Value);
		}
	}
}

