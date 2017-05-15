//
// HttpListenerHandler.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.HttpHandlers
{
	public class HttpListenerHandler : Handler
	{
		public HttpListenerOperation Operation {
			get;
		}

		public HttpListenerHandler (HttpListenerOperation operation, string identifier = null)
			: base (identifier ?? operation.ToString ())
		{
			Operation = operation;
		}

		public override void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
			switch (Operation) {
			case HttpListenerOperation.Get:
				break;
			case HttpListenerOperation.MartinTest:
				break;
			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			switch (Operation) {
			case HttpListenerOperation.Get:
			case HttpListenerOperation.MartinTest:
				return ctx.Expect (response.IsSuccess, "Response.IsSuccess");
			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}
		}

		public override object Clone ()
		{
			return new HttpListenerHandler (Operation, Identifier);
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			await Task.FromResult<object> (null).ConfigureAwait (false);

			var context = connection.HttpListenerContext;

			ctx.LogMessage ("HANDLE REQUEST: {0}", context.Request);

			switch (Operation) {
			case HttpListenerOperation.Get:
			case HttpListenerOperation.MartinTest:
				return HttpResponse.CreateSuccess ();
			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}

			throw new NotImplementedException ();
		}
	}
}
