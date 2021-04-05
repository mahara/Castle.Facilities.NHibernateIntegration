namespace Castle.Facilities.NHibernateIntegration.Internal
{
	#region Using Directives

	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Castle.Core;
	using Castle.Core.Interceptor;
	using Castle.Core.Logging;
	using Castle.DynamicProxy;

	#endregion

	/// <summary>
	///     Interceptor in charge o the automatic session management.
	/// </summary>
	[Transient]
	public class NHibernateSessionInterceptor : IInterceptor, IOnBehalfAware
	{
		private readonly ISessionManager _sessionManager;
		private IEnumerable<MethodInfo> _metaInfo;

		/// <summary>
		///     Constructor
		/// </summary>
		public NHibernateSessionInterceptor(ISessionManager sessionManager)
		{
			this._sessionManager = sessionManager;

			this.Logger = NullLogger.Instance;
		}

		/// <summary>
		///     Gets or sets the logger.
		/// </summary>
		/// <value>The logger.</value>
		public ILogger Logger { get; set; }

		/// <summary>
		///     Intercepts the specified invocation and creates a transaction
		///     if necessary.
		/// </summary>
		/// <param name="invocation">The invocation.</param>
		/// <returns></returns>
		public void Intercept(IInvocation invocation)
		{
			MethodInfo methodInfo;
			if (invocation.Method.DeclaringType.IsInterface)
			{
				methodInfo = invocation.MethodInvocationTarget;
			}
			else
			{
				methodInfo = invocation.Method;
			}

			if (this._metaInfo == null || !this._metaInfo.Contains(methodInfo))
			{
				invocation.Proceed();
				return;
			}

			var session = this._sessionManager.OpenSession();

			try
			{
				invocation.Proceed();
			}
			finally
			{
				session.Dispose();
			}
		}

		#region IOnBehalfAware

		/// <summary>
		///     Sets the intercepted component's ComponentModel.
		/// </summary>
		/// <param name="target">The target's ComponentModel</param>
		public void SetInterceptedComponentModel(ComponentModel target)
		{
			this._metaInfo = (MethodInfo[]) target.ExtendedProperties[NHibernateSessionComponentInspector.SessionRequiredMetaInfo];
		}

		#endregion
	}
}