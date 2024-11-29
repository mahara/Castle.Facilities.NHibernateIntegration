#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/hconceicao/Castle.Facilities.NHibernateIntegration3/blob/309146b186f19f27643090b76e01ec497bdb8daa/src/Castle.Facilities.NHibernateIntegration.Tests/Internals/AsyncLocalTestCase.cs" />
    /// </remarks>
    public class Wrapper : ICloneable
    {
        public Wrapper()
        {
            Console.WriteLine("Constructor");
        }

        public int Counter { get; set; }

        public object Clone()
        {
            Console.WriteLine("Clone");
            return new Wrapper();
        }

        public override string ToString()
        {
            return $"Wrapper #{Counter}";
        }
    }

    [TestFixture]
    public class AsyncLocalTestCase
    {
        //private AsyncLocalSessionStore _localSession;
        private AsyncLocal<Wrapper> _localSession;

        [SetUp]
        public void SetUp()
        {
            //_localSession = new AsyncLocalSessionStore();
            _localSession = new AsyncLocal<Wrapper>(
                (args) =>
                {
                    Console.WriteLine($"Changed from '{args.PreviousValue}' to '{args.CurrentValue}' (thread context changed: {args.ThreadContextChanged}).");
                });
        }

        // Case 1: inner branch starts session (outter transaction holds it)
        // Case 2: outer branch starts session (inner re-use it)
        // All cases: sessions are not shared among threads

        [Test]
        public void Case1()
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 1; i++)
            {
                var task = new Task(async (state) => await EntryPoint(state), i);
                task.Start(TaskScheduler.Current);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            //var result = _localSession.Value;
            //Console.WriteLine($"result: {result}");
        }

        private async Task EntryPoint(object? index)
        {
            //var session = new SessionDelegate(true, MockRepository.DynamicMock<ISession>(), _localSession);
            //var session = new SessionDelegate(true, new Mock<ISession>().Object, _localSession);
            //_localSession.Store("default", session);
            if (_localSession.Value is not null)
            {
                throw new Exception("What?");
            }

            _localSession.Value = new Wrapper();

            await Branch1();
            await Branch2();

            var result = _localSession.Value; // _localSession.FindCompatibleSession("default");
                                              // _localSession.Value = null;
            Console.WriteLine($"[#{index}] result: {result}");

            //session.Dispose();

            //result = _localSession.Value;
        }

        private Task Branch1()
        {
            //var session1 = _localSession.FindCompatibleSession("default");
            var session1 = _localSession.Value!.Counter++;

            return Task.CompletedTask;
        }

        private Task<bool> Branch2()
        {
            var session2 = _localSession.Value!.Counter++;

            var tcs = new TaskCompletionSource<bool>();

            ThreadPool.QueueUserWorkItem(
                (state) =>
                {
                    //var session1 = _localSession.FindCompatibleSession("default");
                    var session1 = _localSession.Value.Counter++;

                    tcs.SetResult(true);
                },
                null);

            //tcs.SetResult(true);

            //Task.Run(() =>
            //{
            //    var session1 = _localSession.FindCompatibleSession("default");

            //    tcs.SetResult(true);
            //});

            return tcs.Task;
        }
    }
}
