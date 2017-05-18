﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;

using MSI = Mono.Security.Interface;

using SSCX = System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	class MonoServer : MonoConnection, IMonoServer
	{
		public MonoConnectionParameters MonoParameters {
			get { return base.Parameters as MonoConnectionParameters; }
		}

		public MonoServer (MonoConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, parameters)
		{
			clientCertIssuersProp = typeof (MSI.MonoTlsSettings).GetTypeInfo ().GetDeclaredProperty ("ClientCertificateIssuers");
		}

		PropertyInfo clientCertIssuersProp;

		protected override bool IsServer {
			get { return true; }
		}

		protected override void GetSettings (TestContext ctx, MSI.MonoTlsSettings settings)
		{
			if (MonoParameters == null)
				return;

			if (MonoParameters.ServerCiphers != null)
				settings.EnabledCiphers = MonoParameters.ServerCiphers.ToArray ();

			if (MonoParameters.ClientCertificateIssuers != null) {
				if (clientCertIssuersProp == null)
					ctx.AssertFail ("MonoTlsSettings.ClientCertificateIssuers is not supported!");
				clientCertIssuersProp.SetValue (settings, MonoParameters.ClientCertificateIssuers);
			}
		}

		protected override async Task<SslStream> Start (TestContext ctx, Stream stream, MSI.MonoTlsSettings settings, CancellationToken cancellationToken)
		{
			var server = await ConnectionProvider.CreateServerStreamAsync (stream, Parameters, settings, cancellationToken);

			ctx.LogMessage ("Successfully authenticated server.");

			return server;
		}
	}
}
