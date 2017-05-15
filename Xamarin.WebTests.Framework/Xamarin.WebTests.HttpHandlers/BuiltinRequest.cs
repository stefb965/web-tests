//
// BuiltinRequest.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpHandlers
{
	public sealed class BuiltinRequest : Request
	{
		public HttpServer Server {
			get;
		}

		public Uri Uri {
			get;
		}

		public override string Method {
			get; set;
		}

		NameValueCollection headers;

		public BuiltinRequest (HttpServer server, Uri uri, string method)
		{
			Server = server;
			Uri = uri;
			Method = method;

			headers = new NameValueCollection ();
		}

		public override async Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			var client = new BuiltinClient (ctx, Server, Uri);
			var connection = await client.ConnectAsync (cancellationToken).ConfigureAwait (false);
			ctx.LogMessage ("CONNECTED: {0}", connection);

			try {
				return await SendRequest (ctx, connection, cancellationToken);
			} catch (Exception ex) {
				ctx.LogMessage ("FAILED TO SEND REQUEST: {0}", ex);
				return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
			} finally {
				connection.Dispose ();
			}
		}

		async Task<Response> SendRequest (TestContext ctx, HttpConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			await connection.Initialize (ctx, cancellationToken);

			var message = new HttpRequest (HttpProtocol.Http11, Method, Uri.AbsolutePath, headers);
			await connection.WriteRequest (ctx, message, cancellationToken);

			ctx.LogMessage ("DONE WRITING REQUEST");

			var response = await connection.ReadResponse (ctx, cancellationToken);
			ctx.LogMessage ("GOT RESPONSE: {0}", response);

			return new SimpleResponse (this, response.StatusCode, response.Body);
		}

		public void AddHeader (string name, string value)
		{
			headers.Add (name, value);
		}

		public override void SendChunked ()
		{
			throw new NotSupportedException ();
		}

		public override void SetContentLength (long contentLength)
		{
			throw new NotImplementedException ();
		}

		public override void SetContentType (string contentType)
		{
			throw new NotImplementedException ();
		}

		public override void SetCredentials (ICredentials credentials)
		{
			throw new NotSupportedException ();
		}

		public override void SetProxy (IWebProxy proxy)
		{
			throw new NotSupportedException ();
		}
	}
}
