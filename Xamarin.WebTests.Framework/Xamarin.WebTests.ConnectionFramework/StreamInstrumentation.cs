//
// StreamInstrumentation.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class StreamInstrumentation : NetworkStream
	{
		public TestContext Context {
			get;
		}

		public StreamInstrumentation (TestContext ctx, Socket socket)
			: base (socket, true)
		{
			Context = ctx;
		}

		MyAction writeAction;
		MyAction readAction;

		public void OnNextBeginRead (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextBeginWrite (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextWrite (Func<Task> before, Func<Task> after)
		{
			var myAction = new MyAction (before, after);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextWrite (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextRead (Func<Task> before, Func<Task> after)
		{
			var myAction = new MyAction (before, after);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextRead (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextRead (AsyncReadHandler handler)
		{
			var myAction = new MyAction (handler);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			Context.LogDebug (4, "StreamInstrumentation.BeginWrite({0},{1})", offset, size);

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action == null)
				return base.BeginWrite (buffer, offset, size, callback, state);

			if (action.Action != null) {
				action.Action ();
				return base.BeginWrite (buffer, offset, size, callback, state);
			}

			var myResult = new MyAsyncResult (action, callback, state);
			var transportResult = base.BeginWrite (buffer, offset, size, WriteCallback, myResult);

			if (transportResult.CompletedSynchronously)
				Task.Factory.StartNew (() => WriteCallback (transportResult));

			return myResult;
		}

		void WriteCallback (IAsyncResult transportResult)
		{
			var myResult = (MyAsyncResult)transportResult.AsyncState;

			try {
				myResult.Action.InvokeBefore ();
				base.EndWrite (transportResult);
				myResult.Action.InvokeAfter ();
				myResult.SetCompleted (false);
			} catch (Exception ex) {
				myResult.SetCompleted (false, ex);
			}
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			var myResult = asyncResult as MyAsyncResult;
			if (myResult == null) {
				base.EndWrite (asyncResult);
				return;
			}

			myResult.WaitUntilComplete ();
			if (myResult.GotException)
				throw myResult.Exception;
		}

		public delegate Task<int> AsyncReadFunc (byte[] buffer, int offset, int count, CancellationToken cancellationToken);
		public delegate Task<int> AsyncReadHandler (byte[] buffer, int offset, int count, AsyncReadFunc func, CancellationToken cancellationToken);
		delegate int SyncReadFunc (byte[] buffer, int offset, int count);

		internal Task<int> BaseReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return base.ReadAsync (buffer, offset, count, cancellationToken);
		}

		public override Task<int> ReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var message = string.Format ("StreamInstrumentation.ReadAsync({0},{1})", offset, count);

			AsyncReadFunc asyncBaseRead = base.ReadAsync;
			AsyncReadHandler asyncReadHandler = (b, o, c, func, ct) => func (b, o, c, ct);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead != null) {
				message += " - action";
				return ReadAsync (buffer, offset, count, message, asyncBaseRead, action.AsyncRead, cancellationToken);
			}

			return ReadAsync (buffer, offset, count, message, asyncBaseRead, asyncReadHandler, cancellationToken);
		}

		async Task<int> ReadAsync (byte[] buffer, int offset, int count, string message,
		                           AsyncReadFunc func, AsyncReadHandler handler, CancellationToken cancellationToken)
		{
			Context.LogDebug (4, message);
			try {
				var ret = await handler (buffer, offset, count, func, cancellationToken).ConfigureAwait (false);
				Context.LogDebug (4, "{0} done: {1}", message, ret);
				return ret;
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var message = string.Format ("StreamInstrumentation.BeginRead({0},{1})", offset, size);
			Context.LogDebug (4, message);

			AsyncReadFunc asyncBaseRead = (b, o, s, _) => Task.Factory.FromAsync (
				(ca, st) => base.BeginRead (b, o, s, ca, st),
				(result) => base.EndRead (result), null);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead == null)
				return base.BeginRead (buffer, offset, size, callback, state);

			message += " - action";

			AsyncReadFunc readFunc = (b, o, s, ct) => action.AsyncRead (b, o, s, asyncBaseRead, ct);
			try {
				Context.LogDebug (4, message);
				var readTask = readFunc (buffer, offset, size, CancellationToken.None);
				Context.LogDebug (4, "{0} got task: {1}", message, readTask.Status);

				var myResult = new MyAsyncResult (action, message, readTask, callback, state);
				return myResult;
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		void ReadCallback (IAsyncResult transportResult)
		{
			var myResult = (MyAsyncResult)transportResult.AsyncState;

			try {
				myResult.Action.InvokeBefore ();
				myResult.Result = base.EndRead (transportResult);
				myResult.Action.InvokeAfter ();
				myResult.SetCompleted (false);
			} catch (Exception ex) {
				myResult.SetCompleted (false, ex);
			}
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			var myResult = asyncResult as MyAsyncResult;
			if (myResult == null)
				return base.EndRead (asyncResult);

			if (myResult.Task != null) {
				Context.LogDebug (4, "{0} end read: {1}", myResult.Message, myResult.Task.Status);
				try {
					myResult.Task.Wait ();
					Context.LogDebug (4, "{0} end read done: {1}", myResult.Message, myResult.Task.Result);
					return myResult.Task.Result;
				} catch (Exception ex) {
					Context.LogDebug (4, "{0} end read failed: {1}", myResult.Message, ex);
					throw;
				}
			}

			myResult.WaitUntilComplete ();
			if (myResult.GotException)
				throw myResult.Exception;

			return myResult.Result;
		}

		public override void Write (byte[] buffer, int offset, int size)
		{
			Context.LogDebug (4, "StreamInstrumentation.Write({0},{1})", offset, size);

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action == null) {
				Write_internal (buffer, offset, size);
				return;
			}

			if (action.Action != null) {
				try {
					Context.LogDebug (4, "StreamInstrumentation.Write({0},{1}) - action", offset, size);
					action.Action ();
					Context.LogDebug (4, "StreamInstrumentation.Write({0},{1}) - action done", offset, size);
				} catch (Exception ex) {
					Context.LogDebug (4, "StreamInstrumentation.Write({0},{1}) - action failed: {2}", offset, size, ex);
					throw;
				}
			}

			Write_internal (buffer, offset, size);
		}

		void Write_internal (byte[] buffer, int offset, int size)
		{
			try {
				base.Write (buffer, offset, size);
				Context.LogDebug (4, "StreamInstrumentation.Write({0},{1}) done", offset, size);
			} catch (Exception ex) {
				Context.LogDebug (4, "StreamInstrumentation.Write({0},{1}) failed: {0}", offset, size, ex);
				throw;
			}
		}

		public override int Read (byte[] buffer, int offset, int size)
		{
			var message = string.Format ("StreamInstrumentation.Read({0},{1})", offset, size);

			SyncReadFunc syncRead = (b, o, s) => base.Read (b, o, s);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead != null) {
				message += " - action";

				AsyncReadFunc asyncBaseRead = (b, o, s, _) => Task.Factory.FromAsync (
					(callback, state) => syncRead.BeginInvoke (b, o, s, callback, state),
					(result) => syncRead.EndInvoke (result), null);

				syncRead = (b, o, s) => action.AsyncRead (b, o, s, asyncBaseRead, CancellationToken.None).Result;
			}

			return Read_internal (buffer, offset, size, message, syncRead);
		}

		int Read_internal (byte[] buffer, int offset, int size, string message, SyncReadFunc func)
		{
			Context.LogDebug (4, message);
			try {
				int ret = func (buffer, offset, size);
				Context.LogDebug (4, "{0} done: {1}", ret);
				return ret;
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", ex);
				throw;
			}
		}

		class MyAction
		{
			public readonly Action Action;
			public readonly AsyncReadHandler AsyncRead;
			public readonly Func<Task> Before;
			public readonly Func<Task> After;

			public MyAction (Action action)
			{
				Action = action;
			}

			public MyAction (AsyncReadHandler handler)
			{
				AsyncRead = handler;
			}

			public MyAction (Func<Task> before, Func<Task> after)
			{
				Before = before;
				After = after;
			}

			public void InvokeBefore ()
			{
				if (Before != null) {
					var task = Before ();
					if (task != null)
						task.Wait ();
				}
			}

			public void InvokeAfter ()
			{
				if (After != null) {
					var task = After ();
					if (task != null)
						task.Wait ();
				}
			}
		}

		class MyAsyncResult : SimpleAsyncResult
		{
			public readonly MyAction Action;
			public readonly string Message;
			public readonly Task<int> Task;
			public int Result;

			internal MyAsyncResult (MyAction action, AsyncCallback callback, object state)
				: base (callback, state)
			{
				Action = action;
			}

			internal MyAsyncResult (MyAction action, string message, Task<int> task, AsyncCallback callback, object state)
				: this (action, callback, state)
			{
				Message = message;
				Task = task;
			}
		}
	}
}

