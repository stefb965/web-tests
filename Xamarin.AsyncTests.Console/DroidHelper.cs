﻿//
// DroidHelper.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc.
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Console
{
	class DroidHelper
	{
		public Program Program {
			get;
		}

		public string SdkRoot {
			get;
		}

		public string Adb {
			get;
		}

		public string AndroidTool {
			get;
		}

		public string EmulatorTool {
			get;
		}

		public DroidDevice Device {
			get;
		}

		public DroidHelper (Program program, string sdkRoot)
		{
			Program = program;
			SdkRoot = sdkRoot;

			if (String.IsNullOrEmpty (SdkRoot))
				SdkRoot = Environment.GetEnvironmentVariable ("ANDROID_SDK_PATH");
			if (String.IsNullOrEmpty (SdkRoot)) {
				var home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				SdkRoot = Path.Combine (home, "Library", "Developer", "Xamarin", "android-sdk-macosx");
			}

			Adb = Path.Combine (SdkRoot, "platform-tools", "adb");
			AndroidTool = Path.Combine (SdkRoot, "tools", "android");
			EmulatorTool = Path.Combine (SdkRoot, "tools", "emulator");

			Device = new DroidDevice ("XamarinWebTests", "android-23", "x86", "Galaxy Nexus");
		}

		public async Task<bool> CheckAvd (CancellationToken cancellationToken)
		{
			var avds = await GetAvds (cancellationToken);
			Program.Debug ("Available Adbs: {0}", string.Join (" ", avds));

			if (!avds.Contains (Device.Name))
				await CreateAvd (true, cancellationToken);
			return true;
		}

		static string[] SplitOutputLines (string output)
		{
			var list = new List<string> ();
			using (var reader = new StringReader (output)) {
				string line;
				while ((line = reader.ReadLine ()) != null)
					list.Add (line);
			}
			return list.ToArray ();
		}

		internal async Task<string[]> GetAvds (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var output = await RunCommandWithOutput (AndroidTool, "list avd -c", cancellationToken);
			var avds = SplitOutputLines (output);

			foreach (var avd in avds) {
				Program.Debug ("AVD: {0}", avd);
			}

			return avds;
		}

		internal Task CreateAvd (bool replace, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var args = new StringBuilder ();
			args.Append ("-v ");
			args.AppendFormat ("create avd -n {0} -t {1}", Device.Name, Device.Target);
			if (!string.IsNullOrEmpty (Device.Abi))
				args.AppendFormat (" --abi {0}", Device.Abi);
			if (!string.IsNullOrEmpty (Device.Device))
				args.AppendFormat (" --device \"{0}\"", Device.Device);
			if (replace)
				args.Append ("  --force");

			return RunCommand (AndroidTool, args.ToString (), cancellationToken);
		}

		public async Task<bool> CheckEmulator (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var running = await CheckEmulatorRunning (cancellationToken);
			if (running)
				return true;

			await StartEmulator ("XamarinWebTests", "emulator.log", cancellationToken);

			return await WaitForEmulator (cancellationToken);
		}

		internal async Task<bool> CheckEmulatorRunning (CancellationToken cancellationToken)
		{
			Program.Debug ("Checking for running emulator");
			var output = await RunCommandWithOutput (Adb, "devices", cancellationToken);
			var re = new Regex ("(emulator-\\d+)\\s+(device|offline)");
			foreach (var line in SplitOutputLines (output)) {
				// Program.Debug ("ADB DEVICES: {0}", line);
				var match = re.Match (line);
				// Program.Debug ("DO WE HAVE IT: {0}", match.Success);
				if (!match.Success)
					continue;

				// Program.Debug ("TEST: |{0}|{1}|", match.Groups [1].Value, match.Groups [2].Value);

				if (match.Groups [2].Value.Equals ("offline")) {
					Program.Debug ("Emulator is offline.");
					continue;
				} else if (!match.Groups [2].Value.Equals ("device")) {
					continue;
				}

				var id = match.Groups [1].Value;

				var getPropArgs = string.Format ("-s {0} shell getprop sys.boot_completed", id);
				var getProp = await RunCommandWithOutput (Adb, getPropArgs, cancellationToken);
				getProp = getProp.Trim ();

				if (getProp.Equals ("1")) {
					Program.Debug ("Emulator completed booting.");
					return true;
				} else {
					Program.Debug ("Emulator still booting ...");
				}
			}

			return false;
		}

		internal async Task<bool> WaitForEmulator (CancellationToken cancellationToken)
		{
			bool running;
			var endtime = DateTime.Now + TimeSpan.FromMinutes (5);
			Program.Debug ("Started to wait for emulator at {0} (will abort at {1}).", DateTime.Now, endtime);
			do {
				await Task.Delay (5000);
				cancellationToken.ThrowIfCancellationRequested ();

				running = await CheckEmulatorRunning (cancellationToken);
			} while (!running && DateTime.Now < endtime);
			Program.Debug ("Finished waiting for emulator at {0}: {1}", DateTime.Now, running ? "SUCCESS" : "FAILURE");
			return running;
		}

		internal Task StartEmulator (string name, string logfile, CancellationToken cancellationToken)
		{
			var args = string.Format ("@{0} > {1} 2>&1 &", name, logfile);
			var shellArgs = string.Format ("-c \"{0} {1}\"", EmulatorTool, args);

			return RunCommand ("/bin/sh", shellArgs, cancellationToken);
		}

		internal async Task<bool> InstallApk (string apk, CancellationToken cancellationToken)
		{
			var args = string.Format ("install -r -d {0}", apk);
			await RunCommand (Adb, args, cancellationToken).ConfigureAwait (false);
			return true;
		}

		Task RunCommand (string command, string args, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<object> ();
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

			Task.Run (() => {
				var tool = string.Join (" ", command, args);
				try {
					var psi = new ProcessStartInfo (command, args);
					psi.UseShellExecute = false;
					psi.RedirectStandardInput = true;

					Program.Debug ("Running tool: {0}", tool);

					var process = Process.Start (psi);

					cts.Token.Register (() => {
						try {
							process.Kill ();
						} catch {
							;
						}
					});

					process.WaitForExit ();

					if (process.ExitCode != 0) {
						var message = string.Format ("External tool failed with exit code {0}.", process.ExitCode);
						tcs.TrySetException (new ExternalToolException (tool, message));
					} else {
						tcs.TrySetResult (null);
					}
				} catch (Exception ex) {
					tcs.TrySetException (new ExternalToolException (tool, ex));
				} finally {
					cts.Dispose ();
				}
			});

			return tcs.Task;
		}

		Task<string> RunCommandWithOutput (string command, string args, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<string> ();
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

			Task.Run (() => {
				var tool = string.Join (" ", command, args);
				try {
					var psi = new ProcessStartInfo (command, args);
					psi.UseShellExecute = false;
					psi.RedirectStandardInput = true;
					psi.RedirectStandardOutput = true;
					psi.RedirectStandardError = true;

					Program.Debug ("Running tool: {0}", tool);

					var process = Process.Start (psi);

					cts.Token.Register (() => {
						try {
							process.Kill ();
						} catch {
							;
						}
					});

					var stdoutTask = process.StandardOutput.ReadToEndAsync ();
					var stderrTask = process.StandardError.ReadToEndAsync ();

					process.WaitForExit ();

					var stdout = stdoutTask.Result;
					var stderr = stderrTask.Result;

					if (process.ExitCode != 0)
						tcs.TrySetException (new ExternalToolException (tool, stderr));
					else
						tcs.TrySetResult (stdout);
				} catch (Exception ex) {
					tcs.TrySetException (new ExternalToolException (tool, ex));
				} finally {
					cts.Dispose ();
				}
			});

			return tcs.Task;
		}


	}
}

