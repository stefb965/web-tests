using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

using MSI = Mono.Security.Interface;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	class MonoClient : MonoConnection, IMonoClient
	{
		public MonoConnectionParameters MonoParameters {
			get { return base.Parameters as MonoConnectionParameters; }
		}

		public MonoClient (MonoConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, parameters)
		{
		}

		protected override bool IsServer {
			get { return false; }
		}

		protected override void GetSettings (TestContext ctx, MSI.MonoTlsSettings settings)
		{
			if (MonoParameters != null && MonoParameters.ClientCiphers != null)
				settings.EnabledCiphers = MonoParameters.ClientCiphers.ToArray ();
		}

		protected override async Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			var targetHost = Parameters.TargetHost ?? EndPoint.HostName ?? EndPoint.Address;
			ctx.LogDebug (1, "Using '{0}' as target host.", targetHost);

			var protocol = Provider.SslStreamProvider.GetProtocol (Parameters, IsServer);
			var clientCertificates = Provider.SslStreamProvider.GetClientCertificates (Parameters);

			await sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false).ConfigureAwait (false);

			ctx.LogDebug (1, "Successfully authenticated client.");
		}
	}
}
