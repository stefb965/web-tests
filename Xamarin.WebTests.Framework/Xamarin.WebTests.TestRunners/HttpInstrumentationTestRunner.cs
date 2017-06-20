//
// HttpInstrumentationTestRunner.cs
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
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using Resources;

	[HttpInstrumentationTestRunner]
	public class HttpInstrumentationTestRunner : AbstractConnection
	{
		public ConnectionTestProvider Provider {
			get;
		}

		protected Uri Uri {
			get;
		}

		protected HttpServerFlags ServerFlags {
			get;
		}

		new public HttpInstrumentationTestParameters Parameters {
			get { return (HttpInstrumentationTestParameters)base.Parameters; }
		}

		public HttpInstrumentationTestType EffectiveType {
			get {
				if (Parameters.Type == HttpInstrumentationTestType.MartinTest)
					return MartinTest;
				return Parameters.Type;
			}
		}

		public HttpServer Server {
			get;
		}

		public HttpInstrumentationTestRunner (IPortableEndPoint endpoint, HttpInstrumentationTestParameters parameters,
						      ConnectionTestProvider provider, Uri uri, HttpServerFlags flags)
			: base (endpoint, parameters)
		{
			Provider = provider;
			ServerFlags = flags;
			Uri = uri;

			Server = new BuiltinHttpServer (uri, endpoint, flags, parameters, null) {
				
			};
		}

		const HttpInstrumentationTestType MartinTest = HttpInstrumentationTestType.Simple;

		public static IEnumerable<HttpInstrumentationTestType> GetInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();

			switch (category) {
			case ConnectionTestCategory.MartinTest:
				yield return HttpInstrumentationTestType.MartinTest;
				yield break;

			default:
				throw ctx.AssertFail (category);
			}
		}

		static string GetTestName (ConnectionTestCategory category, HttpInstrumentationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static HttpInstrumentationTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
		                                                               HttpInstrumentationTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var name = GetTestName (category, type);

			return new HttpInstrumentationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var handler = HelloWorldHandler.Simple;
			await TestRunner.RunTraditional (ctx, Server, handler, cancellationToken, true).ConfigureAwait (false);
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Destroy (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override void Stop ()
		{
			
		}
	}
}
