﻿﻿﻿//
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
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

		public StreamInstrumentationType EffectiveType {
			get {
				if (Parameters.Type == StreamInstrumentationType.MartinTest)
					return MartinTest;
				return Parameters.Type;
			}
		}

		public StreamInstrumentationTestRunner (IServer server, IClient client, ConnectionTestProvider provider,
							StreamInstrumentationParameters parameters)
			: base (server, client, provider, parameters)
		{
		}

		protected override ConnectionHandler CreateConnectionHandler ()
		{
			return new StreamInstrumentationConnectionHandler (this);
		}

		const StreamInstrumentationType MartinTest = StreamInstrumentationType.CloseBeforeClientAuth;

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
				yield return StreamInstrumentationType.ClientHandshake;
				yield return StreamInstrumentationType.ReadDuringClientAuth;
				yield return StreamInstrumentationType.CloseBeforeClientAuth;
				yield return StreamInstrumentationType.CloseDuringClientAuth;
				yield return StreamInstrumentationType.RemoteClosesConnectionDuringRead;
				yield return StreamInstrumentationType.ShortReadDuringClientAuth;
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return StreamInstrumentationType.MartinTest;
				yield break;

			default:
				throw ctx.AssertFail (category);
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
				ClientCertificateValidator = acceptAll
			};
		}

		class StreamInstrumentationConnectionHandler : DefaultConnectionHandler
		{
			public StreamInstrumentationTestRunner Parent {
				get;
			}

			public StreamInstrumentationConnectionHandler (StreamInstrumentationTestRunner runner)
				: base (runner)
			{
				Parent = runner;
			}

			public override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogDebug (4, "StreamInstrumentation - main loop");
				if (Parent.HasFlag (InstrumentationFlags.SkipMainLoop))
					return;
				await base.MainLoop (ctx, cancellationToken);
			}
		}

		StreamInstrumentation clientInstrumentation;
		StreamInstrumentation serverInstrumentation;

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HasFlag (InstrumentationFlags.ServerHandshakeFails))
				Parameters.ExpectServerException = true;
			if (HasFlag (InstrumentationFlags.ClientHandshakeFails))
				Parameters.ExpectClientException = true;
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

			return base.PostRun (ctx, cancellationToken);
		}

		[Flags]
		internal enum InstrumentationFlags {
			None = 0,
			ClientInstrumentation = 1,
			ServerInstrumentation = 2,
			ClientStream = 4,
			ServerStream = 8,
			ClientHandshake = 16,
			ServerHandshake = 32,
			ClientShutdown = 64,
			ServerShutdown = 128,
			ServerHandshakeFails = 256,
			ClientHandshakeFails = 512,
			SkipMainLoop = 1024
		}

		static InstrumentationFlags GetFlags (StreamInstrumentationType type)
		{
			switch (type) {
			case StreamInstrumentationType.ClientHandshake:
			case StreamInstrumentationType.ReadDuringClientAuth:
			case StreamInstrumentationType.ShortReadDuringClientAuth:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ClientStream;
			case StreamInstrumentationType.CloseBeforeClientAuth:
			case StreamInstrumentationType.CloseDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ServerInstrumentation |
					InstrumentationFlags.ClientHandshake | InstrumentationFlags.SkipMainLoop |
					InstrumentationFlags.ServerHandshake | InstrumentationFlags.ServerHandshakeFails |
					InstrumentationFlags.ClientHandshakeFails;
			case StreamInstrumentationType.ReadTimeout:
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ClientShutdown;
			default:
				throw new InternalErrorException ();
			}
		}

		bool HasFlag (InstrumentationFlags flag)
		{
			var flags = GetFlags (EffectiveType);
			return (flags & flag) == flag;
		}

		bool HasAnyFlag (params InstrumentationFlags[] flags)
		{
			return flags.Any (f => HasFlag (f));
		}

		protected override Task StartClient (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			if (HasFlag (InstrumentationFlags.ClientInstrumentation))
				return base.StartClient (ctx, this, cancellationToken);
			return base.StartClient (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			ctx.Assert (instrumentation, Is.Null);
			if (HasFlag (InstrumentationFlags.ServerInstrumentation))
				return base.StartServer (ctx, this, cancellationToken);
			return base.StartServer (ctx, null, cancellationToken);
		}

		public Task<bool> ClientShutdown (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			if (!HasFlag (InstrumentationFlags.ClientShutdown))
				return Task.FromResult (false);

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
				return Instrumentation_CleanShutdown (ctx, shutdown, connection);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return Instrumentation_RemoteClosesConnectionDuringRead (ctx, shutdown, connection);
			case StreamInstrumentationType.ReadTimeout:
				return Instrumentation_ReadTimeout (ctx, shutdown, connection);
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		public Task<bool> ServerShutdown (TestContext ctx, Func<Task> shutdown, Connection connection)
		{
			if (!HasAnyFlag (InstrumentationFlags.ServerShutdown))
				return Task.FromResult (false);

			switch (EffectiveType) {
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		public Stream CreateClientStream (TestContext ctx, Connection connection, Socket socket)
		{
			if (!HasFlag (InstrumentationFlags.ClientInstrumentation))
				return null;

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (4, "SslStreamTestRunner.CreateClientStream()");

			if (!HasFlag (InstrumentationFlags.ClientStream))
				return instrumentation;

			instrumentation.OnNextRead (ReadHandler);

			return instrumentation;

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				switch (EffectiveType) {
				case StreamInstrumentationType.ClientHandshake:
					ctx.LogMessage ("CLIENT HANDSHAKE!");
					break;
				case StreamInstrumentationType.ReadDuringClientAuth:
					await ctx.AssertException<InvalidOperationException> (ReadClient).ConfigureAwait (false);
					break;
				case StreamInstrumentationType.ShortReadDuringClientAuth:
					if (size <= 5)
						instrumentation.OnNextRead (ReadHandler);
					size = 1;
					break;
				default:
					throw ctx.AssertFail (EffectiveType);
				}

				return await func (buffer, offset, size, cancellationToken);
			}

			Task<int> ReadClient ()
			{
				const int bufferSize = 100;
				return Client.Stream.ReadAsync (new byte[bufferSize], 0, bufferSize);
			}
		}

		public Stream CreateServerStream (TestContext ctx, Connection connection, Socket socket)
		{
			if (!HasAnyFlag (InstrumentationFlags.ServerInstrumentation, InstrumentationFlags.ServerShutdown))
				return null;

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (4, "SslStreamTestRunner.CreateServerStream()");

			return instrumentation;
		}

		public async Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			if (!HasFlag (InstrumentationFlags.ClientHandshake))
				return false;

			int readCount = 0;

			clientInstrumentation.OnNextRead (ReadHandler);

			ctx.LogMessage ("CLIENT HANDSHAKE!");

			Constraint constraint;
			if (EffectiveType == StreamInstrumentationType.InvalidDataDuringClientAuth)
				constraint = Is.InstanceOf<AuthenticationException> ();
			else
				constraint = Is.InstanceOf<IOException> ();

			await ctx.AssertException (handshake, constraint, "client handshake").ConfigureAwait (false);

			Server.Abort ();

			return true;

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				switch (EffectiveType) {
				case StreamInstrumentationType.ClientHandshake:
					ctx.LogMessage ("CLIENT HANDSHAKE!");
					break;
				case StreamInstrumentationType.CloseBeforeClientAuth:
					return 0;
				case StreamInstrumentationType.CloseDuringClientAuth:
					if (Interlocked.Increment (ref readCount) > 0)
						return 0;
					clientInstrumentation.OnNextRead (ReadHandler);
					break;
				case StreamInstrumentationType.InvalidDataDuringClientAuth:
					clientInstrumentation.OnNextRead (ReadHandler);
					break;
				default:
					throw ctx.AssertFail (EffectiveType);
				}

				var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

				if (EffectiveType == StreamInstrumentationType.InvalidDataDuringClientAuth) {
					if (ret > 50) {
						for (int i = 10; i < 40; i++)
							buffer[i] = 0xAA;
					}
				}

				return ret;
			}
		}

		public async Task<bool> ServerHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			if (!HasAnyFlag (InstrumentationFlags.ServerHandshakeFails))
				return false;

			ctx.LogMessage ("EXPECTING SERVER HANDSHAKE TO FAIL");

			// serverInstrumentation.OnNextRead (ReadHandler);

			await ctx.AssertException<ObjectDisposedException> (handshake, "server handshake").ConfigureAwait (false);

			Client.Abort ();

			return true;

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				serverInstrumentation.OnNextRead (ReadHandler);

				try {
					return await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);
				} catch {
					return 0;
				}
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
