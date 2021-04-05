namespace Castle.Facilities.NHibernateIntegration.Internal
{
	#region Using Directives

	using System.Linq;
	using System.Reflection;

	using Castle.Core;
	using Castle.MicroKernel;
	using Castle.MicroKernel.ModelBuilder;

	#endregion

	/// <summary>
	///     Inspect components searching for Session Aware services.
	/// </summary>
	public class NHibernateSessionComponentInspector : IContributeComponentModelConstruction
	{
		internal const string SessionRequiredMetaInfo = "nhfacility.SessionRequiredMetaInfo";

		private const string ComponentModelName = "session.interceptor";

		/// <summary>
		///     Process the model
		/// </summary>
		public void ProcessModel(IKernel kernel, ComponentModel model)
		{
			if (model.Implementation.IsDefined(typeof(NHSessionAwareAttribute), true))
			{
				model.Dependencies.Add(new DependencyModel(ComponentModelName, typeof(NHibernateSessionInterceptor), false));

				var methods = model
				              .Implementation
				              .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
				              .Where(m => m.IsDefined(typeof(NHSessionRequiredAttribute), false));

				model.ExtendedProperties[SessionRequiredMetaInfo] = methods.ToArray();

				model.Interceptors.Add(new InterceptorReference(typeof(NHibernateSessionInterceptor)));
			}
		}
	}
}