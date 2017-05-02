//
// ProgramOptions.cs
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
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NDesk.Options;

namespace Xamarin.AsyncTests.Console {
	public class ProgramOptions {
		public Assembly Assembly {
			get;
		}

		public string Application {
			get;
		}

		internal Command Command {
			get;
		}

		public string SettingsFile {
			get;
			private set;
		}

		public string ResultOutput {
			get;
			private set;
		}

		public string JUnitResultOutput {
			get;
			private set;
		}

		public string PackageName {
			get;
			private set;
		}

		public IPEndPoint EndPoint {
			get;
			private set;
		}

		public IPEndPoint GuiEndPoint {
			get;
			private set;
		}

		public int? LogLevel {
			get;
			private set;
		}

		public int? LocalLogLevel {
			get;
			private set;
		}

		public bool Wait {
			get;
			private set;
		}

		public bool DebugMode {
			get;
			private set;
		}

		public bool Wrench {
			get;
			private set;
		}

		public bool Jenkins {
			get;
			private set;
		}

		public string OutputDirectory {
			get;
			private set;
		}

		public string IOSDeviceType {
			get;
			private set;
		}

		public string IOSRuntime {
			get;
			private set;
		}

		public string ExtraLauncherArgs {
			get;
			private set;
		}

		public string CustomSettings {
			get;
			private set;
		}

		public bool OptionalGui {
			get;
			private set;
		}

		public bool SaveOptions {
			get;
			private set;
		}

		public bool ShowCategories {
			get;
			private set;
		}

		public bool ShowFeatures {
			get;
			private set;
		}

		public bool ShowConfiguration {
			get;
			private set;
		}

		public string StdOut {
			get;
			private set;
		}

		public string StdErr {
			get;
			private set;
		}

		public string SdkRoot {
			get;
			private set;
		}

		public string Category {
			get;
			private set;
		}

		public string Features {
			get;
			private set;
		}

		public IList<string> Arguments {
			get;
			private set;
		}

		public IList<Assembly> Dependencies {
			get;
			private set;
		}

		public ProgramOptions (Assembly assembly, string[] args)
		{
			Assembly = assembly;

			var dependencies = new List<string> ();

			ResultOutput = "TestResult.xml";
			JUnitResultOutput = "JUnitTestResult.xml";

			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("endpoint=", v => EndPoint = Program.GetEndPoint (v));
			p.Add ("extra-launcher-args=", v => ExtraLauncherArgs = v);
			p.Add ("gui=", v => GuiEndPoint = Program.GetEndPoint (v));
			p.Add ("wait", v => Wait = true);
			p.Add ("no-result", v => ResultOutput = null);
			p.Add ("package-name=", v => PackageName = v);
			p.Add ("result=", v => ResultOutput = v);
			p.Add ("junit-result=", v => JUnitResultOutput = v);
			p.Add ("log-level=", v => LogLevel = int.Parse (v));
			p.Add ("local-log-level=", v => LocalLogLevel = int.Parse (v));
			p.Add ("dependency=", v => dependencies.Add (v));
			p.Add ("optional-gui", v => OptionalGui = true);
			p.Add ("set=", v => CustomSettings = v);
			p.Add ("category=", v => Category = v);
			p.Add ("features=", v => Features = v);
			p.Add ("debug", v => DebugMode = true);
			p.Add ("save-options", v => SaveOptions = true);
			p.Add ("show-categories", v => ShowCategories = true);
			p.Add ("show-features", v => ShowFeatures = true);
			p.Add ("show-config", v => ShowCategories = ShowFeatures = true);
			p.Add ("ios-device-type=", v => IOSDeviceType = v);
			p.Add ("ios-runtime=", v => IOSRuntime = v);
			p.Add ("stdout=", v => StdOut = v);
			p.Add ("stderr=", v => StdErr = v);
			p.Add ("sdkroot=", v => SdkRoot = v);
			p.Add ("wrench", v => Wrench = true);
			p.Add ("jenkins", v => Jenkins = true);
			p.Add ("output-dir=", v => OutputDirectory = v);
			var arguments = p.Parse (args);

			if (assembly != null) {
				Command = Command.Local;

				if (arguments.Count > 0 && arguments[0].Equals ("local"))
					arguments.RemoveAt (0);
			} else {
				if (arguments.Count < 1)
					throw new ProgramException ("Missing argument.");

				Command command;
				if (!Enum.TryParse (arguments[0], true, out command))
					throw new ProgramException ("Unknown command.");
				arguments.RemoveAt (0);
				Command = command;
			}

			Arguments = arguments;

			var dependencyAssemblies = new Assembly[dependencies.Count];
			for (int i = 0; i < dependencyAssemblies.Length; i++) {
				dependencyAssemblies[i] = Assembly.LoadFile (dependencies[i]);
			}

			Dependencies = dependencyAssemblies;

			switch (Command) {
			case Command.Listen:
				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Local:
				if (assembly != null) {
					if (arguments.Count != 0) {
						arguments.ForEach (a => Program.Error ("Unexpected remaining argument: {0}", a));
						throw new ProgramException ("Unexpected extra argument.");
					}
					Assembly = assembly;
				} else if (arguments.Count == 1) {
					Application = arguments[0];
					Assembly = Assembly.LoadFile (arguments[0]);
					arguments.RemoveAt (0);
				} else if (EndPoint == null) {
					throw new ProgramException ("Missing endpoint");
				}
				break;
			case Command.Connect:
				if (assembly != null)
					throw new ProgramException ("Cannot use 'connect' with assembly.");
				if (arguments.Count == 1) {
					EndPoint = Program.GetEndPoint (arguments[0]);
					arguments.RemoveAt (0);
				} else if (arguments.Count == 0) {
					if (EndPoint == null)
						throw new ProgramException ("Missing endpoint");
				} else {
					arguments.ForEach (a => Program.Error ("Unexpected remaining argument: {0}", a));
					throw new ProgramException ("Unexpected extra argument.");
				}
				break;
			case Command.Simulator:
			case Command.Device:
			case Command.TVOS:
				if (arguments.Count < 1)
					throw new ProgramException ("Expected .app argument");
				Application = arguments[0];
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Mac:
				if (arguments.Count < 1)
					throw new ProgramException ("Expected .app argument");
				Application = arguments[0];
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Android:
				if (arguments.Count < 1)
					throw new ProgramException ("Expected activity argument");

				Application = arguments[0];
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Avd:
			case Command.Emulator:
				if (arguments.Count != 0)
					throw new ProgramException ("Unexpected extra arguments");

				break;
			case Command.Apk:
				if (arguments.Count != 1)
					throw new ProgramException ("Expected .apk argument");

				break;
			case Command.Result:
				if (arguments.Count != 1)
					throw new ProgramException ("Expected TestResult.xml argument");
				ResultOutput = arguments[0];
				break;
			default:
				throw new ProgramException ("Unknown command '{0}'.", Command);
			}
		}
	}
}
