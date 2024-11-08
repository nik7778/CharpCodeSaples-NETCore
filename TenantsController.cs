using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SupportPatriots.Model.Repositories;
using SupportPatriots.Business.Dtos;
using SupportPatriots.WebApi.Controllers.Api.ValidationAttributes;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using SupportPatriots.Business.Interfaces;

namespace SupportPatriots.WebApi.Controllers.Api
{
	[ApiController]
	[Authorize(Policy = "AppTenants")]
	[Route("api/[controller]")]
	public class TenantsController : ControllerBase
	{
		private readonly ITenantRepository tenantRepository;
		private readonly IMapper mapper;
		private readonly ITenantService tenantService;

		public TenantsController(ITenantRepository tenantRepository, IMapper mapper, ITenantService tenantService)
		{
			this.tenantRepository = tenantRepository;
			this.mapper = mapper;
			this.tenantService = tenantService;
		}

		[HttpGet]
		public async Task<IActionResult> Get([DataSourceRequest] DataSourceRequest request)
		{
			var data = await tenantRepository.All.ToDataSourceResultAsync(request);
			return Ok(data);
		}

		[HttpGet("All")]
		public async Task<IActionResult> GetAllTenants()
		{
			var tenants = await tenantRepository.GetAllTenantsFlatAsync();
			return Ok(mapper.Map<List<TenantDto>>(tenants));
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> Get([FromRoute] Guid id)
		{
			var tenant = await tenantRepository.GetTenantAsync(id);
			var tenants = await tenantRepository.GetTenantsAsync(tenant);
			if (tenants.Any())
			{
				tenant.Tenants = tenants;
			}

			return Ok(mapper.Map<TenantDto>(tenant));
		}

		[Authorize(Policy = "AppTenantsCanEdit")]
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] TenantForCreationDto tenant)
		{
			try
			{
				var result = await tenantService.CreateTenant(tenant);
				return CreatedAtRoute("", new { id = result.TenantId }, result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[Authorize(Policy = "AppTenantsCanEdit")]
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] TenantForCreationDto tenant)
		{
			try
			{
				var result = await tenantService.UpdateTenant(tenant);
				return NoContent();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("{tenantId}")]
		[ValidateTenantId]
		public async Task<IActionResult> DeleteTenant(Guid tenantId)
		{
			var tenantFromRepo = await tenantRepository.GetTenantAsync(tenantId);

			await tenantRepository.DeleteChildTenantsAsync(tenantFromRepo);
			await tenantRepository.SaveAsync();
			await tenantRepository.DeleteTenantAsync(tenantFromRepo);
			await tenantRepository.SaveAsync();

			return NoContent();
		}
    }
}
