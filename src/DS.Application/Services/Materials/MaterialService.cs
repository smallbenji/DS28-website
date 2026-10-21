using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Materials;

public class MaterialService(IDataDbContext dataDb)
{
    public async Task AddMaterial(CreateMaterialDto dto)
    {
        await dataDb.Materials.AddAsync(dto.ToModel());
        await dataDb.SaveChangesAsync();
    }

    public async Task RemoveMaterial(int id)
    {
        var material = await dataDb.Materials.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (material == null) return;

        material.DeletedAt = DateTime.UtcNow;

        await dataDb.SaveChangesAsync();
    }

    public async Task<ServiceResult<List<MaterialDto>>> GetMaterials(bool includeDeleted = false)
    {
        var query = includeDeleted
            ? dataDb.Materials.IgnoreQueryFilters()
            : dataDb.Materials;

        return new ServiceResult<List<MaterialDto>>(ServiceOutcome.Success, (await query
            .AsNoTracking()
            .ToListAsync()
            ).Select(x => new MaterialDto(x)).ToList());
    }

    public async Task CreateMaterialOrder(CreateMaterialOrderDto dto)
    {
        await dataDb.MaterialOrders.AddAsync(dto.ToModel());
        await dataDb.SaveChangesAsync();
    }

    public async Task<List<MaterialOrder>> GetMaterialOrders()
    {
        return await dataDb.MaterialOrders
            .AsNoTracking()
            .Where(x => x.DeletedAt == null)
            .ToListAsync();
    }

    public async Task RemoveMaterialOrder(int id)
    {
        var materialOrder = await dataDb.MaterialOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (materialOrder == null) return;

        materialOrder.DeletedAt = DateTime.UtcNow;

        await dataDb.SaveChangesAsync();
    }
}