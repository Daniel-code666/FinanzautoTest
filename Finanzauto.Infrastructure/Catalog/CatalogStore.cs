using System.Linq.Expressions;
using Finanzauto.Application.Catalog;
using Finanzauto.Application.Common.Exceptions;
using Finanzauto.Domain.Entities;
using Finanzauto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Finanzauto.Infrastructure.Catalog;

public sealed class CatalogStore(FinanzautoDbContext db) : ICatalogStore
{
    private static readonly Expression<Func<Product, ProductResponse>> ProductProjection = x => new(
        x.ProductId, x.ProductName, x.CategoryId, x.Category.CategoryName,
        x.SupplierId, x.Supplier.CompanyName, x.QuantityPerUnit, x.UnitPrice,
        x.UnitsInStock, x.UnitsOnOrder, x.ReorderLevel, x.Discontinued);

    // El predicado explícito mantiene el mismo conjunto en Count, página y detalle,
    // incluso si una referencia se desactiva directamente en DBeaver.
    private IQueryable<Product> VisibleProducts() => db.Products.AsNoTracking()
        .Where(x => x.Category.Active && x.Supplier.Active);

    public async Task<CatalogPage<ProductResponse>> ListProductsAsync(ProductQuery query, CancellationToken ct)
    {
        var products = VisibleProducts();
        if (query.CategoryId.HasValue) products = products.Where(x => x.CategoryId == query.CategoryId);
        if (query.SupplierId.HasValue) products = products.Where(x => x.SupplierId == query.SupplierId);
        if (query.MinPrice.HasValue) products = products.Where(x => x.UnitPrice >= query.MinPrice);
        if (query.MaxPrice.HasValue) products = products.Where(x => x.UnitPrice <= query.MaxPrice);
        if (query.Discontinued.HasValue) products = products.Where(x => x.Discontinued == query.Discontinued);
        if (query.InStock.HasValue)
            products = query.InStock.Value ? products.Where(x => x.UnitsInStock > 0) : products.Where(x => x.UnitsInStock == 0);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            products = products.Where(x => EF.Property<string>(x, "SearchName").Contains(term));
        }
        var total = await products.CountAsync(ct);
        IOrderedQueryable<Product> ordered = (query.SortBy, query.Descending) switch
        {
            (ProductSort.Name, false) => products.OrderBy(x => x.ProductName).ThenBy(x => x.ProductId),
            (ProductSort.Name, true) => products.OrderByDescending(x => x.ProductName).ThenByDescending(x => x.ProductId),
            (ProductSort.Price, false) => products.OrderBy(x => x.UnitPrice).ThenBy(x => x.ProductId),
            (ProductSort.Price, true) => products.OrderByDescending(x => x.UnitPrice).ThenByDescending(x => x.ProductId),
            (_, true) => products.OrderByDescending(x => x.ProductId),
            _ => products.OrderBy(x => x.ProductId)
        };
        var items = await ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(ProductProjection).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    public Task<ProductDetailResponse?> GetProductAsync(int id, CancellationToken ct) =>
        VisibleProducts().Where(x => x.ProductId == id)
            .Select(x => new ProductDetailResponse(
                new ProductResponse(x.ProductId, x.ProductName, x.CategoryId, x.Category.CategoryName,
                    x.SupplierId, x.Supplier.CompanyName, x.QuantityPerUnit, x.UnitPrice, x.UnitsInStock,
                    x.UnitsOnOrder, x.ReorderLevel, x.Discontinued),
                new CategoryDetailResponse(x.CategoryId, x.Category.CategoryName, x.Category.Description,
                    x.Category.Picture, x.Category.PictureContentType))).SingleOrDefaultAsync(ct);

    public async Task<int> SaveProductAsync(int? id, Product product, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await CatalogReferenceGuard.LockActiveAsync(db, [product.CategoryId], product.SupplierId, ct);
        if (id.HasValue)
        {
            var existing = await db.Products.SingleOrDefaultAsync(x => x.ProductId == id, ct)
                ?? throw new ApiException(404, "Producto no encontrado.");
            product.ProductId = id.Value;
            db.Entry(existing).CurrentValues.SetValues(new
            {
                product.ProductName,
                product.CategoryId,
                product.SupplierId,
                product.QuantityPerUnit,
                product.UnitPrice,
                product.UnitsInStock,
                product.UnitsOnOrder,
                product.ReorderLevel,
                product.Discontinued
            });
        }
        else db.Products.Add(product);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return product.ProductId;
    }

    public async Task DeactivateProductAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == id, ct)
            ?? throw new ApiException(404, "Producto no encontrado.");
        db.Products.Remove(product);
        await SaveAsync(ct);
    }

    public async Task<CatalogPage<CategoryResponse>> ListCategoriesAsync(CatalogQuery query, CancellationToken ct)
    {
        var categories = db.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            categories = categories.Where(x => x.CategoryName.ToUpper().Contains(term));
        }
        var total = await categories.CountAsync(ct);
        var items = await categories.OrderBy(x => x.CategoryId).Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).Select(x => new CategoryResponse(x.CategoryId, x.CategoryName,
                x.Description, x.Picture != null)).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    public Task<CategoryDetailResponse?> GetCategoryAsync(int id, CancellationToken ct) =>
        db.Categories.AsNoTracking().Where(x => x.CategoryId == id)
            .Select(x => new CategoryDetailResponse(x.CategoryId, x.CategoryName, x.Description,
                x.Picture, x.PictureContentType)).SingleOrDefaultAsync(ct);

    public async Task<int> SaveCategoryAsync(int? id, Category category, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var name = category.CategoryName.ToUpperInvariant();
        if (await db.Categories.IgnoreQueryFilters()
            .AnyAsync(x => EF.Property<string>(x, "NormalizedName") == name && (!id.HasValue || x.CategoryId != id), ct))
            throw new ApiException(409, "El nombre de categoría ya existe, incluso si está inactiva.");

        if (id.HasValue)
        {
            var existing = await LockCategoryAsync(id.Value, ct);
            category.CategoryId = id.Value;
            db.Entry(existing).CurrentValues.SetValues(new
            {
                category.CategoryName,
                category.Description,
                category.Picture,
                category.PictureContentType
            });
        }
        else db.Categories.Add(category);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return category.CategoryId;
    }

    public async Task DeactivateCategoryAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var category = await LockCategoryAsync(id, ct);
        if (await db.Products.AnyAsync(x => x.CategoryId == id, ct))
            throw new ApiException(409, "Reasigna o desactiva los productos activos antes de eliminar la categoría.");
        db.Categories.Remove(category);
        await SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<Category> LockCategoryAsync(int id, CancellationToken ct)
    {
        var categories = await db.Categories.FromSql($"""
            SELECT * FROM "Categories" WHERE "CategoryId" = {id} AND "Active" FOR UPDATE
            """).ToListAsync(ct);
        return categories.SingleOrDefault() ?? throw new ApiException(404, "Categoría no encontrada.");
    }

    public async Task<CatalogPage<SupplierResponse>> ListSuppliersAsync(CatalogQuery query, CancellationToken ct)
    {
        var suppliers = db.Suppliers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            suppliers = suppliers.Where(x => x.CompanyName.ToUpper().Contains(term));
        }
        var total = await suppliers.CountAsync(ct);
        var items = await suppliers.OrderBy(x => x.SupplierId).Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).Select(x => new SupplierResponse(x.SupplierId, x.CompanyName)).ToListAsync(ct);
        return new(items, total, query.Page, query.PageSize);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ApiException(409, "Ya existe un registro con ese nombre.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ApiException(409, "Una referencia ya no existe. Actualiza los datos e intenta nuevamente.");
        }
    }
}
