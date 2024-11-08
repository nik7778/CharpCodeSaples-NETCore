using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SupportPatriots.Core.Application;
using SupportPatriots.Model.DBContext;
using SupportPatriots.Model.Models;
using Microsoft.EntityFrameworkCore;

namespace SupportPatriots.Model.Repositories
{
    public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
    {
        public TenantRepository(SupportPatriotsMvcContext context, ApplicationUserContext apUserContext) 
        : base(context, apUserContext)
        {
        }

        public IQueryable<Tenant> All
        {
            get { return context.Tenants; }
        }

        public async Task<Tenant> GetTenantAsync(Guid tenantId) =>
            await context.FindAsync<Tenant>(tenantId);

        public async Task<Tenant> GetTenantAsync(string tenantName) =>
            await All.FirstOrDefaultAsync(t => t.Name == tenantName);

        public async Task<Tenant> GetSuperTenantAsync() => await All.FirstOrDefaultAsync(s => s.IsSuperTenant);

        public async Task<List<Tenant>> GetTenantsAsync(Tenant tenant)
        {
            var tenants = new List<Tenant>();
            if (tenant.IsSuperTenant)
            {
                tenants = await GetAllTenantsAsync();
            }
            else if (tenant.IsParent)
            {
                tenants = await GetTenantsByParentAsync(tenant.TenantId);
            }

            return tenants;
        }

        public async Task<bool> TenantExistsAsync(string tenantName) =>
            await context.Tenants.AnyAsync(
                t => t.Name == tenantName);

        public override void Add(Tenant tenant)
        {
            tenant.TenantId = Guid.NewGuid();
            tenant.Setting = new TenantSetting {TenantId = tenant.TenantId};
            base.Add(tenant);
        }

        public async Task DeleteChildTenantsAsync(Tenant tenant)
        {
            var childTenants = await context.Tenants.Where(t => t.ParentTenantId == tenant.TenantId).ToListAsync();
            foreach (var childTenant in childTenants)
            {
                await DeleteTenantAsync(childTenant);
            }
        }

        public async Task DeleteTenantAsync(Tenant tenant)
        {
            var settings = await context.TenantSettings.FirstOrDefaultAsync(ts => ts.TenantId == tenant.TenantId);
            if (settings != null)
            {
                context.Remove(settings);
            }
            context.Tenants.Remove(tenant);
        }

        public async Task<List<Tenant>> GetAllTenantsAsync()
        {
            var parentTenants = await All.Where(t => !t.IsSuperTenant && !t.ParentTenantId.HasValue)
                .ToListAsync();
            var tenants = parentTenants.Select(t => new Tenant
            {
                TenantId = t.TenantId,
                Name = t.Name,
                Tenants = GetTenantsByParentAsync(t.TenantId).Result
            });

            return tenants.ToList();
        }

        public async Task<List<Tenant>> GetAllTenantsFlatAsync() => await All.ToListAsync();

        private async Task<List<Tenant>> GetTenantsByParentAsync(Guid parentTenantId) =>
            await All.Where(t => t.ParentTenantId == parentTenantId)
                .ToListAsync();
    }

    public interface ITenantRepository
    {
		IQueryable<Tenant> All { get; }

		Task<List<Tenant>> GetAllTenantsAsync();
        Task<List<Tenant>> GetAllTenantsFlatAsync();
        Task<Tenant> GetTenantAsync(Guid tenantId);
        Task<Tenant> GetTenantAsync(string tenantName);
        Task<List<Tenant>> GetTenantsAsync(Tenant tenant);
        Task<bool> TenantExistsAsync(string tenantName);
        void Add(Tenant tenant);
        void Update(Tenant tenant);
        Task DeleteChildTenantsAsync(Tenant tenant);
        Task DeleteTenantAsync(Tenant tenant);
        Task SaveAsync();
        Task<Tenant> GetSuperTenantAsync();
    }
}
