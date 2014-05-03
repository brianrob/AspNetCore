// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Exposes role related api which will automatically save changes to the RoleStore
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public class RoleManager<TRole> : IDisposable where TRole : class
    {
        private bool _disposed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="services"></param>
        /// <param name="store">The IRoleStore is responsible for commiting changes via the UpdateAsync/CreateAsync methods</param>
        public RoleManager(IServiceProvider services, IRoleStore<TRole> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }
            RoleValidator = services.GetService<IRoleValidator<TRole>>() ?? new RoleValidator<TRole>();
            Store = store;
        }

        /// <summary>
        ///     Persistence abstraction that the Manager operates against
        /// </summary>
        protected IRoleStore<TRole> Store { get; private set; }

        /// <summary>
        ///     Used to validate roles before persisting changes
        /// </summary>
        public IRoleValidator<TRole> RoleValidator { get; set; }

        /// <summary>
        ///     Returns an IQueryable of roles if the store is an IQueryableRoleStore
        /// </summary>
        public virtual IQueryable<TRole> Roles
        {
            get
            {
                var queryableStore = Store as IQueryableRoleStore<TRole>;
                if (queryableStore == null)
                {
                    throw new NotSupportedException(Resources.StoreNotIQueryableRoleStore);
                }
                return queryableStore.Roles;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IQueryableRoleStore
        /// </summary>
        public virtual bool SupportsQueryableRoles
        {
            get
            {
                ThrowIfDisposed();
                return Store is IQueryableRoleStore<TRole>;
            }
        }

        /// <summary>
        ///     Dispose this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IdentityResult> ValidateRoleInternal(TRole role, CancellationToken cancellationToken)
        {
            return (RoleValidator == null) ? IdentityResult.Success : await RoleValidator.ValidateAsync(this, role, cancellationToken);
        }

        /// <summary>
        ///     CreateAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            var result = await ValidateRoleInternal(role, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            await Store.CreateAsync(role, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     UpdateAsync an existing role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            var result = await ValidateRoleInternal(role, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            await Store.UpdateAsync(role, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     DeleteAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            await Store.DeleteAsync(role, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     Returns true if the role exists
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await FindByNameAsync(roleName, cancellationToken) != null;
        }

        /// <summary>
        ///     FindByLoginAsync a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.FindByIdAsync(roleId, cancellationToken);
        }

        /// <summary>
        /// Return the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetRoleNameAsync(role, cancellationToken);
        }

        /// <summary>
        /// Set the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetRoleNameAsync(TRole role, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            await Store.SetRoleNameAsync(role, name, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        /// Return the role id for a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetRoleIdAsync(role, cancellationToken);
        }

        /// <summary>
        ///     FindByLoginAsync a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await Store.FindByNameAsync(roleName, cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        ///     When disposing, actually dipose the store
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Store.Dispose();
            }
            _disposed = true;
        }
    }
}