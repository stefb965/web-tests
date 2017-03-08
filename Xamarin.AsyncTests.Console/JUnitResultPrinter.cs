﻿//
// JUnitResultPrinter.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)

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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.Console
{
	public class JUnitResultPrinter
	{
		public TestResult Result {
			get;
			private set;
		}

		public bool ShowIgnored {
			get { return showIgnored ?? true; }
			set { showIgnored = value; }
		}

		bool? showIgnored;
		int current;

		JUnitResultPrinter (TestResult result)
		{
			Result = result;
		}

		public static void Print (TestResult result, string output)
		{
			var settings = new XmlWriterSettings {
				Indent = true
			};
			using (var writer = XmlWriter.Create (output, settings)) {
				var printer = new JUnitResultPrinter (result);
				var root = new XElement ("testsuites");
				printer.Print (root);
				root.WriteTo (writer);
			}
		}

		bool Print (XElement root)
		{
			var timestamp = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);
			Visit (root, TestName.Empty, Result);
			return true;
		}

		static int nextId;

		XElement Print (XElement root, TestName parent, TestResult node)
		{
			var timestamp = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);
			var suite = new XElement ("testsuite");
			suite.SetAttributeValue ("id", node.Name.ID);
			// suite.SetAttributeValue ("name", "X" + parent.Name + "Y" + node.Name.Name + "Z");
			suite.SetAttributeValue ("name", parent.Name);
			suite.SetAttributeValue ("errors", "0");
			suite.SetAttributeValue ("failures", "0");
			suite.SetAttributeValue ("tests", "1");
			suite.SetAttributeValue ("timestamp", timestamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
			suite.SetAttributeValue ("hostname", "localhost");
			// suite.SetAttributeValue ("time", "0");
			root.Add (suite);

			var properties = new XElement ("properties");
			suite.Add (properties);

			if (!node.HasChildren || node.Children.Count == 0) {
				var test = new XElement ("testcase");
				test.SetAttributeValue ("name", node.Name.LocalName);
				test.SetAttributeValue ("status", node.Status);
				suite.Add (test);
			}

			var systemOut = new XElement ("system-out");
			suite.Add (systemOut);

			var systemErr = new XElement ("system-err");
			suite.Add (systemErr);

			if (node.Path != null) {
				var serializedPath = node.Path.SerializePath ().ToString ();
				systemOut.Add (serializedPath);
				systemOut.Add (Environment.NewLine);
				systemOut.Add (Environment.NewLine);
			}

			systemErr.Add (string.Format ("TEST: {0} {1} {2}", node.HasLogEntries, node.HasMessages, node.Name.HasParameters));

			if (node.Name.HasParameters) {
				foreach (var parameter in node.Name.Parameters) {
					var propNode = new XElement ("property");
					propNode.SetAttributeValue ("name", parameter.Name);
					propNode.SetAttributeValue ("value", parameter.Value);
					systemOut.Add (string.Format ("{0} = {1}{2}", parameter.Name, parameter.Value, Environment.NewLine));
					properties.Add (propNode);
				}
			}

			systemOut.Add (Environment.NewLine);

			if (node.HasMessages) {
				foreach (var message in node.Messages) {
					systemOut.Add (message + Environment.NewLine);
				}
			}

			return suite;
		}

		string FormatName (TestName name)
		{
			return name.FullName;
		}

		void Visit (XElement root, TestName parent, TestResult result)
		{
			if (false && result.Status == TestStatus.Ignored)
				return;

			var path = (IPathNode)result.Path;
			if (false && path != null)
				System.Console.WriteLine ("TEST: {0} - {1} {2} {3} - {4}", path.GetType ().FullName, path.Identifier, path.Name, path.ParameterType,
				                          result.Name.HasParameters);

			XElement node = root;

			if (result.Path.Identifier == "suite") {
				;
			} else {
				node = Print (root, parent, result);
			}

			System.Console.WriteLine ("VISIT: {0} {1} - {2} {3} - {4}", result.Name.FullName, result.Status, result.HasLogEntries, result.HasMessages, result.Name.HasParameters);
			if (result.HasChildren) {
				foreach (var child in result.Children)
					Visit (node, result.Name, child);
				return;
			}

			if (result.Status == TestStatus.Success)
				return;
			else if (result.Status == TestStatus.Ignored && !ShowIgnored)
				return;

			// Writer.WriteLine ("{0}) {1}: {2}", ++current, FormatName (node.Name), node.Status);

			if (result.Status == TestStatus.Error && result.HasErrors) {
				foreach (var error in result.Errors) {
					// Writer.WriteLine ();
					// Writer.WriteLine (error);
				}
			}

			// Writer.WriteLine ();
		}
	}
}

