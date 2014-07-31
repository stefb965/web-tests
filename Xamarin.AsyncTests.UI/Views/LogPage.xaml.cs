﻿//
// LogPage.xaml.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
﻿using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{	
	public partial class LogPage : ContentPage
	{
		public TestApp App {
			get;
			private set;
		}

		public LogPage (TestApp app)
		{
			App = app;

			InitializeComponent ();
		}

		public void Log (string message)
		{
			// FIXME: Can we somehow auto-scroll to bottom?
			var line = string.Format ("[{0}]   {1}{2}", DateTime.Now, message, Environment.NewLine);
			Editor.Text = line + Editor.Text;
		}

		public void Clear ()
		{
			Editor.Text = string.Empty;
		}
	}
}
