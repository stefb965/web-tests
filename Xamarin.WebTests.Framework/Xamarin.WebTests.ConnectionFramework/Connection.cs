﻿using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class Connection : AbstractConnection, IConnection
	{
		public ConnectionProvider Provider {
			get;
		}

		public bool SupportsCleanShutdown => Provider.SupportsCleanShutdown;

		public ProtocolVersions SupportedProtocols => Provider.SupportedProtocols;

		public abstract ConnectionType ConnectionType {
			get;
		}

		protected Connection (ConnectionProvider provider, ConnectionParameters parameters)
			: base (GetEndPoint (parameters), parameters)
		{
			Provider = provider;
		}

		static IPortableEndPoint GetEndPoint (ConnectionParameters parameters)
		{
			if (parameters.EndPoint != null)
				return parameters.EndPoint;

			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetLoopbackEndpoint (4433);
		}

		protected override Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Start (ctx, null, cancellationToken);
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => Close ());
		}

		[StackTraceEntryPoint]
		public abstract Task Start (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task Shutdown (TestContext ctx, CancellationToken cancellationToken);

		public abstract void Abort ();
	}
}

