//
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
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Console
{
	class DroidHelper
	{
		public string SdkRoot {
			get;
			private set;
		}

		public string Adb {
			get;
			private set;
		}

		public string AndroidTool {
			get;
			private set;
		}

		public DroidHelper (string sdkRoot)
		{
			SdkRoot = sdkRoot;

			Adb = Path.Combine (SdkRoot, "platform-tools", "adb");
			AndroidTool = Path.Combine (SdkRoot, "tools", "android");
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

			var output = await RunCommandWithOutput (AndroidTool, cancellationToken, "list", "avd", "-c");
			var avds = SplitOutputLines (output);

			foreach (var avd in avds) {
				Program.Debug ("AVD: {0}", avd);
			}

			return avds;
		}

		internal async Task CreateAvd (string name, string target, string abi, string device, bool replace, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var args = new StringBuilder ();
			args.Append ("-v ");
			args.AppendFormat ("create avd -n {0} -t {1}", name, target);
			if (!string.IsNullOrEmpty (abi))
				args.AppendFormat (" --abi {0}", abi);
			if (!string.IsNullOrEmpty (device))
				args.AppendFormat (" --device \"{0}\"", device);
			if (replace)
				args.Append ("  --force");

			var output = await RunCommand (AndroidTool, cancellationToken, args.ToString ());
			Program.Debug ("CREATE AVD: {0}", output);
		}

		Task<bool> RunCommand (string command, CancellationToken cancellationToken, string args)
		{
			var tcs = new TaskCompletionSource<bool> ();
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

					tcs.TrySetResult (process.ExitCode == 0);
				} catch (Exception ex) {
					tcs.TrySetException (new ExternalToolException (tool, ex));
				} finally {
					cts.Dispose ();
				}
			});

			return tcs.Task;
		}

		Task<string> RunCommandWithOutput (string command, CancellationToken cancellationToken, params string[] args)
		{
			var tcs = new TaskCompletionSource<string> ();
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

			Task.Run (() => {
				var joinedArgs = string.Join (" ", args);
				var tool = string.Join (" ", command, joinedArgs);
				try {
					var psi = new ProcessStartInfo (command, joinedArgs);
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

