#region License

//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

#endregion

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities103
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Data;

	using Castle.Facilities.NHibernateIntegration.SessionStores;
	using Castle.MicroKernel;
	using Castle.Services.Transaction;

	using NHibernate;

	using NUnit.Framework;

	using Rhino.Mocks;

	using ITransaction = Castle.Services.Transaction.ITransaction;

	#endregion

	[TestFixture]
	public class DefaultSessionManagerTestCase : IssueTestCase
	{
		protected override string ConfigurationFile => "EmptyConfiguration.xml";

		public override void OnSetUp()
		{
			this._sessionStore = new CallContextSessionStore();
			this._kernel = this.mockRepository.DynamicMock<IKernel>();
			this._factoryResolver = this.mockRepository.DynamicMock<ISessionFactoryResolver>();
			this._transactionManager = this.mockRepository.DynamicMock<ITransactionManager>();
			this._transaction = this.mockRepository.DynamicMock<ITransaction>();
			this._sessionFactory = this.mockRepository.DynamicMock<ISessionFactory>();
			this._session = this.mockRepository.DynamicMock<ISession>();
			this._statelessSession = this.mockRepository.DynamicMock<IStatelessSession>();
			this._contextDictionary = new Hashtable();
			this._sessionManager = new DefaultSessionManager(this._sessionStore, this._kernel, this._factoryResolver);
		}

		private const string Alias = "myAlias";
		private const string InterceptorFormatString = DefaultSessionManager.InterceptorFormatString;
		private const string InterceptorName = DefaultSessionManager.InterceptorName;
		private const IsolationMode DefaultIsolationMode = IsolationMode.ReadUncommitted;
		private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadUncommitted;

		private ISessionStore _sessionStore;
		private IKernel _kernel;
		private ISessionFactoryResolver _factoryResolver;
		private ITransactionManager _transactionManager;
		private ITransaction _transaction;
		private ISessionFactory _sessionFactory;
		private ISession _session;
		private IStatelessSession _statelessSession;
		private IDictionary _contextDictionary;
		private ISessionManager _sessionManager;

		[Test]
		public void WhenBeginTransactionFailsSessionIsRemovedFromSessionStore()
		{
			using (this.mockRepository.Record())
			{
				Expect.Call(this._kernel.Resolve<ITransactionManager>()).Return(this._transactionManager);
				Expect.Call(this._transactionManager.CurrentTransaction).Return(this._transaction);
				Expect.Call(this._factoryResolver.GetSessionFactory(Alias)).Return(this._sessionFactory);
				Expect.Call(this._kernel.HasComponent(string.Format(InterceptorFormatString, Alias))).Return(false);
				Expect.Call(this._kernel.HasComponent(InterceptorName)).Return(false).Repeat.Any();
				Expect.Call(this._sessionFactory.OpenSession()).Return(this._session);
				this._session.FlushMode = this._sessionManager.DefaultFlushMode;
				Expect.Call(this._transaction.Context).Return(this._contextDictionary).Repeat.Any();
				Expect.Call(this._transaction.IsolationMode).Return(DefaultIsolationMode).Repeat.Any();
				Expect.Call(this._session.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
			}

			using (this.mockRepository.Playback())
			{
				try
				{
					this._sessionManager.OpenSession(Alias);
					Assert.Fail("DbException not thrown");
				}
				catch (Exception ex)
				{
					//ignore
					Console.WriteLine(ex.ToString());
				}

				Assert.IsNull(this._sessionStore.FindCompatibleSession(Alias),
				              "The sessionStore shouldn't contain compatible session if the session creation fails");
			}
		}

		[Test]
		public void WhenBeginTransactionFailsStatelessSessionIsRemovedFromSessionStore()
		{
			using (this.mockRepository.Record())
			{
				Expect.Call(this._kernel.Resolve<ITransactionManager>()).Return(this._transactionManager);
				Expect.Call(this._transactionManager.CurrentTransaction).Return(this._transaction);
				Expect.Call(this._factoryResolver.GetSessionFactory(Alias)).Return(this._sessionFactory);
				Expect.Call(this._sessionFactory.OpenStatelessSession()).Return(this._statelessSession);
				Expect.Call(this._transaction.Context).Return(this._contextDictionary).Repeat.Any();
				Expect.Call(this._transaction.IsolationMode).Return(DefaultIsolationMode).Repeat.Any();
				Expect.Call(this._statelessSession.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
			}

			using (this.mockRepository.Playback())
			{
				try
				{
					this._sessionManager.OpenStatelessSession(Alias);
					Assert.Fail("DbException not thrown");
				}
				catch (Exception)
				{
					//ignore
					//Console.WriteLine(ex.ToString());
				}

				Assert.IsNull(this._sessionStore.FindCompatibleStatelessSession(Alias),
				              "The sessionStore shouldn't contain compatible session if the session creation fails");
			}
		}
	}
}