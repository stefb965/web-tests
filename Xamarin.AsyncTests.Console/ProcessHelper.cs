//
// ProcessHelper.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc.
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

namespace Xamarin.AsyncTests.Console {
	class ProcessHelper : IDisposable {
		Process process;
		string commandLine;
		CancellationTokenSource cts;
		TaskCompletionSource<object> tcs;

		public string CommandLine {
			get;
		}

		ProcessHelper (Process process, CancellationToken cancellationToken)
		{
			this.process = process;

			commandLine = process.StartInfo.FileName;
			if (!string.IsNullOrWhiteSpace (process.StartInfo.Arguments))
				commandLine += " " + process.StartInfo.Arguments;

			tcs = new TaskCompletionSource<object> ();

			cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => {
				try {
					process.Kill ();
				} catch {
					;
				}
			});

			process.EnableRaisingEvents = true;
			process.Exited += (sender, e) => {
				if (cts.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (process.ExitCode != 0) {
					var message = string.Format ("External tool failed with exit code {0}.", process.ExitCode);
					tcs.TrySetException (new ExternalToolException (commandLine, message));
				} else {
					tcs.TrySetResult (null);
				}
			};
		}

		public void Abort ()
		{
			cts.Cancel ();
		}

		public Task WaitForExit (CancellationToken cancellationToken)
		{
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				linkedCts.Token.Register (() => Abort ());
				return tcs.Task;
			}
		}

		public static Task<ProcessHelper> RunCommand (string command, string args, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var tool = string.Join (" ", command, args);
			var psi = new ProcessStartInfo (command, args);
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;

			Program.Debug ("Running tool: {0}", tool);

			return RunCommand (psi, cancellationToken);
		}

		public static Task<ProcessHelper> RunCommand (ProcessStartInfo psi, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			return Task.Run (() => {
				cancellationToken.ThrowIfCancellationRequested ();

				var process = Process.Start (psi);
				return new ProcessHelper (process, cancellationToken);
			});
		}

		public static Task<string> RunCommandWithOutput (string command, string args, CancellationToken cancellationToken)
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

		public void Dispose ()
		{
			if (process != null) {
				try {
					process.Kill ();
				} catch {
					;
				}
				process.Dispose ();
				process = null;
			}
			if (cts != null) {
				cts.Dispose ();
				cts = null;
			}
		}
	}
}
