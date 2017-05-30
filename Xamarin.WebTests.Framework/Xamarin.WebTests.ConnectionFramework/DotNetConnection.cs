//
// DotNetConnection.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using ConnectionFramework;

	public abstract class DotNetConnection : Connection, ICommonConnection
	{
		public DotNetConnection (ConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, GetEndPoint (parameters), parameters)
		{
		}

		Socket socket;
		Socket accepted;
		Stream innerStream;
		IConnectionInstrumentation instrumentation;
		TaskCompletionSource<SslStream> tcs;
		int shutdown;
		int aborted;

		SslStream sslStream;

		public Stream Stream {
			get { return sslStream; }
		}

		public SslStream SslStream {
			get { return sslStream; }
		}

		public ProtocolVersions ProtocolVersion {
			get { return (ProtocolVersions)SslStream.SslProtocol; }
		}

		protected abstract bool IsServer {
			get;
		}

		public bool HasFlag (SslStreamFlags flags)
		{
			return (Parameters.SslStreamFlags & flags) != 0;
		}

		protected abstract Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken);

		static IPortableEndPoint GetEndPoint (ConnectionParameters parameters)
		{
			if (parameters.EndPoint != null)
				return parameters.EndPoint;

			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetLoopbackEndpoint (4433);
		}

		IPEndPoint GetEndPoint ()
		{
			if (EndPoint != null)
				return new IPEndPoint (IPAddress.Parse (EndPoint.Address), EndPoint.Port);
			else
				return new IPEndPoint (IPAddress.Loopback, 4433);
		}

		void CreateSslStream (TestContext ctx, Socket innerSocket)
		{
			if (instrumentation != null) {
				if (IsServer)
					innerStream = instrumentation.CreateServerStream (ctx, this, innerSocket);
				else
					innerStream = instrumentation.CreateClientStream (ctx, this, innerSocket);
				if (innerStream == null)
					innerStream = new NetworkStream (innerSocket, true);
			} else {
				innerStream = new NetworkStream (innerSocket, true);
			}

			sslStream = Provider.SslStreamProvider.CreateSslStream (ctx, innerStream, Parameters, IsServer);
		}

		public sealed override Task Start (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			this.instrumentation = instrumentation;

			if (IsServer)
				StartServer (ctx, cancellationToken);
			else
				StartClient (ctx, cancellationToken);
			return FinishedTask;
		}

		void StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var endpoint = GetEndPoint ();
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind (endpoint);
			socket.Listen (1);

			ctx.LogMessage ("Listening at {0}.", endpoint);

			tcs = new TaskCompletionSource<SslStream> ();

			socket.BeginAccept (async ar => {
				try {
					accepted = socket.EndAccept (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					ctx.LogMessage ("Accepted connection from {0}.", accepted.RemoteEndPoint);
					CreateSslStream (ctx, accepted);
					await Handshake (ctx, cancellationToken).ConfigureAwait (false);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		void StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			var endpoint = GetEndPoint ();
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			ctx.LogMessage ("Connecting to {0}.", endpoint);

			tcs = new TaskCompletionSource<SslStream> ();

			socket.BeginConnect (endpoint, async ar => {
				try {
					socket.EndConnect (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					CreateSslStream (ctx, socket);
					await Handshake (ctx, cancellationToken).ConfigureAwait (false);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		async Task Handshake (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			if (instrumentation != null) {
				Task<bool> task;
				if (IsServer)
					task = instrumentation.ServerHandshake (ctx, TheHandshake, this);
				else
					task = instrumentation.ClientHandshake (ctx, TheHandshake, this);
				if (await task.ConfigureAwait (false))
					return;
			}

			cancellationToken.ThrowIfCancellationRequested ();

			await TheHandshake ().ConfigureAwait (false);

			Task TheHandshake ()
			{
				return Start (ctx, sslStream, cancellationToken);
			}
		}

		public sealed override Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return tcs.Task;
		}

		public sealed override async Task Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref aborted, 1, 0) != 0)
				return;
			if (Interlocked.CompareExchange (ref shutdown, 1, 0) != 0)
				return;

			if (instrumentation != null) {
				Task<bool> task;
				if (IsServer)
					task = instrumentation.ServerShutdown (ctx, Shutdown_internal, this, cancellationToken);
				else
					task = instrumentation.ClientShutdown (ctx, Shutdown_internal, this, cancellationToken);
				if (await task.ConfigureAwait (false))
					return;
			}

			await Shutdown_internal ();

			async Task Shutdown_internal ()
			{
				if (SupportsCleanShutdown)
					await sslStream.ShutdownAsync ().ConfigureAwait (false);

				ctx.LogDebug (5, "Shutting down socket.");
				(IsServer ? accepted : socket).Shutdown (SocketShutdown.Send);
			}
		}

		public override void Abort ()
		{
			if (Interlocked.CompareExchange (ref aborted, 1, 0) != 0)
				return;
			if (innerStream != null) {
				innerStream.Dispose ();
			}
		}

		protected override void Stop ()
		{
			shutdown = 1;

			try {
				if (sslStream != null) {
					sslStream.Dispose ();
					sslStream = null;
				}
			} catch {
				;
			}
			if (accepted != null) {
				try {
					accepted.Shutdown (SocketShutdown.Both);
				} catch {
					;
				}
				try {
					accepted.Dispose ();
				} catch {
					;
				}
				accepted = null;
			}
			if (socket != null) {
				try {
					socket.Shutdown (SocketShutdown.Both);
				} catch {
					;
				}
				try {
					socket.Dispose ();
				} catch {
					;
				}
				socket = null;
			}
//			instrumentation = null;
		}

	}
}

