using System;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionConfigurationProviderCollection : ITestConfigurationProvider
	{
		List<ReflectionConfigurationProvider> providers;
		List<TestFeature> features;
		List<TestCategory> categories;
		bool resolved;

		public ReflectionConfigurationProviderCollection (string name)
		{
			Name = name;
			providers = new List<ReflectionConfigurationProvider> ();
		}

		public string Name {
			get;
			private set;
		}

		public void Add (ITestConfigurationProvider provider)
		{
			Add (new ReflectionConfigurationProvider (provider));
		}

		public void Add (ReflectionConfigurationProvider provider)
		{
			if (resolved)
				throw new InvalidOperationException ();
			providers.Add (provider);
		}

		public void Resolve ()
		{
			if (resolved)
				return;

			features = new List<TestFeature> ();
			categories = new List<TestCategory> ();

			foreach (var provider in providers) {
				features.AddRange (provider.Features);
				categories.AddRange (provider.Categories);
			}

			resolved = true;
		}

		public IEnumerable<TestFeature> Features {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return features;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return categories;
			}
		}
	}
}

