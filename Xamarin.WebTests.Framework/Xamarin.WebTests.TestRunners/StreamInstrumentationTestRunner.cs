﻿//
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
	public class StreamInstrumentationTestRunner : ClientAndServer, IConnectionInstrumentation
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

		internal InstrumentationFlags EffectiveFlags => GetFlags (EffectiveType);

		public ConnectionHandler ConnectionHandler {
			get;
		}

		public StreamInstrumentationTestRunner (Connection server, Connection client, ConnectionTestProvider provider,
							StreamInstrumentationParameters parameters)
			: base (server, client, parameters)
		{
			ConnectionHandler = new DefaultConnectionHandler (this);
		}

		const StreamInstrumentationType MartinTest = StreamInstrumentationType.DisposeDuringClientAuth;

		public static IEnumerable<StreamInstrumentationType> GetStreamInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.SslStreamInstrumentation:
				yield return StreamInstrumentationType.ClientHandshake;
				yield return StreamInstrumentationType.ReadDuringClientAuth;
				yield return StreamInstrumentationType.CloseBeforeClientAuth;
				yield return StreamInstrumentationType.CloseDuringClientAuth;
				yield return StreamInstrumentationType.RemoteClosesConnectionDuringRead;
				yield return StreamInstrumentationType.ShortReadDuringClientAuth;
				yield return StreamInstrumentationType.CleanShutdown;
				yield return StreamInstrumentationType.DoubleShutdown;
				yield return StreamInstrumentationType.WriteAfterShutdown;
				yield return StreamInstrumentationType.ReadAfterShutdown;
				yield return StreamInstrumentationType.ShortReadAndClose;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationExperimental:
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationMono:
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

		protected override void InitializeConnection (TestContext ctx)
		{
			ConnectionHandler.InitializeConnection (ctx);
			base.InitializeConnection (ctx);
		}

		protected sealed override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (4, "StreamInstrumentationTestRunner({0}) - main loop", EffectiveType);
			if (HasFlag (InstrumentationFlags.SkipMainLoop))
				return;
			await ConnectionHandler.MainLoop (ctx, cancellationToken);
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
			SkipMainLoop = 1024,

			HandshakeFails = ClientHandshakeFails | ServerHandshakeFails,

			NeedClientInstrumentation = ClientInstrumentation | ClientStream | ClientShutdown,
			NeedServerInstrumentation = ServerInstrumentation | ServerStream | ServerShutdown
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
			case StreamInstrumentationType.DisposeDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ServerInstrumentation |
					InstrumentationFlags.ClientHandshake | InstrumentationFlags.SkipMainLoop |
					InstrumentationFlags.ServerHandshake | InstrumentationFlags.ServerHandshakeFails |
					InstrumentationFlags.ClientHandshakeFails;
			case StreamInstrumentationType.ShortReadAndClose:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
			case StreamInstrumentationType.ReadTimeout:
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
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

		void LogDebug (TestContext ctx, int level, string message, params object[] args)
		{
			var formatted = string.Format (message, args);
			ctx.LogDebug (level, string.Format ("StreamInstrumentationTestRunner({0}): {1}", EffectiveType, formatted));
		}

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedClientInstrumentation) != 0)
				return Client.Start (ctx, this, cancellationToken);
			return Client.Start (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedServerInstrumentation) != 0)
				return Server.Start (ctx, this, cancellationToken);
			return Server.Start (ctx, null, cancellationToken);
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!HasAnyFlag (InstrumentationFlags.ClientShutdown)) {
				if (HasAnyFlag (InstrumentationFlags.HandshakeFails))
					return FinishedTask;
				return Client.Shutdown (ctx, cancellationToken);
			}

			switch (EffectiveType) {
			case StreamInstrumentationType.ShortReadAndClose:
				return Instrumentation_ShortReadAndClose (ctx);
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
				return Instrumentation_CleanClientShutdown (ctx, cancellationToken);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return Instrumentation_RemoteClosesConnectionDuringRead (ctx, cancellationToken);
			case StreamInstrumentationType.ReadTimeout:
				return Instrumentation_ReadTimeout (ctx, cancellationToken);
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!HasAnyFlag (InstrumentationFlags.ServerShutdown)) {
				if (HasAnyFlag (InstrumentationFlags.HandshakeFails))
					return FinishedTask;
				return Server.Shutdown (ctx, cancellationToken);
			}

			LogDebug (ctx, 4, "ServerShutdown()");

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
				return Instrumentation_CleanServerShutdown (ctx, cancellationToken);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
			case StreamInstrumentationType.ShortReadAndClose:
				return FinishedTask;
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		public Stream CreateClientStream (TestContext ctx, Connection connection, Socket socket)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedClientInstrumentation) == 0)
				throw ctx.AssertFail ("CreateClientStream()");

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			LogDebug (ctx, 4, "CreateClientStream()");

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
					LogDebug (ctx, 4, "CreateClientStream(): client handshake");
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
			if ((EffectiveFlags & InstrumentationFlags.NeedServerInstrumentation) == 0)
				throw ctx.AssertFail ("CreateServerStream()");

			var instrumentation = new StreamInstrumentation (ctx, socket);
			if (Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			LogDebug (ctx, 4, "CreateServerStream()");

			return instrumentation;
		}

		public async Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			if (!HasFlag (InstrumentationFlags.ClientHandshake))
				return false;

			int readCount = 0;

			clientInstrumentation.OnNextRead (ReadHandler);

			LogDebug (ctx, 4, "ClientHandshake()");
				  
			Constraint constraint;
			if (EffectiveType == StreamInstrumentationType.InvalidDataDuringClientAuth)
				constraint = Is.InstanceOf<AuthenticationException> ();
			else if (EffectiveType == StreamInstrumentationType.DisposeDuringClientAuth)
				constraint = Is.InstanceOf<ObjectDisposedException> ().Or.InstanceOfType<IOException> ();
			else
				constraint = Is.InstanceOf<IOException> ();

			await ctx.AssertException (handshake, constraint, "client handshake").ConfigureAwait (false);

			Server.Dispose ();

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
					LogDebug (ctx, 4, "ClientHandshake(): client handshake");
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
				case StreamInstrumentationType.DisposeDuringClientAuth:
					Client.SslStream.Dispose ();
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

			LogDebug (ctx, 4, "ServerHandshake() - expecting failure");

			Constraint constraint;
			if (EffectiveType == StreamInstrumentationType.DisposeDuringClientAuth)
				constraint = Is.InstanceOf<ObjectDisposedException> ().Or.InstanceOfType<IOException> ();
			else
				constraint = Is.InstanceOf<ObjectDisposedException> ();

			await ctx.AssertException (handshake, constraint, "server handshake").ConfigureAwait (false);

			Client.Close ();

			return true;
		}

		async Task Instrumentation_RemoteClosesConnectionDuringRead (TestContext ctx, CancellationToken cancellationToken)
		{
			clientInstrumentation.OnNextRead ((buffer, offset, count, func, innerCancellationToken) => {
				return ctx.Assert (
					() => func (buffer, offset, count, innerCancellationToken),
					Is.EqualTo (0), "inner read returns zero");
			});

			var outerCts = new CancellationTokenSource (5000);

			var readBuffer = new byte[256];
			var readTask = Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, outerCts.Token);

			Server.Close ();

			await ctx.Assert (() => readTask, Is.EqualTo (0), "read returns zero").ConfigureAwait (false);
		}

		TaskCompletionSource<bool> cleanClientShutdownTcs = new TaskCompletionSource<bool> ();

		async Task Instrumentation_CleanServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "Instrumentation_CleanServerShutdown";
			LogDebug (ctx, 4, me);

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.DoubleShutdown:
				return;
			case StreamInstrumentationType.ReadAfterShutdown:
				await ReadAfterShutdown ().ConfigureAwait (false);
				return;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			async Task ReadAfterShutdown ()
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var ok = await cleanClientShutdownTcs.Task.ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - client finished {1}", me, ok);

				cancellationToken.ThrowIfCancellationRequested ();

				await ConnectionHandler.WriteBlob (ctx, Server, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - write done", me);
			}
		}

		async Task Instrumentation_CleanClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "Instrumentation_CleanClientShutdown";
			LogDebug (ctx, 4, me);

			int bytesWritten = 0;

			clientInstrumentation.OnNextWrite (WriteHandler);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				await Client.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - done", me);
			} catch (OperationCanceledException) {
				LogDebug (ctx, 4, "{0} - canceled");
				cleanClientShutdownTcs.TrySetCanceled ();
				throw;
			} catch (Exception ex) {
				LogDebug (ctx, 4, "{0} - error", me, ex);
				cleanClientShutdownTcs.TrySetException (ex);
				throw;
			}

			var ok = ctx.Expect (bytesWritten, Is.GreaterThan (0), "{0} - bytes written", me);
			ok &= ctx.Expect (Client.SslStream.CanWrite, Is.False, "{0} - SslStream.CanWrite", me);

			cleanClientShutdownTcs.TrySetResult (ok);

			cancellationToken.ThrowIfCancellationRequested ();

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
				break;
			case StreamInstrumentationType.DoubleShutdown:
				await DoubleShutdown ();
				break;
			case StreamInstrumentationType.WriteAfterShutdown:
				await WriteAfterShutdown ();
				break;
			case StreamInstrumentationType.ReadAfterShutdown:
				await ReadAfterShutdown ();
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			Task DoubleShutdown ()
			{
				// Don't use Client.Shutdown() as that might throw a different exception.
				return ctx.AssertException<InvalidOperationException> (Client.SslStream.ShutdownAsync, "double shutdown");
			}

			Task WriteAfterShutdown ()
			{
				return ctx.AssertException<InvalidOperationException> (() => ConnectionHandler.WriteBlob (ctx, Client, cancellationToken));
			}

			async Task ReadAfterShutdown ()
			{
				LogDebug (ctx, 4, "{0} reading", me);
				await ConnectionHandler.ExpectBlob (ctx, Client, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} reading done", me);
			}

			async Task WriteHandler (byte[] buffer, int offset, int size,
			                         StreamInstrumentation.AsyncWriteFunc func,
						 CancellationToken innerCancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				innerCancellationToken.ThrowIfCancellationRequested ();
				LogDebug (ctx, 4, "{0} - write handler: {1} {2}", me, offset, size);
				await func (buffer, offset, size, innerCancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - write handler done", me);
				bytesWritten += size;
			}
		}

		// FIXME: broken
		async Task Instrumentation_ReadTimeout (TestContext ctx, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<bool> ();

			ctx.LogMessage ("TEST!");

			clientInstrumentation.OnNextRead (async (buffer, offset, count, func, innerCancellationToken) => {
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
		}

		async Task Instrumentation_ShortReadAndClose (TestContext ctx)
		{
			var me = "Instrumentation_ShortReadAndClose()";
			LogDebug (ctx, 4, me);

			bool readDone = false;

			clientInstrumentation.OnNextRead (ReadHandler);

			var writeBuffer = ConnectionHandler.TheQuickBrownFoxBuffer;
			var readBuffer = new byte[writeBuffer.Length + 256];

			var readTask = ClientRead ();

			await Server.Stream.WriteAsync (writeBuffer, 0, writeBuffer.Length);

			Server.Close ();

			LogDebug (ctx, 4, "{0} - closed server", me);

			await ctx.Assert (() => readTask, Is.EqualTo (writeBuffer.Length), "first client read").ConfigureAwait (false);

			LogDebug (ctx, 4, "{0} - first read done", me);

			readDone = true;

			await ctx.Assert (ClientRead, Is.EqualTo (0), "second client read").ConfigureAwait (false);

			LogDebug (ctx, 4, "{0} - second read done", me);

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				clientInstrumentation.OnNextRead (ReadHandler);

				LogDebug (ctx, 4, "{0} - read handler: {1} {2}", me, offset, size);
				var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - read handler done: {1}", me, ret);

				if (readDone)
					ctx.Assert (ret, Is.EqualTo (0), "inner read returns zero");
				return ret;
			}

			Task<int> ClientRead ()
			{
				return Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, CancellationToken.None);
			}
		}

		// FIXME: broken
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

		// FIXME: broken
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
