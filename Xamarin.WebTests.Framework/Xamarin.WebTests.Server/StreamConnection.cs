﻿﻿﻿//
// StreamConnection.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server
{
	class StreamConnection : HttpConnection
	{
		public Stream Stream {
			get;
			private set;
		}

		HttpStreamReader reader;
		StreamWriter writer;

		public StreamConnection (TestContext ctx, HttpServer server, Stream stream, ISslStream sslStream)
			: base (ctx, server, sslStream)
		{
			Stream = stream;

			reader = new HttpStreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
		}

		public override async Task<bool> HasRequest (CancellationToken cancellationToken)
		{
			return !await reader.IsEndOfStream (cancellationToken).ConfigureAwait (false);
		}

		public override Task<HttpRequest> ReadRequest (CancellationToken cancellationToken)
		{
			return HttpRequest.Read (reader, cancellationToken);
		}

		public override Task<HttpResponse> ReadResponse (CancellationToken cancellationToken)
		{
			return HttpResponse.Read (reader, cancellationToken);
		}

		internal override Task WriteRequest (HttpRequest request, CancellationToken cancellationToken)
		{
			return request.Write (writer, cancellationToken);
		}

		internal override Task WriteResponse (HttpResponse response, CancellationToken cancellationToken)
		{
			return response.Write (writer, cancellationToken);
		}

		protected override void Close ()
		{
			if (reader != null) {
				reader.Dispose ();
				reader = null;
			}
			if (writer != null) {
				writer.Dispose ();
				writer.Dispose ();
			}
			if (Stream != null) {
				Stream.Dispose ();
				Stream = null;
			}
		}
	}
}
