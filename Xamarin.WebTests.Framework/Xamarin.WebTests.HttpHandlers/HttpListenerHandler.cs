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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
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

		public string ExpectedUrl {
			get; set;
		}

		public string ExpectedRawUrl {
			get; set;
		}

		public Request CreateRequest (TestContext ctx, HttpServer server, Uri uri)
		{
			switch (Operation) {
			case HttpListenerOperation.Get:
				return new TraditionalRequest (uri);

			case HttpListenerOperation.MartinTest:
			case HttpListenerOperation.TestUriEscape:
				var key = "Product/1";
				ExpectedRawUrl = uri.AbsolutePath + Uri.EscapeDataString (key) + "/";
				ExpectedUrl = uri.AbsoluteUri + Uri.EscapeDataString (key) + "/";
				return new BuiltinRequest (server, new Uri (ExpectedUrl), "GET");

			case HttpListenerOperation.SimpleBuiltin:
			case HttpListenerOperation.TestCookies:
				return new BuiltinRequest (server, uri, "GET");
			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}
		}

		void ConfigureBuiltinRequest (TestContext ctx, BuiltinRequest request, Uri uri)
		{
			switch (Operation) {
			case HttpListenerOperation.TestCookies:
				ctx.Assert (uri.Host, Is.EqualTo ("127.0.0.1"), "Uri.Host");
				request.Request.CustomHeaderSection = "Host: 127.0.0.1\r\n" +
					"Cookie:$Version=\"1\"; " +
					"Cookie1=Value1; $Path=\"/\"; " +
					"CookieM=ValueM; $Path=\"/p2\"; $Domain=\"test\"; $Port=\"99\";" +
					"Cookie2=Value2; $Path=\"/foo\";" +
					"\r\n" +
					"\r\n";
				break;
			default:
				request.AddHeader ("Host", uri.Host);
				break;
			}
		}

		public override void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
			switch (Operation) {
			case HttpListenerOperation.Get:
				break;
			case HttpListenerOperation.SimpleBuiltin:
			case HttpListenerOperation.TestCookies:
			case HttpListenerOperation.TestUriEscape:
			case HttpListenerOperation.MartinTest:
				ConfigureBuiltinRequest (ctx, (BuiltinRequest)request, uri);
				break;
			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			switch (Operation) {
			case HttpListenerOperation.Get:
			case HttpListenerOperation.SimpleBuiltin:
			case HttpListenerOperation.TestCookies:
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

		HttpResponse CheckCookies (TestContext ctx, HttpListenerContext context)
		{
			var ok = ctx.Expect (context.Request.Cookies.Count, Is.EqualTo (3), "#1");

			foreach (Cookie c in context.Request.Cookies) {
				ctx.LogDebug (8, "COOKIE: {0} - {1} - |{2}|{3}|{4}|{5}|", c, c.Name, c.Value, c.Path, c.Port, c.Domain);

				switch (c.Name) {
				case "Cookie1":
					ok &= ctx.Expect (c.Value, Is.EqualTo ("Value1"), "#2");
					ok &= ctx.Expect (c.Path, Is.EqualTo ("\"/\""), "#3");
					ok &= ctx.Expect (c.Port.Length, Is.EqualTo (0), "#4");
					ok &= ctx.Expect (c.Domain.Length, Is.EqualTo (0), "#5");
					break;
				case "CookieM":
					ok &= ctx.Expect (c.Value, Is.EqualTo ("ValueM"), "#6");
					ok &= ctx.Expect (c.Path, Is.EqualTo ("\"/p2\""), "#7");
					ok &= ctx.Expect (c.Port, Is.EqualTo ("\"99\""), "#8");
					ok &= ctx.Expect (c.Domain, Is.EqualTo ("\"test\""), "#9");
					break;
				case "Cookie2":
					ok &= ctx.Expect (c.Value, Is.EqualTo ("Value2"), "#10");
					ok &= ctx.Expect (c.Path, Is.EqualTo ("\"/foo\""), "#11");
					ok &= ctx.Expect (c.Port.Length, Is.EqualTo (0), "#12");
					ok &= ctx.Expect (c.Domain.Length, Is.EqualTo (0), "#13");
					break;
				default:
					ctx.Expect (false, "Invalid cookie name " + c.Name);
					ok = false;
					break;
				}
			}

			if (ok)
				return HttpResponse.CreateSuccess ();
			return HttpResponse.CreateError ("Test failed.");
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			await Task.FromResult<object> (null).ConfigureAwait (false);

			var context = connection.HttpListenerContext;
			ctx.LogDebug (8, "HANDLE REQUEST: {0} {1}", context.Request.RawUrl, context.Request.Url);

			var ok = true;
			if (ExpectedRawUrl != null)
				ok &= ctx.Expect (context.Request.RawUrl, Is.EqualTo (ExpectedRawUrl), "ExpectedRawUrl");
			if (ExpectedUrl != null)
				ok &= ctx.Expect (context.Request.Url.AbsoluteUri, Is.EqualTo (ExpectedUrl), "ExpectedUrl");

			if (!ok)
				return HttpResponse.CreateError ("Assertion failed.");

			switch (Operation) {
			case HttpListenerOperation.Get:
			case HttpListenerOperation.SimpleBuiltin:
				return HttpResponse.CreateSuccess ();

			case HttpListenerOperation.TestCookies:
				return CheckCookies (ctx, context);

			case HttpListenerOperation.MartinTest:
			case HttpListenerOperation.TestUriEscape:
				return HttpResponse.CreateSuccess ();

			default:
				throw ctx.AssertFail ("Unknown HttpListenerOperation `{0}'.", Operation);
			}

			throw new NotImplementedException ();
		}
	}
}
