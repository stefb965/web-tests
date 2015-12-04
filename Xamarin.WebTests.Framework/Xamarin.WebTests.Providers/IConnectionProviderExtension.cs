using System;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Providers
{
	public interface IConnectionProviderFactoryExtension : IExtensionCollection
	{
		void Initialize (ConnectionProviderFactory factory, IDefaultConnectionSettings settings);
	}
}
