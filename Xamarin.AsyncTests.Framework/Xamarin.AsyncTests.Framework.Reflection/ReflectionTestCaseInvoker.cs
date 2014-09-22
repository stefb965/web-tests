﻿//
// ReflectionTestCaseInvoker.cs
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestCaseInvoker : TestInvoker
	{
		public ReflectionTestCaseBuilder Builder {
			get;
			private set;
		}

		public ReflectionTestCaseInvoker (ReflectionTestCaseBuilder builder)
		{
			Builder = builder;
		}

		public override Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			// instance = MartinTest (ctx, instance);

			ctx.OnTestRunning ();

			if (Builder.ExpectedExceptionType != null)
				return ExpectingException (ctx, instance, Builder.ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (ctx, instance, cancellationToken);
		}

		#if FIXME
		static TestInstance MartinTest (TestContext ctx, TestInstance instance)
		{
			var name = TestInstance.GetTestName (instance);
			var node = TestSerializer.Serialize (instance);
			ctx.LogMessage ("MARTIN TEST: {0} {1}", name, node);

			var result = TestSerializer.Deserialize (ctx, node);
			ctx.LogMessage ("MARTIN TEST DONE: {0}", result);
			return result;
		}
		#endif

		int GetTimeout ()
		{
			if (Builder.Attribute.Timeout != 0)
				return Builder.Attribute.Timeout;
			else if (Builder.Fixture.Attribute.Timeout != 0)
				return Builder.Fixture.Attribute.Timeout;
			else
				return 30000;
		}

		object InvokeInner (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			int timeout = GetTimeout ();
			CancellationTokenSource timeoutCts = null;
			CancellationTokenSource methodCts = null;
			CancellationToken methodToken;

			if (timeout > 0) {
				timeoutCts = new CancellationTokenSource (timeout);
				methodCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, timeoutCts.Token);
				methodToken = methodCts.Token;
			} else {
				methodToken = cancellationToken;
			}

			var parameters = Builder.Method.GetParameters ();

			ctx.LogDebug (10, "INVOKE: {0} {1} {2}", Builder.Name, Builder.Method, instance);

			var index = parameters.Length - 1;
			while (index >= 0) {
				if (instance is NamedTestInstance) {
					instance = instance.Parent;
					continue;
				}

				var param = parameters [index];
				var paramType = param.ParameterType;
				index--;

				if (paramType.Equals (typeof(CancellationToken))) {
					args.AddFirst (methodToken);
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					args.AddFirst (ctx);
					continue;
				}

				var forkedInstance = instance as ForkedTestInstance;
				if (forkedInstance != null) {
					args.AddFirst (forkedInstance);
					instance = instance.Parent;
					continue;
				}

				var heavyInstance = instance as HeavyTestInstance;
				if (heavyInstance != null) {
					args.AddFirst (heavyInstance.Current);
					instance = instance.Parent;
					continue;
				}

				var parameterizedInstance = instance as ParameterizedTestInstance;
				if (parameterizedInstance == null)
					throw new InternalErrorException ();

				if (!paramType.GetTypeInfo ().IsAssignableFrom (parameterizedInstance.ParameterType))
					throw new InternalErrorException ();

				args.AddFirst (parameterizedInstance.Current);
				instance = instance.Parent;
			}

			while (instance != null) {
				if (instance is FixtureTestInstance)
					break;
				var host = instance.Host;
				if (!(host is RepeatedTestHost || host is ReflectionPropertyHost || host is NamedTestHost))
					throw new InternalErrorException ();
				instance = instance.Parent;
			}

			object thisInstance = null;
			if (!Builder.Method.IsStatic) {
				var fixtureInstance = instance as FixtureTestInstance;
				if (fixtureInstance == null)
					throw new InternalErrorException ();
				thisInstance = fixtureInstance.Instance;
				instance = null;
			}

			if (instance != null)
				throw new InternalErrorException ();

			try {
				return DoInvokeInner (thisInstance, args.ToArray ());
			} finally {
				if (timeoutCts != null)
					timeoutCts.Dispose ();
			}
		}

		[StackTraceEntryPoint]
		object DoInvokeInner (object instance, object[] args)
		{
			return Builder.Method.Invoke (instance, args);
		}

		bool CheckFinalStatus (TestContext ctx, bool ok)
		{
			if (ctx.HasPendingException) {
				ctx.OnTestFinished (TestStatus.Error);
				return false;
			} else if (!ok) {
				ctx.OnError (new AssertionException ("Test failed", ctx.GetStackTrace ()));
				return false;
			} else {
				ctx.OnTestFinished (TestStatus.Success);
				return true;
			}
		}

		async Task<bool> ExpectingSuccess (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (ctx, instance, cancellationToken);
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}

			var task = retval as Task;
			if (task == null)
				return CheckFinalStatus (ctx, true);

			try {
				bool ok;
				var btask = retval as Task<bool>;
				if (btask != null)
					ok = await btask;
				else {
					await task;
					ok = true;
				}

				return CheckFinalStatus (ctx, ok);
			} catch (OperationCanceledException) {
				ctx.OnTestFinished (TestStatus.Canceled);
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		async Task<bool> ExpectingException (
			TestContext ctx, TestInstance instance,
			TypeInfo expectedException, CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (ctx, instance, cancellationToken);
				var task = retval as Task;
				if (task != null)
					await task;

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				ctx.OnError (new AssertionException (message, ctx.GetStackTrace ()));
				return false;
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ())) {
					ctx.OnTestFinished (TestStatus.Success);
					return true;
				}
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				ctx.OnError (new AssertionException (message, ex, ctx.GetStackTrace ()));
				return false;
			}
		}
	}
}

