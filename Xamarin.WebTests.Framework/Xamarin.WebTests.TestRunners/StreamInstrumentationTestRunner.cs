//
// StreamInstrumentationTestRunner.cs
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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestFramework;
	using Resources;

	[StreamInstrumentationTestRunner]
	public class StreamInstrumentationTestRunner : ConnectionTestRunner, IConnectionInstrumentation
	{
		new public StreamInstrumentationParameters Parameters {
			get { return (StreamInstrumentationParameters)base.Parameters; }
		}

		public StreamInstrumentationTestRunner (IServer server, IClient client, ConnectionTestProvider provider,
		                                        StreamInstrumentationParameters parameters)
			: base (server, client, provider, parameters)
		{
		}

		protected override ConnectionHandler CreateConnectionHandler ()
		{
			return new DefaultConnectionHandler (this);
		}

		const StreamInstrumentationType MartinTest = StreamInstrumentationType.ClientHandshake;

		public static IEnumerable<StreamInstrumentationType> GetStreamInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.SslStreamInstrumentation:
				yield return StreamInstrumentationType.ClientHandshake;
				yield return StreamInstrumentationType.ReadDuringClientAuth;
				yield return StreamInstrumentationType.CloseBeforeClientAuth;
				yield return StreamInstrumentationType.CloseDuringClientAuth;
				yield return StreamInstrumentationType.InvalidDataDuringClientAuth;
				yield return StreamInstrumentationType.RemoteClosesConnectionDuringRead;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationMono:
				yield return StreamInstrumentationType.CleanShutdown;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationWorking:
				yield return StreamInstrumentationType.ReadDuringClientAuth;
				yield return StreamInstrumentationType.RemoteClosesConnectionDuringRead;
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return StreamInstrumentationType.MartinTest;
				yield break;

			default:
				throw new InternalErrorException ();
			}
		}

		static string GetTestName (ConnectionTestCategory category, StreamInstrumentationType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static StreamInstrumentationParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
									     StreamInstrumentationType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var name = GetTestName (category, type);

			return new StreamInstrumentationParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll, UseStreamInstrumentation = true
			};
		}

		StreamInstrumentation clientInstrumentation;
		StreamInstrumentation serverInstrumentation;

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

			return base.PostRun (ctx, cancellationToken);
		}

		[Flags]
		internal enum StreamInstrumentationFlags {
			None = 0,
			ClientInstrumentation = 1,
			ServerInstrumentation = 2,
			ClientHandshake = 4
		}

		static StreamInstrumentationFlags GetFlags (StreamInstrumentationType type)
		{
			switch (type) {
			case StreamInstrumentationType.ClientHandshake:
			case StreamInstrumentationType.ReadDuringClientAuth:
			case StreamInstrumentationType.CloseBeforeClientAuth:
			case StreamInstrumentationType.CloseDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				return StreamInstrumentationFlags.ClientInstrumentation | StreamInstrumentationFlags.ClientHandshake;
			case StreamInstrumentationType.ReadTimeout:
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return StreamInstrumentationFlags.ClientInstrumentation;
			case StreamInstrumentationType.MartinTest:
				goto case MartinTest;
			default:
				return StreamInstrumentationFlags.None;
			}
		}

		bool HasFlag (StreamInstrumentationFlags flag)
		{
			var flags = GetFlags (Parameters.Type);
			return (flags & flag) == flag;
		}

		protected override Task StartClient (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			if (HasFlag (StreamInstrumentationFlags.ClientInstrumentation))
				return base.StartClient (ctx, this, cancellationToken);
			return base.StartClient (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			if (HasFlag (StreamInstrumentationFlags.ServerInstrumentation))
				return base.StartServer (ctx, this, cancellationToken);
			return base.StartServer (ctx, null, cancellationToken);
		}

		Stream IConnectionInstrumentation.CreateNetworkStream (TestContext ctx, Connection connection, Socket socket)
		{
			if (HasFlag (StreamInstrumentationFlags.ClientInstrumentation))
				return CreateClientInstrumentation (ctx, connection, socket);
			return null;
		}

		Task<bool> IConnectionInstrumentation.Shutdown (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			switch (Parameters.Type) {
			case StreamInstrumentationType.CleanShutdown:
				return Instrumentation_CleanShutdown (ctx, shutdown, connection);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return Instrumentation_RemoteClosesConnectionDuringRead (ctx, shutdown, connection);
			case StreamInstrumentationType.ReadTimeout:
				return Instrumentation_ReadTimeout (ctx, shutdown, connection);
			case StreamInstrumentationType.ClientHandshake:
			case StreamInstrumentationType.ReadDuringClientAuth:
			case StreamInstrumentationType.CloseBeforeClientAuth:
			case StreamInstrumentationType.CloseDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				break;
			case StreamInstrumentationType.MartinTest:
				goto case MartinTest;
			}

			return Task.FromResult (false);
		}

		Stream CreateClientInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			if (connection.ConnectionType != ConnectionType.Client)
				return null;

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (4, "SslStreamTestRunner.CreateNetworkStream()");

			if (HasFlag (StreamInstrumentationFlags.ClientHandshake))
				Instrumentation_ClientHandshake (ctx, instrumentation);

			return instrumentation;
		}

		void Instrumentation_ClientHandshake (TestContext ctx, StreamInstrumentation instrumentation)
		{
			int readCount = 0;

			instrumentation.OnNextRead (ReadHandler);

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
			                             StreamInstrumentation.AsyncReadFunc func,
			                             CancellationToken cancellationToken)
			{
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				switch (Parameters.Type) {
				case StreamInstrumentationType.ClientHandshake:
					ctx.LogMessage ("CLIENT HANDSHAKE!");
					break;
				case StreamInstrumentationType.ReadDuringClientAuth:
					await ctx.AssertException<InvalidOperationException> (ReadClient).ConfigureAwait (false);
					break;
				case StreamInstrumentationType.CloseBeforeClientAuth:
					return 0;
				case StreamInstrumentationType.CloseDuringClientAuth:
					if (Interlocked.Increment (ref readCount) > 0)
						return 0;
					instrumentation.OnNextRead (ReadHandler);
					break;
				case StreamInstrumentationType.MartinTest:
					goto case MartinTest;
				default:
					throw ctx.AssertFail ("Unknown instrumentation type: '{0}'.", Parameters.Type);
				}

				return await func (buffer, offset, size, cancellationToken);
			}

			Task<int> ReadClient ()
			{
				const int bufferSize = 100;
				return Client.Stream.ReadAsync (new byte[bufferSize], 0, bufferSize);
			}
		}

		async Task<bool> Instrumentation_ReadTimeout (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			if (connection.ConnectionType != ConnectionType.Client)
				throw ctx.AssertFail ("Client only.");

			var tcs = new TaskCompletionSource<bool> ();

			ctx.LogMessage ("TEST!");

			clientInstrumentation.OnNextRead (async (buffer, offset, count, func, cancellationToken) => {
				ctx.LogMessage ("ON READ WITH TIMEOUT!");
				var result = await tcs.Task;
				ctx.LogMessage ("ON READ #1: {0}", result);
				if (!result)
					return 0;
				return -1;
			});

			var timeoutTask = Task.Delay (10000).ContinueWith (t => {
				ctx.LogMessage ("TIMEOUT!");
				tcs.TrySetResult (false);
			});

			var outerCts = new CancellationTokenSource (5000);

			var readBuffer = new byte[256];
			var readTask = Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, outerCts.Token);

			try {
				var ret = await readTask.ConfigureAwait (false);
				ctx.LogMessage ("READ TASK DONE: {0}", ret);
			} catch (Exception ex) {
				ctx.LogMessage ("READ TASK FAILED: {0}", ex.Message);
			} finally {
				tcs.TrySetResult (true);
				outerCts.Dispose ();
			}

			return true;
		}

		async Task<bool> Instrumentation_RemoteClosesConnectionDuringRead (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			ctx.Assert (connection.ConnectionType, Is.EqualTo (ConnectionType.Client));

			clientInstrumentation.OnNextRead ((buffer, offset, count, func, cancellationToken) => {
				return ctx.Assert (
					() => func (buffer, offset, count, cancellationToken),
					Is.EqualTo (0), "inner read returns zero");
			});

			var outerCts = new CancellationTokenSource (5000);

			var readBuffer = new byte[256];
			var readTask = Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, outerCts.Token);

			await Server.Shutdown (ctx, false, CancellationToken.None);

			await ctx.Assert (() => readTask, Is.EqualTo (0), "read returns zero").ConfigureAwait (false);
			return true;
		}

		async Task<bool> Instrumentation_CleanShutdown (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			if (connection.ConnectionType != ConnectionType.Client)
				return false;

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

			return true;
		}

		void Instrumentation_DisposeBeforeClientAuth (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead ((buffer, offset, count, func, cancellationToken) => {
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				ctx.LogMessage ("CALLING DISPOSE!");
				Client.SslStream.Dispose ();
				ctx.LogMessage ("CALLING DISPOSE DONE!");
				return func (buffer, offset, count, cancellationToken);
			});
		}

		Task Instrumentation_Dispose (TestContext ctx, Func<Task> shutdown)
		{
			ctx.LogMessage ("CALLING CLOSE!");
			var portable = DependencyInjector.Get<IPortableSupport> ();
			portable.Close (Client.SslStream);
			ctx.LogMessage ("DONE CALLING CLOSE!");
			return FinishedTask;
		}

		async Task Instrumentation_MartinTest (TestContext ctx, Func<Task> shutdown)
		{
			ctx.LogMessage ("DISPOSE INSTRUMENTATION!");

			var buffer = new byte[4096];
			var readTask = Server.Stream.ReadAsync (buffer, 0, buffer.Length);
			var readTask2 = readTask.ContinueWith (async t => {
				;
				ctx.LogMessage ("READ TASK: {0} {1}", t.Status, t.Id);

				await Task.Yield ();
				ctx.LogMessage ("READ TASK #1");
				await Task.Delay (5000);
				ctx.LogMessage ("READ TASK #2");

				var ret = await Server.Stream.ReadAsync (buffer, 0, buffer.Length);
				ctx.LogMessage ("READ TASK #1: {0}", ret);
			});

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

			await Task.Yield ();
			ctx.LogMessage ("SHUTDOWN TASK #1");

			await readTask.ConfigureAwait (false);
			ctx.LogMessage ("SHUTDOWN COMPLETE!");
		}

	}
}
