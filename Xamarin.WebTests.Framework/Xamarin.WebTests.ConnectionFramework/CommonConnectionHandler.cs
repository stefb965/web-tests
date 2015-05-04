﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class CommonConnectionHandler : IConnectionHandler
	{
		public ICommonConnection Connection {
			get;
			private set;
		}

		public CommonConnectionHandler (ICommonConnection connection)
		{
			Connection = connection;
		}

		public bool SupportsCleanShutdown {
			get { return Connection.SupportsCleanShutdown; }
		}

		public async Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			await Connection.WaitForConnection ();
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			await WaitForConnection (ctx, cancellationToken);
			var wrapper = new StreamWrapper (Connection.Stream);
			await MainLoop (ctx, wrapper, cancellationToken);
		}

		protected abstract Task MainLoop (TestContext ctx, ILineBasedStream stream, CancellationToken cancellationToken);

		public Task<bool> Shutdown (TestContext ctx, bool attemptCleanShutdown, bool waitForReply, CancellationToken cancellationToken)
		{
			return Connection.Shutdown (attemptCleanShutdown, waitForReply);
		}

		public void Close ()
		{
			Connection.Dispose ();
		}
	}
}

