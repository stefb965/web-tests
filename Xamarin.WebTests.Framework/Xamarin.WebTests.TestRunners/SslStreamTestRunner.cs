//
// SslStreamTestRunner.cs
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestFramework;
	using Resources;

	[SslStreamTestRunner]
	public class SslStreamTestRunner : ConnectionTestRunner, IConnectionInstrumentation
	{
		new public SslStreamTestParameters Parameters {
			get { return (SslStreamTestParameters)base.Parameters; }
		}

		public SslStreamTestRunner (IServer server, IClient client, ConnectionTestProvider provider, SslStreamTestParameters parameters)
			: base (server, client, provider, parameters)
		{
		}

		protected override ConnectionHandler CreateConnectionHandler ()
		{
			return new DefaultConnectionHandler (this);
		}

		static string GetTestName (ConnectionTestCategory category, ConnectionTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static SslStreamTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category, ConnectionTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var rejectAll = certificateProvider.RejectAll ();
			var acceptNull = certificateProvider.AcceptNull ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromLocalCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			var name = GetTestName (category, type);

			switch (type) {
			case ConnectionTestType.Default:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll
				};

			case ConnectionTestType.AcceptFromLocalCA:
				return new SslStreamTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.NoValidator:
				// The default validator only allows ResourceManager.SelfSignedServerCertificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ExpectClientException = true
				};

			case ConnectionTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ExpectClientException = true, ClientCertificateValidator = rejectAll
				};

			case ConnectionTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.PenguinCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RequestClientCertificate:
				/*
				 * Request client certificate, but do not require it.
				 *
				 * FIXME:
				 * SslStream with Mono's old implementation fails here.
				 */
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.RequireClientCertificate:
				// Require client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.OptionalClientCertificate:
				/*
				 * Request client certificate without requiring one and do not provide it.
				 *
				 * To ask for an optional client certificate (without requiring it), you need to specify a custom validation
				 * callback and then accept the null certificate with `SslPolicyErrors.RemoteCertificateNotAvailable' in it.
				 *
				 * FIXME:
				 * Mono with the old TLS implementation throws SecureChannelFailure.
				 */
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, AskForClientCertificate = true,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RejectClientCertificate:
				// Reject client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = rejectAll, AskForClientCertificate = true,
					ExpectClientException = true, ExpectServerException = true
				};

			case ConnectionTestType.MissingClientCertificate:
				// Missing client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ExpectClientException = true, ExpectServerException = true
				};

			case ConnectionTestType.MustNotInvokeGlobalValidator:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll,
					GlobalValidationFlags = GlobalValidationFlags.MustNotInvoke
				};

			case ConnectionTestType.MustNotInvokeGlobalValidator2:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.MustNotInvoke,
					ExpectClientException = true
				};

			case ConnectionTestType.SyncAuthenticate:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll, SslStreamFlags = SslStreamFlags.SyncAuthenticate
				};

			case ConnectionTestType.ReadDuringClientAuth:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll, UseStreamInstrumentation = true
				};

			case ConnectionTestType.CleanShutdown:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll, UseStreamInstrumentation = true
				};

			case ConnectionTestType.MartinTest:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll, SslStreamFlags = SslStreamFlags.MartinTest,
					UseStreamInstrumentation = true
				};

			default:
				throw new InternalErrorException ();
			}
		}

		protected override async Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.OnRun (ctx, cancellationToken);

			if (Parameters.ExpectServerException)
				ctx.AssertFail ("expecting server exception");
			if (Parameters.ExpectClientException)
				ctx.AssertFail ("expecting client exception");

			if (!IsManualServer) {
				ctx.Expect (Server.SslStream.IsAuthenticated, "server is authenticated");

				if (Server.Parameters.RequireClientCertificate) {
					ctx.LogDebug (1, "Client certificate required: {0} {1}", Server.SslStream.IsMutuallyAuthenticated, Server.SslStream.RemoteCertificate != null);
					ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
					ctx.Expect (Server.SslStream.RemoteCertificate, Is.Not.Null, "server has client certificate");
				}
			}

			if (!IsManualClient) {
				ctx.Expect (Client.SslStream.IsAuthenticated, "client is authenticated");

				ctx.Expect (Client.SslStream.RemoteCertificate, Is.Not.Null, "client has server certificate");
			}

			if (!IsManualConnection && Server.Parameters.AskForClientCertificate && Client.Parameters.ClientCertificate != null)
				ctx.Expect (Client.SslStream.LocalCertificate, Is.Not.Null, "client has local certificate");

		}

		protected override void OnWaitForServerConnectionCompleted (TestContext ctx, Task task)
		{
			if (Parameters.ExpectServerException) {
				ctx.Assert (task.IsFaulted, "expecting exception");
				throw new ConnectionFinishedException ();
			}

			if (task.IsFaulted) {
				if (Parameters.ExpectClientException)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			base.OnWaitForServerConnectionCompleted (ctx, task);
		}

		protected override void OnWaitForClientConnectionCompleted (TestContext ctx, Task task)
		{
			if (task.IsFaulted) {
				if (Parameters.ExpectClientException)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			base.OnWaitForClientConnectionCompleted (ctx, task);
		}

		RemoteCertificateValidationCallback savedGlobalCallback;
		TestContext savedContext;
		bool restoreGlobalCallback;
		StreamInstrumentation clientInstrumentation;
		StreamInstrumentation serverInstrumentation;

		void SetGlobalValidationCallback (TestContext ctx, RemoteCertificateValidationCallback callback)
		{
			savedGlobalCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = callback;
			savedContext = ctx;
			restoreGlobalCallback = true;
		}

		bool GlobalValidator (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			savedContext.AssertFail ("Global validator has been invoked!");
			return false;
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			savedGlobalCallback = ServicePointManager.ServerCertificateValidationCallback;

			if (Parameters.GlobalValidationFlags == GlobalValidationFlags.MustNotInvoke)
				SetGlobalValidationCallback (ctx, GlobalValidator);
			else if (Parameters.GlobalValidationFlags != 0)
				ctx.AssertFail ("Invalid GlobalValidationFlags");

			ctx.Assert (Parameters.ExpectChainStatus, Is.Null, "Parameters.ExpectChainStatus");
			ctx.Assert (Parameters.ExpectPolicyErrors, Is.Null, "Parameters.ExpectPolicyErrors");

			return base.PreRun (ctx, cancellationToken);
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (clientInstrumentation != null) {
				clientInstrumentation.Dispose ();
				clientInstrumentation = null;
			}
			if (serverInstrumentation != null) {
				serverInstrumentation.Dispose ();
				serverInstrumentation = null;
							}

			if (restoreGlobalCallback)
				ServicePointManager.ServerCertificateValidationCallback = savedGlobalCallback;

			return base.PostRun (ctx, cancellationToken);
		}

		protected override Task StartClient (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			switch (Parameters.Type) {
			case ConnectionTestType.ReadDuringClientAuth:
			case ConnectionTestType.MartinTest:
				return base.StartClient (ctx, this, cancellationToken);
			default:
				return base.StartClient (ctx, null, cancellationToken);
			}
		}

		protected override Task StartServer (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			switch (Parameters.Type) {
			case ConnectionTestType.MartinTest:
				return base.StartServer (ctx, this, cancellationToken);
			default:
				return base.StartServer (ctx, null, cancellationToken);
			}
		}

		Stream IConnectionInstrumentation.CreateNetworkStream (TestContext ctx, Connection connection, Socket socket)
		{
			switch (Parameters.Type) {
			case ConnectionTestType.ReadDuringClientAuth:
			case ConnectionTestType.MartinTest:
				return CreateClientInstrumentation (ctx, connection, socket);
			default:
				return null;
			}
		}

		Task IConnectionInstrumentation.Shutdown (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			if (connection.ConnectionType != ConnectionType.Client)
				return FinishedTask;

			switch (Parameters.Type) {
			case ConnectionTestType.CleanShutdown:
				return Instrumentation_CleanShutdown (ctx, shutdown);
			}

			return shutdown ();
		}

		Stream CreateClientInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			if (connection.ConnectionType != ConnectionType.Client)
				return null;

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (4, "SslStreamTestRunner.CreateNetworkStream()");

			switch (Parameters.Type) {
			case ConnectionTestType.ReadDuringClientAuth:
				Instrumentation_ReadBeforeClientAuth (ctx, instrumentation);
				break;
			case ConnectionTestType.MartinTest:
				Instrumentation_Dispose (ctx, instrumentation);			                        
				break;
			}

			return instrumentation;
		}

		void Instrumentation_ReadBeforeClientAuth (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead (() => {
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				var buffer = new byte[100];
				ctx.AssertException<InvalidOperationException> (() => Client.Stream.Read (buffer, 0, buffer.Length));
			});
		}

		async Task Instrumentation_CleanShutdown (TestContext ctx, Func<Task> shutdown)
		{
			ctx.LogMessage ("DISPOSE INSTRUMENTATION!");

			clientInstrumentation.OnNextWrite (() => {
				ctx.LogMessage ("ON WRITE!");
			});

			ctx.LogMessage ("CALLING SHUTDOWN!");
			try {
				await shutdown ().ConfigureAwait (false);
				ctx.LogMessage ("SHUTDOWN DONE!");
			} catch (Exception ex) {
				ctx.LogMessage ("SHUTDOWN FAILED: {0}", ex);
				throw;
			}
		}

		void Instrumentation_DisposeBeforeClientAuth (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead (() => {
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				ctx.LogMessage ("CALLING DISPOSE!");
				Client.SslStream.Dispose ();
				ctx.LogMessage ("CALLING DISPOSE DONE!");
			});
		}
	}
}

