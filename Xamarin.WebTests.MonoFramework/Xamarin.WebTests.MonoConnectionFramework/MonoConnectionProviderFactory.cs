//
// MonoConnectionProviderFactory.cs
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
using System.Net;
using System.Threading;
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;
using Mono.Security.Interface;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	public class MonoConnectionProviderFactory : IConnectionProviderFactoryExtension, ISingletonInstance
	{
		int initialized;

		public static readonly Guid NewTlsID = new Guid ("e5ff34f1-8b7a-4aa6-aff9-24719d709693");
		public static readonly Guid OldTlsID = new Guid ("cf8baa0d-c6ed-40ae-b512-dec8d097e9af");

		internal MonoConnectionProviderFactory ()
		{
		}

		public void Initialize (ConnectionProviderFactory factory, IDefaultConnectionSettings settings)
		{
			if (Interlocked.Exchange (ref initialized, 1) != 0)
				throw new InvalidOperationException ();

			var providers = DependencyInjector.GetCollection<IMonoTlsProviderFactory> ();
			foreach (var provider in providers) {
				var monoProvider = new MonoConnectionProvider (factory, provider.ConnectionProviderType, provider.ConnectionProviderFlags, provider.Name, provider.Provider);
				factory.Install (monoProvider);

				if (settings.InstallTlsProvider != null && provider.Provider.ID == settings.InstallTlsProvider.Value)
					MonoTlsProviderFactory.SetDefaultProvider (provider.Provider.Name);
			}
		}

		public void RegisterProvider (IMonoTlsProviderFactory factory)
		{
			if (initialized != 0)
				throw new InvalidOperationException ();

			DependencyInjector.RegisterCollection<IMonoTlsProviderFactory> (factory);
		}

		public void RegisterProvider (string name, MonoTlsProvider provider, ConnectionProviderType type, ConnectionProviderFlags flags)
		{
			RegisterProvider (new FactoryImpl (name, provider, type, flags));
		}

		class FactoryImpl : IMonoTlsProviderFactory
		{
			public string Name {
				get;
				private set;
			}

			public MonoTlsProvider Provider {
				get;
				private set;
			}

			public ConnectionProviderType ConnectionProviderType {
				get;
				private set;
			}

			public ConnectionProviderFlags ConnectionProviderFlags {
				get;
				private set;
			}

			public FactoryImpl (string name, MonoTlsProvider provider, ConnectionProviderType type, ConnectionProviderFlags flags)
			{
				Name = name;
				Provider = provider;
				ConnectionProviderType = type;
				ConnectionProviderFlags = flags;
			}
		}
	}
}

