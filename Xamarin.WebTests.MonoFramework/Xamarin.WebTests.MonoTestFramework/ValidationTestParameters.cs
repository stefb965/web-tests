//
// ValidationTestParameters.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Resources;

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	public abstract class ValidationTestParameters : ITestParameter, ICloneable
	{
		public ValidationTestCategory Category {
			get;
			private set;
		}

		public string Identifier {
			get;
			private set;
		}

		string ITestParameter.Value {
			get { return Identifier; }
		}

		List<CertificateResourceType> types = new List<CertificateResourceType> ();

		public IReadOnlyCollection<CertificateResourceType> Types {
			get {
				return types;
			}
		}

		public string Host {
			get; set;
		}

		public void Add (CertificateResourceType type)
		{
			types.Add (type);
		}

		public ValidationTestParameters (ValidationTestCategory category, string identifier)
		{
			Category = category;
			Identifier = identifier;
		}

		protected ValidationTestParameters (ValidationTestParameters other)
		{
			Category = other.Category;
			Identifier = other.Identifier;
			types.AddRange (other.types);
			Host = other.Host;
		}

		object ICloneable.Clone ()
		{
			return DeepClone ();
		}

		public abstract ValidationTestParameters DeepClone ();
	}
}

