using System;
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
		public abstract bool SupportsCleanShutdown {
			get;
		}

		public abstract ProtocolVersions SupportedProtocols {
			get;
		}

		public abstract ConnectionType ConnectionType {
			get;
		}

		protected Connection (IPortableEndPoint endpoint, ConnectionParameters parameters)
			: base (endpoint, parameters)
		{
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
		public abstract Task Shutdown (TestContext ctx, bool attemptCleanShutdown, CancellationToken cancellationToken);
	}
}

