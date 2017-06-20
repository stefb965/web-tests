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
using System.Net.Sockets;
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
	public class HttpInstrumentationTestRunner : AbstractConnection, IHttpServerDelegate
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
				Delegate = this
			};
		}

		const HttpInstrumentationTestType MartinTest = HttpInstrumentationTestType.AbortDuringHandshake;

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
			requestDoneTcs = new TaskCompletionSource<bool> ();

			var handler = HelloWorldHandler.Simple;

			HttpStatusCode expectedStatus;
			WebExceptionStatus expectedError;

			switch (EffectiveType) {
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.SecureChannelFailure;
				break;
			case HttpInstrumentationTestType.AbortDuringHandshake:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.RequestCanceled;
				break;
			case HttpInstrumentationTestType.Simple:
				expectedStatus = HttpStatusCode.OK;
				expectedError = WebExceptionStatus.Success;
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			var runner = new MyRunner (this, Server, handler, true);
			try {
				await runner.Run (ctx, cancellationToken, expectedStatus, expectedError).ConfigureAwait (false);
				requestDoneTcs.TrySetResult (true);
			} catch {
				requestDoneTcs.TrySetResult (false);
				throw;
			}
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

		void ConfigureRequest (TestContext ctx, Uri uri, TraditionalRequest request)
		{
			if (Interlocked.CompareExchange (ref currentRequest, request, null) != null)
				throw ctx.AssertFail ("Invalid nested call!");
		}

		class MyRunner : TraditionalTestRunner
		{
			public HttpInstrumentationTestRunner Parent {
				get;
			}

			public MyRunner (HttpInstrumentationTestRunner parent, HttpServer server, Handler handler, bool sendAsync)
				: base (server, handler, sendAsync)
			{
				Parent = parent;
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				Parent.ConfigureRequest (ctx, uri, (TraditionalRequest)request);
				base.ConfigureRequest (ctx, uri, request);
			}
		}

		StreamInstrumentation serverInstrumentation;
		TraditionalRequest currentRequest;
		TaskCompletionSource<bool> requestDoneTcs;

		async Task<bool> IHttpServerDelegate.CheckCreateConnection (
			TestContext ctx, HttpConnection connection, Task initTask,
			CancellationToken cancellationToken)
		{
			try
			{
				await initTask.ConfigureAwait (false);
				return true;
			} catch (OperationCanceledException) {
				return false;
			} catch {
				if (EffectiveType == HttpInstrumentationTestType.InvalidDataDuringHandshake ||
				    EffectiveType == HttpInstrumentationTestType.AbortDuringHandshake)
					return false;
				throw;
			}
		}

		bool IHttpServerDelegate.HandleConnection (TestContext ctx, HttpConnection connection, HttpRequest request, Handler handler)
		{
			ctx.LogMessage ("HANDLE CONNECTION!");
			return true;
		}

		Stream IHttpServerDelegate.CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			var instrumentation = new StreamInstrumentation (ctx, Parameters.Identifier, socket, ownsSocket);
			if (Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null) != null)
				throw ctx.AssertFail ("Invalid nested call!");

			InstallReadHandler (ctx, instrumentation);

			return instrumentation;
		}

		void InstallReadHandler (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead ((b, o, s, f, c) => ReadHandler (ctx, instrumentation, b, o, s, f, c));
		}

		async Task<int> ReadHandler (TestContext ctx, StreamInstrumentation instrumentation,
		                             byte[] buffer, int offset, int size,
		                             StreamInstrumentation.AsyncReadFunc func,
		                             CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			switch (EffectiveType) {
			case HttpInstrumentationTestType.Simple:
			case HttpInstrumentationTestType.AbortDuringHandshake:
				break;
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				InstallReadHandler (ctx, instrumentation);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			if (EffectiveType == HttpInstrumentationTestType.AbortDuringHandshake) {
				ctx.Assert (currentRequest, Is.Not.Null, "current request");
				currentRequest.Request.Abort ();
				// Wait until the client request finished, to make sure we are actually aboring.
				await requestDoneTcs.Task.ConfigureAwait (false);
			}

			var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

			if (EffectiveType == HttpInstrumentationTestType.InvalidDataDuringHandshake) {
				if (ret > 50) {
					for (int i = 10; i < 40; i++)
						buffer[i] = 0xAA;
				}
			}

			return ret;
		}
	}
}
