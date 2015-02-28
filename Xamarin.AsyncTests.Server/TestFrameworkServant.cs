﻿//
// TestFrameworkServant.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	class TestFrameworkServant : ObjectServant, RemoteTestFramework
	{
		public TestApp App {
			get;
			private set;
		}

		public TestFramework LocalFramework {
			get;
			private set;
		}

		public override string Type {
			get { return "TestFramework"; }
		}

		public TestFrameworkServant (ServerConnection connection)
			: base (connection)
		{
			App = connection.App;
			LocalFramework = App.GetLocalTestFramework ();
		}

		TestFrameworkClient RemoteObject<TestFrameworkClient,TestFrameworkServant>.Client {
			get { throw new ServerErrorException (); }
		}

		TestFrameworkServant RemoteObject<TestFrameworkClient, TestFrameworkServant>.Servant {
			get { return this; }
		}

		public Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken)
		{
			return LocalFramework.LoadTestSuite (cancellationToken);
		}

		public Task<TestCase> ResolveTest (TestContext ctx, ITestPath path, CancellationToken cancellationToken)
		{
			return LocalFramework.ResolveTest (ctx, path, cancellationToken);
		}
	}
}
