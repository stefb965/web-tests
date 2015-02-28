﻿//
// ParameterSerializer.cs
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
using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	static class Serializer
	{
		public static readonly IValueSerializer<string> String = new StringSerializer ();
		public static readonly IValueSerializer<XElement> Element = new ElementSerializer ();
		public static readonly IValueSerializer<TestName> TestName = new TestNameSerializer ();
		public static readonly Serializer<TestLoggerBackend> TestLogger = new TestLoggerSerializer ();
		public static readonly IValueSerializer<TestResult> TestResult = new TestResultSerializer ();
		public static readonly Serializer<Handshake> Handshake = new HandshakeSerializer ();
		public static readonly IValueSerializer<TestLoggerBackend.LogEntry> LogEntry = new LogEntrySerializer ();
		public static readonly IValueSerializer<TestLoggerBackend.StatisticsEventArgs> StatisticsEventArgs = new StatisticsEventArgsSerializer ();

		public static XElement Write (object instance)
		{
			if (instance is string)
				return String.Write ((string)instance);
			else if (instance is XElement)
				return Element.Write ((XElement)instance);
			else if (instance is TestName)
				return TestName.Write ((TestName)instance);
			else if (instance is TestResult)
				return TestResult.Write ((TestResult)instance);
			else if (instance is TestLoggerBackend.LogEntry)
				return LogEntry.Write ((TestLoggerBackend.LogEntry)instance);
			else if (instance is TestLoggerBackend.StatisticsEventArgs)
				return StatisticsEventArgs.Write ((TestLoggerBackend.StatisticsEventArgs)instance);
			else if (instance is ObjectProxy)
				return RemoteObjectManager.WriteProxy ((ObjectProxy)instance);
			else
				throw new ServerErrorException ();
		}

		public static T Read<T> (XElement node)
		{
			if (typeof(T) == typeof(string))
				return ((IValueSerializer<T>)String).Read (node);
			else if (typeof(T) == typeof(XElement))
				return ((IValueSerializer<T>)Element).Read (node);
			else if (typeof(T) == typeof(TestName))
				return ((IValueSerializer<T>)TestName).Read (node);
			else if (typeof(T) == typeof(TestResult))
				return ((IValueSerializer<T>)TestResult).Read (node);
			else if (typeof(T) == typeof(TestLoggerBackend.LogEntry))
				return ((IValueSerializer<T>)LogEntry).Read (node);
			else if (typeof(T) == typeof(TestLoggerBackend.StatisticsEventArgs))
				return ((IValueSerializer<T>)StatisticsEventArgs).Read (node);
			else
				throw new ServerErrorException ();
		}

		static readonly SettingsSerializer settingsSerializer = new SettingsSerializer ();

		public static IValueSerializer<SettingsBag> Settings {
			get { return settingsSerializer; }
		}

		internal static SettingsBag ReadSettings (XElement node)
		{
			return settingsSerializer.Read (node);
		}

		internal static XElement WriteSettings (SettingsBag settings)
		{
			return settingsSerializer.Write (settings);
		}

		class SettingsSerializer : IValueSerializer<SettingsBag>
		{
			public XElement Write (SettingsBag instance)
			{
				var settings = new XElement ("Settings");

				foreach (var entry in instance.Settings) {
					var item = new XElement ("Setting");
					settings.Add (item);

					item.SetAttributeValue ("Key", entry.Key);
					item.SetAttributeValue ("Value", entry.Value);
				}

				return settings;
			}

			public SettingsBag Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("Settings"))
					throw new ServerErrorException ();

				var settings = SettingsBag.CreateDefault ();
				foreach (var element in node.Elements ("Setting")) {
					var key = element.Attribute ("Key");
					var value = element.Attribute ("Value");
					settings.Add (key.Value, value.Value);
				}
				return settings;
			}
		}

		class StringSerializer : IValueSerializer<string>
		{
			public string Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("Text"))
					throw new ServerErrorException ();

				return node.Attribute ("Value").Value;
			}

			public XElement Write (string instance)
			{
				var element = new XElement ("Text");
				element.SetAttributeValue ("Value", instance);
				return element;
			}
		}

		class ElementSerializer : IValueSerializer<XElement>
		{
			public XElement Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("Element"))
					throw new ServerErrorException ();

				return (XElement)node.FirstNode;
			}

			public XElement Write (XElement instance)
			{
				return new XElement ("Element", instance);
			}
		}

		class TestNameSerializer : IValueSerializer<TestName>
		{
			public TestName Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestName"))
					throw new ServerErrorException ();

				var builder = new TestNameBuilder ();
				var nameAttr = node.Attribute ("Name");
				if (nameAttr != null)
					builder.PushName (nameAttr.Value);

				foreach (var child in node.Elements ("Parameter")) {
					var name = child.Attribute ("Name").Value;
					var value = child.Attribute ("Value").Value;
					var isHidden = bool.Parse (child.Attribute ("IsHidden").Value);
					builder.PushParameter (name, value, isHidden);
				}

				return builder.GetName ();
			}

			public XElement Write (TestName instance)
			{
				var element = new XElement ("TestName");
				if (instance.Name != null)
					element.SetAttributeValue ("Name", instance.Name);

				if (instance.HasParameters) {
					foreach (var parameter in instance.Parameters) {
						var node = new XElement ("Parameter");
						element.Add (node);

						node.SetAttributeValue ("Name", parameter.Name);
						node.SetAttributeValue ("Value", parameter.Value);
						node.SetAttributeValue ("IsHidden", parameter.IsHidden.ToString ());
					}
				}

				return element;
			}
		}

		class TestLoggerSerializer : Serializer<TestLoggerBackend>
		{
			public TestLoggerBackend Read (Connection connection, object sender, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestLogger"))
					throw new ServerErrorException ();

				var objectId = long.Parse (node.Attribute ("ObjectID").Value);

				var proxy = new TestLoggerClient (connection, objectId);
				return proxy;
			}

			public XElement Write (Connection connection, object sender, TestLoggerBackend instance)
			{
				var element = new XElement ("TestLogger");
				var proxy = new TestLoggerServant ((ClientConnection)connection);
				element.SetAttributeValue ("ObjectID", proxy.ObjectID);

				return element;
			}
		}

		class TestResultSerializer : IValueSerializer<TestResult>
		{
			public TestResult Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestResult"))
					throw new ServerErrorException ();

				var name = Serializer.TestName.Read (node.Element ("TestName"));
				var status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);

				var result = new TestResult (name, status);

				var error = node.Attribute ("Error");
				if (error != null)
					result.AddError (new SavedException (error.Value));

				foreach (var child in node.Elements ("TestResult")) {
					result.AddChild (Read (child));
				}

				return result;
			}

			public XElement Write (TestResult instance)
			{
				var element = new XElement ("TestResult");
				element.SetAttributeValue ("Status", instance.Status.ToString ());

				if (instance.Error != null)
					element.SetAttributeValue ("Error", instance.Error.ToString ());

				element.Add (Serializer.TestName.Write (instance.Name));

				foreach (var child in instance.Children)
					element.Add (Write (child));

				return element;
			}
		}

		class StatisticsEventArgsSerializer : IValueSerializer<TestLoggerBackend.StatisticsEventArgs>
		{
			public TestLoggerBackend.StatisticsEventArgs Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestStatisticsEventArgs"))
					throw new ServerErrorException ();

				var instance = new TestLoggerBackend.StatisticsEventArgs ();
				instance.Type = (TestLoggerBackend.StatisticsEventType)Enum.Parse (typeof(TestLoggerBackend.StatisticsEventType), node.Attribute ("Type").Value);
				instance.Status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);
				instance.IsRemote = true;

				var name = node.Element ("TestName");
				if (name != null)
					instance.Name = Serializer.TestName.Read (name);

				return instance;
			}

			public XElement Write (TestLoggerBackend.StatisticsEventArgs instance)
			{
				if (instance.IsRemote)
					throw new ServerErrorException ();

				var element = new XElement ("TestStatisticsEventArgs");
				element.SetAttributeValue ("Type", instance.Type.ToString ());
				element.SetAttributeValue ("Status", instance.Status.ToString ());

				if (instance.Name != null)
					element.Add (Serializer.TestName.Write (instance.Name));

				return element;
			}
		}

		class HandshakeSerializer : Serializer<Handshake>
		{
			public Handshake Read (Connection connection, object sender, XElement node)
			{
				if (!node.Name.LocalName.Equals ("HandshakeRequest"))
					throw new ServerErrorException ();

				var instance = new Handshake ();
				instance.WantStatisticsEvents = bool.Parse (node.Attribute ("WantStatisticsEvents").Value);

				var settings = node.Element ("Settings");
				if (settings != null)
					instance.Settings = Serializer.Settings.Read (settings);

				var logger = node.Element ("TestLogger");
				if (logger != null)
					instance.Logger = Serializer.TestLogger.Read (connection, sender, logger);

				return instance;
			}

			public XElement Write (Connection connection, object sender, Handshake instance)
			{
				var element = new XElement ("HandshakeRequest");
				element.SetAttributeValue ("WantStatisticsEvents", instance.WantStatisticsEvents);

				if (instance.Settings != null)
					element.Add (Serializer.Settings.Write (instance.Settings));

				if (instance.Logger != null)
					element.Add (Serializer.TestLogger.Write (connection, sender, instance.Logger));

				return element;
			}
		}

		class LogEntrySerializer : IValueSerializer<TestLoggerBackend.LogEntry>
		{
			public TestLoggerBackend.LogEntry Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("LogEntry"))
					throw new ServerErrorException ();

				TestLoggerBackend.EntryKind kind;
				if (!Enum.TryParse (node.Attribute ("Kind").Value, out kind))
					throw new ServerErrorException ();

				var text = node.Attribute ("Text").Value;
				var logLevel = int.Parse (node.Attribute ("LogLevel").Value);

				Exception exception = null;
				var error = node.Element ("Error");
				if (error != null) {
					var errorMessage = error.Attribute ("Text").Value;
					var stackTrace = error.Attribute ("StackTrace").Value;
					exception = new SavedException (errorMessage, stackTrace);
				}

				var instance = new TestLoggerBackend.LogEntry (kind, logLevel, text, exception);
				return instance;
			}

			public XElement Write (TestLoggerBackend.LogEntry instance)
			{
				var element = new XElement ("LogEntry");
				element.SetAttributeValue ("Kind", instance.Kind);
				element.SetAttributeValue ("LogLevel", instance.LogLevel);
				element.SetAttributeValue ("Text", instance.Text);

				if (instance.Error != null) {
					var error = new XElement ("Error");
					error.SetAttributeValue ("Text", instance.Error.Message);
					error.SetAttributeValue ("StackTrace", instance.Error.StackTrace);
					element.Add (error);
				}

				return element;
			}
		}

		internal class PathWrapper : ITestPath
		{
			readonly XElement node;

			public PathWrapper (XElement node)
			{
				this.node = node;
			}

			public XElement Serialize ()
			{
				return node;
			}
		}
	}

	interface IValueSerializer<T>
	{
		T Read (XElement node);

		XElement Write (T instance);
	}

	interface Serializer<T>
	{
		T Read (Connection connection, object sender, XElement node);

		XElement Write (Connection connection, object sender, T instance);
	}
}

