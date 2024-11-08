using SupportPatriots.Business.Dtos;
using SupportPatriots.Business.Interfaces;
using SupportPatriots.Model.Models;
using SupportPatriots.Model.Repositories;
using AutoMapper;
using System;
using System.Threading.Tasks;

namespace SupportPatriots.Business
{
	public class TenantService : ITenantService
	{
		private readonly ITenantRepository tenantRepository;
		private readonly IMapper mapper;

		public TenantService(ITenantRepository tenantRepository, IMapper mapper)
		{
			this.tenantRepository = tenantRepository;
			this.mapper = mapper;
		}

		public async Task<TenantDto> CreateTenant(TenantForCreationDto tenant)
		{
			if (!tenant.ParentTenantId.HasValue)
			{
				throw new Exception("Parent tenant id is required!");
			}
			var parentTenant = await tenantRepository.GetTenantAsync(tenant.ParentTenantId.Value);
			if (parentTenant == null)
			{
				throw new Exception("Tenant does not exist!");
			}

			if (!parentTenant.IsParent)
			{
				throw new Exception("Create tenants bellow child tenants is not allowed!");
			}
			if (await tenantRepository.TenantExistsAsync(tenant.Name))
			{
				throw new Exception($"Tenant {tenant.Name} already exists!");
			}

			var tenantToAdd = mapper.Map<Tenant>(tenant);
			tenantRepository.Add(tenantToAdd);
			await tenantRepository.SaveAsync();

			return mapper.Map<TenantDto>(tenantToAdd);
		}

		public async Task<bool> UpdateTenant(TenantForCreationDto tenant)
		{
			if (!tenant.TenantId.HasValue)
			{
				throw new Exception("Please use POST if creation of a new tenant was intended!");
			}
			var tenantFromRepo = await tenantRepository.GetTenantAsync(tenant.TenantId.Value);
			if (tenantFromRepo == null)
			{
				throw new Exception("Tenant does not exist!");
			}

			if (await TenantNameExists(tenant.Name, tenantFromRepo.TenantId))
			{
				throw new Exception($"Tenant {tenant.Name} already exists!");
			}

			mapper.Map(tenant, tenantFromRepo);
			tenantRepository.Update(tenantFromRepo);
			await tenantRepository.SaveAsync();
			return true;
		}

		private async Task<bool> TenantNameExists(string tenantName, Guid currentTenantId)
		{
			var tenant = await tenantRepository.GetTenantAsync(tenantName);
			return tenant != null && tenant.TenantId != currentTenantId;
		}

        public async Task<TenantDto> GetTenant(string tenantName)
        {
            var result = await tenantRepository.GetTenantAsync(tenantName);
            return mapper.Map<TenantDto>(result);
		}
	}

}
