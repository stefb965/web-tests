﻿//
// TestHttpListener.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.TestRunners;

namespace Xamarin.WebTests.Tests {
	[AsyncTestFixture]
	public class TestHttpListener : ITestParameterSource<Handler> {
		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			switch (filter) {
			case "martin":
				yield return HelloWorldHandler.Simple;
				yield return HelloWorldHandler.Simple;
				break;
			}
		}

		[Martin]
		[ConnectionTestFlags (ConnectionTestFlags.RequireMonoServer)]
		// [HttpServerFlags (HttpServerFlags.HttpListener | HttpServerFlags.SSL | HttpServerFlags.ReuseConnection)]
		[HttpServerFlags (HttpServerFlags.HttpListener)]
		// [AsyncTest (ParameterFilter = "martin", Unstable = true)]
		public Task MartinTest (TestContext ctx, HttpServer server, Handler handler,
		                        CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, true);
		}
	}
}
