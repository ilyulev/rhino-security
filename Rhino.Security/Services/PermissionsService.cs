using System;
using System.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using Rhino.Security.Impl.Util;
using Rhino.Security.Interfaces;
using Rhino.Security.Model;
using System.Linq;

namespace Rhino.Security.Services
{
    /// <summary>
	/// Allow to retrieve and remove permissions
	/// on users, user groups, entities groups and entities.
	/// </summary>
	public class PermissionsService : IPermissionsService
	{
		private readonly IAuthorizationRepository authorizationRepository;
        private readonly ISession session;

        /// <summary>
		/// Initializes a new instance of the <see cref="PermissionsService"/> class.
		/// </summary>
		/// <param name="authorizationRepository">The authorization editing service.</param>
		/// <param name="session">The NHibernate session</param>
		public PermissionsService(IAuthorizationRepository authorizationRepository,
		                          ISession session)
		{
			this.authorizationRepository = authorizationRepository;
		    this.session = session;
		}

		#region IPermissionsService Members

		/// <summary>
		/// Gets the permissions for the specified user
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns></returns>
		public Permission[] GetPermissionsFor(IUser user)
		{
			DetachedCriteria criteria = DetachedCriteria.For<Permission>()
				.Add(Restrictions.Eq("User.Id", user.SecurityInfo.Identifier)
				     || Subqueries.PropertyIn("UsersGroup.Id",
				                              SecurityCriterions.AllGroups(user).SetProjection(Projections.Id())));

			return FindResults(criteria);
		}


		/// <summary>
		/// Gets the permissions for the specified entity
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="operationName">Name of the operation.</param>
		/// <returns></returns>
		public Permission[] GetGlobalPermissionsFor(IUser user, string operationName)
		{
		    return this.GetGlobalPermissionsFor(user, new string[] { operationName });
		}

        /// <summary>
        /// Gets the permissions for the specified operations
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="operationNames">Names of the operations.</param>
        /// <returns></returns>
        public Permission[] GetGlobalPermissionsFor(IUser user, string[] operationNames)
        {
            if (operationNames == null)
                throw new ArgumentNullException("operationNames");

            string[] allOperationNames = Strings.GetHierarchicalOperationNames(operationNames);
            DetachedCriteria criteria = DetachedCriteria.For<Permission>()
                .Add(UserRestriction(user)
                     || Subqueries.PropertyIn("UsersGroup.Id",
                                              SecurityCriterions.AllGroups(user).SetProjection(Projections.Id())))
                .Add(Restrictions.IsNull("EntitiesGroup"))
                .Add(Restrictions.IsNull("EntitySecurityKey"))
                .CreateAlias("Operation", "op")
                .Add(Restrictions.In("op.Name", allOperationNames));

            return FindResults(criteria);
        }

        static SimpleExpression UserRestriction(IUser user)
        {
            return Restrictions.Eq("User.Id", user.SecurityInfo.Identifier);
        }

        /// <summary>
        /// Gets all permissions for the specified operation
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor(string operationName)
        {
            return this.GetPermissionsFor(new string[] {operationName});
        }
        
        /// <summary>
        /// Gets all permissions for the specified operations
        /// </summary>
        /// <param name="operationNames">Names of the operations.</param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor(string[] operationNames)
        {
            if (operationNames == null)
                throw new ArgumentNullException("operationNames");

            string[] allOperationNames = Strings.GetHierarchicalOperationNames(operationNames);
            DetachedCriteria criteria = DetachedCriteria.For<Permission>()
                .CreateAlias("Operation", "op")
                .Add(Restrictions.In("op.Name", allOperationNames));

            return this.FindResults(criteria);
        }

        /// <summary>
        /// Get all permissions for the specified operation and user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor(IUser user, string operationName)
        {
            return this.GetPermissionsFor(user, new string[] { operationName });
        }

        /// <summary>
        /// Get all permissions for the specified operations and user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="operationNames"></param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor(IUser user, string[] operationNames)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (operationNames == null) throw new ArgumentNullException("operationNames");

            string[] allOperationNames = Strings.GetHierarchicalOperationNames(operationNames);

            DetachedCriteria criteria = DetachedCriteria.For<Permission>()
             .Add(UserRestriction(user) || Subqueries.PropertyIn("UsersGroup.Id",
                                              SecurityCriterions.AllGroups(user).SetProjection(Projections.Id())))
                .CreateAlias("Operation", "op")
                .Add(Restrictions.In("op.Name", allOperationNames));

            return FindResults(criteria);
        }

        /// <summary>
		/// Gets the permissions for the specified user and entity
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="user">The user.</param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public Permission[] GetPermissionsFor<TEntity>(IUser user, TEntity entity) where TEntity : class
		{
			Guid key = Security.ExtractKey(entity);
			EntitiesGroup[] entitiesGroups = authorizationRepository.GetAssociatedEntitiesGroupsFor(entity);

			DetachedCriteria criteria = DetachedCriteria.For<Permission>()
                .Add(UserRestriction(user)
				     || Subqueries.PropertyIn("UsersGroup.Id",
				                              SecurityCriterions.AllGroups(user).SetProjection(Projections.Id())))
                .Add(Restrictions.Eq("EntitySecurityKey", key) || Restrictions.In("EntitiesGroup", entitiesGroups));

			return FindResults(criteria);
		}

		/// <summary>
		/// Gets the permissions for the specified entity
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="user">The user.</param>
		/// <param name="entity">The entity.</param>
		/// <param name="operationName">Name of the operation.</param>
		/// <returns></returns>
		public Permission[] GetPermissionsFor<TEntity>(IUser user, TEntity entity, string operationName) where TEntity : class
		{
            return this.GetPermissionsFor(user, entity, new string[] { operationName });
		}

        /// <summary>
        /// Gets the permissions for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="user">The user.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="operationNames">Names of the operations.</param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor<TEntity>(IUser user, TEntity entity, string[] operationNames) where TEntity : class
        {
            Guid key = Security.ExtractKey(entity);
            //string[] allOperationNames = Strings.GetHierarchicalOperationNames(operationNames);
            EntitiesGroup[] entitiesGroups = authorizationRepository.GetAssociatedEntitiesGroupsFor(entity);		

            AbstractCriterion onCriteria =
                (Restrictions.Eq("EntitySecurityKey", key) || Restrictions.In("EntitiesGroup", entitiesGroups)) ||
                (Restrictions.IsNull("EntitiesGroup") && Restrictions.IsNull("EntitySecurityKey"));
            DetachedCriteria criteria = DetachedCriteria.For<Permission>()
                .Add(UserRestriction(user)
                     || Subqueries.PropertyIn("UsersGroup.Id",
                                              SecurityCriterions.AllGroups(user).SetProjection(Projections.Id())))
                .Add(onCriteria)
                .CreateAlias("Operation", "op")
                .Add(Restrictions.In("op.Name", operationNames));

            return FindResults(criteria);
        }

		/// <summary>
		/// Gets the permissions for the specified entity
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="entity">The entity.</param>
		/// <returns></returns>
		public Permission[] GetPermissionsFor<TEntity>(TEntity entity) where TEntity : class
		{
			if (entity is IUser) // the combpiler will direct IUser instance to here, annoying
				return GetPermissionsFor((IUser) entity);

			Guid key = Security.ExtractKey(entity);
			EntitiesGroup[] groups = authorizationRepository.GetAssociatedEntitiesGroupsFor(entity);
			DetachedCriteria criteria = DetachedCriteria.For<Permission>()
				.Add(Restrictions.Eq("EntitySecurityKey", key) || Restrictions.In("EntitiesGroup", groups));

			return FindResults(criteria);
		}

        /// <summary>
        /// Gets the permissions for the specified user group
        /// </summary>
        /// <param name="usersGroup">The group.</param>
        /// <param name="operationName">The operation name.</param>
        /// <returns></returns>
        public Permission[] GetPermissionsFor(UsersGroup usersGroup, string operationName)
        {
            string[] operationNames = Strings.GetHierarchicalOperationNames(operationName);

            DetachedCriteria criteria = DetachedCriteria.For<Permission>()
              .Add(Restrictions.Eq("UsersGroup", usersGroup))
              .CreateAlias("Operation", "op")
              .Add(Restrictions.In("op.Name", operationNames));

            return FindResults(criteria);
        }

		#endregion

		private Permission[] FindResults(DetachedCriteria criteria)
		{
		    ICollection<Permission> permissions = criteria.GetExecutableCriteria(session)
		        .AddOrder(Order.Desc("Level"))
		        .AddOrder(Order.Asc("Allow"))
                .SetCacheable(true)
                .List<Permission>();
		    return permissions.ToArray();
		}
	}
}
