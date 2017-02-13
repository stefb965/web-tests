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
			Visit (root, Result);
			return true;

			var suite = new XElement ("testsuite");
			suite.SetAttributeValue ("id", 1);
			suite.SetAttributeValue ("package", Result.Name.FullName);
			suite.SetAttributeValue ("name", Result.Name.LocalName);
			suite.SetAttributeValue ("errors", "0");
			suite.SetAttributeValue ("failures", "0");
			suite.SetAttributeValue ("tests", "1");
			suite.SetAttributeValue ("timestamp", timestamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
			suite.SetAttributeValue ("hostname", "localhost");
			suite.SetAttributeValue ("time", "0");
			root.Add (suite);

			var properties = new XElement ("properties");
			suite.Add (properties);

			var tcase = new XElement ("testcase");
			tcase.SetAttributeValue ("classname", "martin.test");
			tcase.SetAttributeValue ("name", "Test1");
			tcase.SetAttributeValue ("time", "123.345000");
			suite.Add (tcase);

			var systemOut = new XElement ("system-out");
			systemOut.Add (new XText ("Hello World!"));
			suite.Add (systemOut);

			var systemErr = new XElement ("system-err");
			// systemErr.Add ("Test Error");
			suite.Add (systemErr);

			// Writer.WriteLine ();
			// Writer.WriteLine ("Test result: {0} - {1}", Result.Name.FullName, Result.Status);
			// Writer.WriteLine ();

			if (Result.Status == TestStatus.Success)
				return true;

			Visit (root, Result);
			return false;
		}

		void Print (XElement root, TestResult node)
		{
			var timestamp = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);
			var suite = new XElement ("testsuite");
			suite.SetAttributeValue ("id", node.Name.ID);
			suite.SetAttributeValue ("package", node.Name.FullName);
			suite.SetAttributeValue ("name", node.Name.LocalName);
			suite.SetAttributeValue ("errors", "0");
			suite.SetAttributeValue ("failures", "0");
			suite.SetAttributeValue ("tests", "1");
			suite.SetAttributeValue ("timestamp", timestamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
			suite.SetAttributeValue ("hostname", "localhost");
			suite.SetAttributeValue ("time", "0");
			root.Add (suite);

			var properties = new XElement ("properties");
			suite.Add (properties);

			if (node.Name.HasParameters) {
				foreach (var parameter in node.Name.Parameters) {
					var propNode = new XElement ("property");
					propNode.SetAttributeValue ("name", parameter.Name);
					propNode.SetAttributeValue ("value", parameter.Value);
					properties.Add (propNode);
				}
			}

			var test = new XElement ("testcase");
			test.SetAttributeValue ("classname", node.Name.FullName);
			test.SetAttributeValue ("name", node.Name.LocalName);
			test.SetAttributeValue ("time", "0");
			suite.Add (test);

			var systemOut = new XElement ("system-out");
			suite.Add (systemOut);

			var systemErr = new XElement ("system-err");
			suite.Add (systemErr);

			systemErr.Add (string.Format ("TEST: {0} {1}", node.HasLogEntries, node.HasMessages));

			if (node.HasMessages) {
				foreach (var message in node.Messages) {
					systemOut.Add (message + Environment.NewLine);
				}
			}

			if (false && node.HasLogEntries) {
				foreach (var entry in node.LogEntries) {
					switch (entry.Kind) {
					case TestLoggerBackend.EntryKind.Error:
						break;
					default:
						break;
					}
					if (!string.IsNullOrEmpty (entry.Text))
						systemOut.Add (entry.Text);
				}
			}

			// systemOut.Add (new XText ("Hello World!"));

			// Visit (root, node);

			// systemErr.Add ("Test Error");
		}

		string FormatName (TestName name)
		{
			return name.FullName;
		}

		void Visit (XElement root, TestResult node)
		{
			if (false && node.Status == TestStatus.Ignored)
				return;

			var path = (IPathNode)node.Path;
			if (false && path != null)
				System.Console.WriteLine ("TEST: {0} - {1} {2} {3} - {4}", path.GetType ().FullName, path.Identifier, path.Name, path.ParameterType,
				                          node.Name.HasParameters);

			Print (root, node);

			System.Console.WriteLine ("VISIT: {0} {1} - {2} {3} - {4}", node.Name.FullName, node.Status, node.HasLogEntries, node.HasMessages, node.Name.HasParameters);
			if (node.HasChildren) {
				foreach (var child in node.Children)
					Visit (root, child);
				return;
			}

			if (node.Status == TestStatus.Success)
				return;
			else if (node.Status == TestStatus.Ignored && !ShowIgnored)
				return;

			// Writer.WriteLine ("{0}) {1}: {2}", ++current, FormatName (node.Name), node.Status);

			if (node.Status == TestStatus.Error && node.HasErrors) {
				foreach (var error in node.Errors) {
					// Writer.WriteLine ();
					// Writer.WriteLine (error);
				}
			}

			// Writer.WriteLine ();
		}
	}
}
