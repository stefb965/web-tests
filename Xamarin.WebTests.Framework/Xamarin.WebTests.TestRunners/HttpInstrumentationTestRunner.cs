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

		public HttpInstrumentationTestType EffectiveType => GetEffectiveType (Parameters.Type);

		static HttpInstrumentationTestType GetEffectiveType (HttpInstrumentationTestType type)
		{
			if (type == HttpInstrumentationTestType.MartinTest)
				return MartinTest;
			return type;
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

		const HttpInstrumentationTestType MartinTest = HttpInstrumentationTestType.CancelQueuedRequest;

		public static IEnumerable<HttpInstrumentationTestType> GetInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();

			switch (category) {
			case ConnectionTestCategory.MartinTest:
				yield return HttpInstrumentationTestType.MartinTest;
				yield break;

			case ConnectionTestCategory.HttpInstrumentation:
				yield return HttpInstrumentationTestType.Simple;
				yield return HttpInstrumentationTestType.InvalidDataDuringHandshake;
				yield return HttpInstrumentationTestType.AbortDuringHandshake;
				yield return HttpInstrumentationTestType.ParallelRequests;
				yield return HttpInstrumentationTestType.SimpleQueuedRequest;
				yield return HttpInstrumentationTestType.ThreeParallelRequests;
				yield return HttpInstrumentationTestType.ParallelRequestsSomeQueued;
				yield return HttpInstrumentationTestType.ManyParallelRequests;
				yield break;

			case ConnectionTestCategory.HttpInstrumentationStress:
				yield return HttpInstrumentationTestType.ManyParallelRequestsStress;
				yield break;

			case ConnectionTestCategory.HttpInstrumentationExperimental:
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

			var parameters = new HttpInstrumentationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			switch (GetEffectiveType (type)) {
			case HttpInstrumentationTestType.SimpleQueuedRequest:
			case HttpInstrumentationTestType.CancelQueuedRequest:
				parameters.ConnectionLimit = 1;
				break;
			case HttpInstrumentationTestType.ThreeParallelRequests:
				parameters.ConnectionLimit = 5;
				break;
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
				parameters.CountParallelRequests = 5;
				parameters.ConnectionLimit = 3;
				break;
			case HttpInstrumentationTestType.ManyParallelRequests:
				parameters.CountParallelRequests = 10;
				parameters.ConnectionLimit = 5;
				break;
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				parameters.CountParallelRequests = 100;
				parameters.ConnectionLimit = 25;
				break;
			}

			return parameters;
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
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
			case HttpInstrumentationTestType.SimpleQueuedRequest:
			case HttpInstrumentationTestType.CancelQueuedRequest:
			case HttpInstrumentationTestType.ParallelRequests:
			case HttpInstrumentationTestType.ThreeParallelRequests:
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				expectedStatus = HttpStatusCode.OK;
				expectedError = WebExceptionStatus.Success;
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			currentOperation = new Operation (this, handler, false, expectedStatus, expectedError);

			await currentOperation.Run (ctx, cancellationToken).ConfigureAwait (false);

			switch (EffectiveType) {
			case HttpInstrumentationTestType.ParallelRequests:
				ctx.Assert (readHandlerCalled, Is.EqualTo (2), "ReadHandler called twice");
				break;
			case HttpInstrumentationTestType.ThreeParallelRequests:
				ctx.Assert (readHandlerCalled, Is.EqualTo (3), "ReadHandler called three times");
				break;
			case HttpInstrumentationTestType.SimpleQueuedRequest:
				ctx.Assert (queuedTask, Is.Not.Null, "have queued task");
				await queuedTask.ConfigureAwait (false);
				ctx.Assert (readHandlerCalled, Is.EqualTo (2), "ReadHandler called twice");
				break;
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				// ctx.Assert (readHandlerCalled, Is.EqualTo (Parameters.CountParallelRequests + 1), "ReadHandler count");
				break;
			}
		}

		async Task<Operation> StartParallel (TestContext ctx, CancellationToken cancellationToken, Handler handler,
		                                     HttpStatusCode expectedStatus = HttpStatusCode.OK,
		                                     WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			await Server.StartParallel (ctx, cancellationToken).ConfigureAwait (false);
			return new Operation (this, handler, true, expectedStatus, expectedError);
		}

		async Task RunParallel (TestContext ctx, CancellationToken cancellationToken, Handler handler,
		                        HttpStatusCode expectedStatus = HttpStatusCode.OK,
		                        WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			var operation = await StartParallel (ctx, cancellationToken, handler, expectedStatus, expectedError).ConfigureAwait (false);
			await operation.Run (ctx, cancellationToken);
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

		class Operation : TraditionalTestRunner
		{
			public HttpInstrumentationTestRunner Parent {
				get;
			}

			public bool IsParallelRequest {
				get;
			}

			public HttpStatusCode ExpectedStatus {
				get;
			}

			public WebExceptionStatus ExpectedError {
				get;
			}

			TraditionalRequest currentRequest;
			ServicePoint servicePoint;
			TaskCompletionSource<TraditionalRequest> requestTask;
			TaskCompletionSource<bool> requestDoneTask;

			public bool HasRequest => currentRequest != null;

			public TraditionalRequest Request {
				get {
					if (currentRequest == null)
						throw new InvalidOperationException ();
					return currentRequest;
				}
			}

			public ServicePoint ServicePoint {
				get {
					if (servicePoint == null)
						throw new InvalidOperationException ();
					return servicePoint;
				}
			}

			public Operation (HttpInstrumentationTestRunner parent, Handler handler,
			                  bool parallel, HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent.Server, handler, true)
			{
				Parent = parent;
				IsParallelRequest = parallel;
				ExpectedStatus = expectedStatus;
				ExpectedError = expectedError;
				requestTask = new TaskCompletionSource<TraditionalRequest> ();
				requestDoneTask = new TaskCompletionSource<bool> ();
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				currentRequest = (TraditionalRequest)request;
				servicePoint = currentRequest.RequestExt.ServicePoint;
				requestTask.SetResult (currentRequest);

				if (IsParallelRequest)
					ConfigureParallelRequest (ctx);
				else
					ConfigureRequest (ctx);

				base.ConfigureRequest (ctx, uri, request);
			}

			public void ConfigureParallelRequest (TestContext ctx)
			{
				switch (Parent.EffectiveType) {
				case HttpInstrumentationTestType.ParallelRequests:
				case HttpInstrumentationTestType.SimpleQueuedRequest:
				case HttpInstrumentationTestType.CancelQueuedRequest:
					ctx.Assert (servicePoint, Is.Not.Null, "ServicePoint");
					ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (1), "ServicePoint.CurrentConnections");
					break;
				case HttpInstrumentationTestType.ThreeParallelRequests:
				case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
				case HttpInstrumentationTestType.ManyParallelRequests:
				case HttpInstrumentationTestType.ManyParallelRequestsStress:
					break;
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			public void ConfigureRequest (TestContext ctx)
			{
				if (Parent.Parameters.ConnectionLimit != 0)
					ServicePoint.ConnectionLimit = Parent.Parameters.ConnectionLimit;
				if (Parent.Parameters.IdleTime != 0)
					ServicePoint.MaxIdleTime = Parent.Parameters.IdleTime;
			}

			public async Task Run (TestContext ctx, CancellationToken cancellationToken)
			{
				try {
					await Run (ctx, cancellationToken, ExpectedStatus, ExpectedError).ConfigureAwait (false);
					requestDoneTask.TrySetResult (true);
				} catch {
					requestDoneTask.TrySetResult (false);
					throw;
				}
			}

			public Task<TraditionalRequest> WaitForRequest ()
			{
				return requestTask.Task;
			}

			public Task<bool> WaitForCompletion ()
			{
				return requestDoneTask.Task;
			}
		}

		StreamInstrumentation serverInstrumentation;
		Operation currentOperation;
		int readHandlerCalled;
		TraditionalRequest queuedRequest;
		Task queuedTask;

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
			return true;
		}

		Stream IHttpServerDelegate.CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			var instrumentation = new StreamInstrumentation (ctx, Parameters.Identifier, socket, ownsSocket);
			var old = Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null);
			InstallReadHandler (ctx, old == null, instrumentation);
			return instrumentation;
		}

		void InstallReadHandler (TestContext ctx, bool primary, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead ((b, o, s, f, c) => ReadHandler (ctx, primary, instrumentation, b, o, s, f, c));
		}

		async Task<int> ReadHandler (TestContext ctx, bool primary,
					     StreamInstrumentation instrumentation,
					     byte[] buffer, int offset, int size,
					     StreamInstrumentation.AsyncReadFunc func,
					     CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

			Interlocked.Increment (ref readHandlerCalled);

			switch (EffectiveType) {
			case HttpInstrumentationTestType.Simple:
				ctx.Assert (primary, "Primary request");
				break;

			case HttpInstrumentationTestType.ParallelRequests:
				ctx.Assert (currentOperation.HasRequest, "current request");
				if (primary) {
					await RunParallel (ctx, cancellationToken, HelloWorldHandler.Simple).ConfigureAwait (false);
				} else {
					ctx.Assert (currentOperation.ServicePoint.CurrentConnections, Is.EqualTo (2), "ServicePoint.CurrentConnections");
				}
				break;

			case HttpInstrumentationTestType.SimpleQueuedRequest:
				ctx.Assert (currentOperation.HasRequest, "current request");
				if (primary) {
					var task = RunParallel (ctx, cancellationToken, HelloWorldHandler.Simple);
					if (Interlocked.CompareExchange (ref queuedTask, task, null) != null)
						throw ctx.AssertFail ("Invalid nested call");
				}
				break;

			case HttpInstrumentationTestType.ThreeParallelRequests:
				ctx.Assert (currentOperation.HasRequest, "current request");
				if (primary) {
					var secondTask = RunParallel (ctx, cancellationToken, HelloWorldHandler.Simple);
					var thirdTask = RunParallel (ctx, cancellationToken, HelloWorldHandler.Simple);
					await Task.WhenAll (secondTask, thirdTask).ConfigureAwait (false);
				} else {
					ctx.Assert (currentOperation.ServicePoint.CurrentConnections, Is.EqualTo (3), "ServicePoint.CurrentConnections");
				}
				break;

			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				ctx.Assert (currentOperation.HasRequest, "current request");
				if (primary) {
					var parallelTasks = new Task [Parameters.CountParallelRequests];
					for (int i = 0; i < parallelTasks.Length; i++)
						parallelTasks [i] = RunParallel (ctx, cancellationToken, HelloWorldHandler.Simple);
					await Task.WhenAll (parallelTasks).ConfigureAwait (false);
				} else {
					// ctx.Expect (currentServicePoint.CurrentConnections, Is.EqualTo (3), "ServicePoint.CurrentConnections");
				}
				break;

			case HttpInstrumentationTestType.AbortDuringHandshake:
				ctx.Assert (primary, "Primary request");
				ctx.Assert (currentOperation.HasRequest, "current request");
				currentOperation.Request.Request.Abort ();
				// Wait until the client request finished, to make sure we are actually aboring.
				await currentOperation.WaitForCompletion ().ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				ctx.Assert (primary, "Primary request");
				InstallReadHandler (ctx, primary, instrumentation);
				break;

			case HttpInstrumentationTestType.CancelQueuedRequest:
				ctx.Assert (currentOperation.HasRequest, "current request");
				if (primary) {
					var operation = await StartParallel (
						ctx, cancellationToken, HelloWorldHandler.Simple,
						HttpStatusCode.InternalServerError, WebExceptionStatus.RequestCanceled).ConfigureAwait (false);
					var task = operation.Run (ctx, cancellationToken);
					if (Interlocked.CompareExchange (ref queuedTask, task, null) != null)
						throw new InvalidOperationException ("Invalid nested call.");
					var request = await operation.WaitForRequest ().ConfigureAwait (false);
					// Wait a bit to make sure the request has been queued.
					await Task.Delay (500).ConfigureAwait (false);
					request.Request.Abort ();
				}
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}

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
