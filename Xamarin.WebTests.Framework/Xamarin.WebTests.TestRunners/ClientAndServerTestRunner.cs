﻿//
// ClientAndServerTestRunner.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;

	public abstract class ClientAndServerTestRunner : ClientAndServer
	{
		public ClientAndServerTestRunner (IServer server, IClient client)
			: base (server, client)
		{
		}

		public ClientAndServerTestRunner (IServer server, IClient client, ClientAndServerParameters parameters)
			: base (server, client, parameters)
		{
		}

		protected virtual Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await WaitForConnection (ctx, cancellationToken);
			} catch (ConnectionFinishedException) {
				return;
			}

			if ((Server.Parameters.Flags & ServerFlags.ExpectServerException) != 0)
				ctx.AssertFail ("expecting server exception");
			if ((Server.Parameters.Flags & ServerFlags.ClientAbortsHandshake) != 0)
				ctx.AssertFail ("expecting client to abort handshake");
			if ((Client.Parameters.Flags & (ClientFlags.ExpectTrustFailure | ClientFlags.ExpectWebException)) != 0)
				ctx.AssertFail ("expecting client exception");

			cancellationToken.ThrowIfCancellationRequested ();
			await OnRun (ctx, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			await MainLoop (ctx, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			if (SupportsCleanShutdown)
				await Shutdown (ctx, cancellationToken);
		}

		protected abstract Task MainLoop (TestContext ctx, CancellationToken cancellationToken);

		protected virtual void OnWaitForServerConnectionCompleted (TestContext ctx, Task task)
		{
			if ((Server.Parameters.Flags & ServerFlags.ExpectServerException) != 0) {
				ctx.Assert (task.IsFaulted, "expecting exception");
				throw new ConnectionFinishedException ();
			}

			if (task.IsFaulted) {
				if ((Server.Parameters.Flags & (ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException)) != 0)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			ctx.Assert (task.Status, Is.EqualTo (TaskStatus.RanToCompletion), "expecting success");
		}

		protected virtual void OnWaitForClientConnectionCompleted (TestContext ctx, Task task)
		{
			if (task.IsFaulted) {
				if ((Client.Parameters.Flags & (ClientFlags.ExpectWebException | ClientFlags.ExpectTrustFailure)) != 0)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			ctx.Assert (task.Status, Is.EqualTo (TaskStatus.RanToCompletion), "expecting success");
		}

		protected override Task WaitForServerConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = base.WaitForServerConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForServerConnectionCompleted (ctx, t));
		}

		protected override Task WaitForClientConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = base.WaitForClientConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForClientConnectionCompleted (ctx, t));
		}

		protected class ConnectionFinishedException : Exception
		{
		}
	}
}
