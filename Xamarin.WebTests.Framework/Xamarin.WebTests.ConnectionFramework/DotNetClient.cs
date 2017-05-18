﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
// using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class DotNetClient : DotNetConnection, IClient
	{
		readonly ISslStreamProvider sslStreamProvider;

		public DotNetClient (ConnectionProvider provider, ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return false; }
		}

		protected override async Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			var targetHost = Parameters.TargetHost ?? EndPoint.HostName ?? EndPoint.Address;
			ctx.LogDebug (1, "Using '{0}' as target host.", targetHost);

			var protocol = sslStreamProvider.GetProtocol (Parameters, IsServer);
			var clientCertificates = sslStreamProvider.GetClientCertificates (Parameters);

			await sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false).ConfigureAwait (false);

			ctx.LogDebug (1, "Successfully authenticated client.");
		}
	}
}

