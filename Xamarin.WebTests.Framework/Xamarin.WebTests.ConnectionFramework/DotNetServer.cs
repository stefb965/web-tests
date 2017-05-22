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
	public class DotNetServer : DotNetConnection, IServer
	{
		readonly ISslStreamProvider sslStreamProvider;

		public DotNetServer (ConnectionProvider provider, ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return true; }
		}

		protected override async Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken)
		{
			var certificate = Parameters.ServerCertificate;
			var protocol = sslStreamProvider.GetProtocol (Parameters, IsServer);
			var askForCert = Parameters.AskForClientCertificate || Parameters.RequireClientCertificate;

			Task task;
			string function;
			if (HasFlag (SslStreamFlags.SyncAuthenticate)) {
				function = "SslStream.AuthenticateAsServer()";
				ctx.LogDebug (1, "Calling {0} synchronously.", function);
				task = Task.Run (() => sslStream.AuthenticateAsServer (certificate, askForCert, protocol, false));
			} else {
				function = "SslStream.AuthenticateAsServerAsync()";
				ctx.LogDebug (1, "Calling {0} async.", function);
				task = sslStream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false);
			}

			try {
				await task.ConfigureAwait (false);
				ctx.LogDebug (1, "{0} completed successfully.", function);
			} catch (Exception ex) {
				ctx.LogError (string.Format ("{0} failed.", function), ex);
				throw;
			}
		}
	}
}

