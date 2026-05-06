using System.Runtime.CompilerServices;
using DingxinErp.Core.Common;
using DingxinErp.Core.Entities;
using DingxinErp.Core.Interfaces;
using DingxinErp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DingxinErp.Infrastructure.Repositories;

/// <summary>
/// 範例 Repository 實作 — EF Core 資料存取
/// ★ 雙路徑分流：InMemory 用 LINQ Skip/Take、SQL Server 用 ROW_NUMBER() 相容 2008 R2
/// </summary>
public class SampleRepository : ISampleRepository
{
    private readonly AppDbContext _context;

    public SampleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SampleHeader>> GetPagedAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDir = null)
    {
        if (_context.IsInMemoryDatabase)
            return await GetPagedByLinqAsync(page, pageSize, search, sortBy, sortDir);
        else
            return await GetPagedByRawSqlAsync(page, pageSize, search, sortBy, sortDir);
    }

    /// <summary>允許排序的欄位白名單（防止 SQL Injection）</summary>
    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "TA001", "TA002", "TA003", "TA004", "TA005", "TA007"
    };

    /// <summary>取得安全的排序欄位名稱</summary>
    private static string GetSafeOrderColumn(string? sortBy) =>
        !string.IsNullOrWhiteSpace(sortBy) && AllowedSortColumns.Contains(sortBy) ? sortBy.ToUpperInvariant() : "TA001";

    /// <summary>取得排序方向</summary>
    private static bool IsDescending(string? sortDir) =>
        string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

    /// <summary>InMemory 路徑：LINQ Skip/Take</summary>
    private async Task<PagedResult<SampleHeader>> GetPagedByLinqAsync(int page, int pageSize, string? search, string? sortBy, string? sortDir)
    {
        var query = _context.SampleHeaders
            .Include(h => h.Details)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTrim = search.Trim();
            query = query.Where(h =>
                h.TA001.Contains(searchTrim) ||
                h.TA002.Contains(searchTrim) ||
                h.TA004.Contains(searchTrim) ||
                (h.TA005 != null && h.TA005.Contains(searchTrim)));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // 動態排序
        var col = GetSafeOrderColumn(sortBy);
        var desc = IsDescending(sortDir);
        query = (col, desc) switch
        {
            ("TA001", false) => query.OrderBy(h => h.TA001).ThenBy(h => h.TA002),
            ("TA001", true) => query.OrderByDescending(h => h.TA001).ThenByDescending(h => h.TA002),
            ("TA002", false) => query.OrderBy(h => h.TA002),
            ("TA002", true) => query.OrderByDescending(h => h.TA002),
            ("TA003", false) => query.OrderBy(h => h.TA003),
            ("TA003", true) => query.OrderByDescending(h => h.TA003),
            ("TA004", false) => query.OrderBy(h => h.TA004),
            ("TA004", true) => query.OrderByDescending(h => h.TA004),
            ("TA005", false) => query.OrderBy(h => h.TA005),
            ("TA005", true) => query.OrderByDescending(h => h.TA005),
            ("TA007", false) => query.OrderBy(h => h.TA007),
            ("TA007", true) => query.OrderByDescending(h => h.TA007),
            _ => query.OrderBy(h => h.TA001).ThenBy(h => h.TA002),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SampleHeader>
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = page,
            PageSize = pageSize,
            SearchString = search
        };
    }

    /// <summary>SQL Server 路徑：ROW_NUMBER() OVER() 相容 SQL Server 2008 R2</summary>
    private async Task<PagedResult<SampleHeader>> GetPagedByRawSqlAsync(int page, int pageSize, string? search, string? sortBy, string? sortDir)
    {
        var startRow = (page - 1) * pageSize + 1;
        var endRow = page * pageSize;

        // 動態建構 WHERE 條件
        var whereClause = "1=1";
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().Replace("'", "''");
            whereClause = $@"(
                TA001 LIKE '%' + '{s}' + '%' OR
                TA002 LIKE '%' + '{s}' + '%' OR
                TA004 LIKE '%' + '{s}' + '%' OR
                ISNULL(TA005,'') LIKE '%' + '{s}' + '%')";
        }

        // COUNT 查詢
        var countSql = $"SELECT COUNT(*) FROM SAMPLE_HEADER WHERE {whereClause}";
        var totalItems = 0;
        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = countSql;
            if (cmd.Connection!.State != System.Data.ConnectionState.Open)
                await cmd.Connection.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            totalItems = Convert.ToInt32(result);
        }

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // 動態排序欄位（白名單驗證）
        var orderCol = GetSafeOrderColumn(sortBy);
        var orderDir = IsDescending(sortDir) ? "DESC" : "ASC";

        // ROW_NUMBER() 分頁查詢 — 相容 SQL Server 2008 R2
        var pageSql = FormattableStringFactory.Create(
            $@"SELECT * FROM (
                SELECT *, ROW_NUMBER() OVER (ORDER BY {orderCol} {orderDir}) AS RowNum
                FROM SAMPLE_HEADER
                WHERE {whereClause}
            ) AS PagedResults
            WHERE RowNum BETWEEN {startRow} AND {endRow}
            ORDER BY RowNum");

        var items = await _context.SampleHeaders
            .FromSql(pageSql)
            .Include(h => h.Details)
            .AsNoTracking()
            .ToListAsync();

        return new PagedResult<SampleHeader>
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = page,
            PageSize = pageSize,
            SearchString = search
        };
    }

    public async Task<SampleHeader?> GetByKeyAsync(string ta001, string ta002)
    {
        return await _context.SampleHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.TA001 == ta001 && h.TA002 == ta002);
    }

    public async Task<List<SampleDetail>> GetDetailsByKeyAsync(string ta001, string ta002)
    {
        return await _context.SampleDetails
            .Where(d => d.TB001 == ta001 && d.TB002 == ta002)
            .OrderBy(d => d.TB003)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string ta001, string ta002)
    {
        return await _context.SampleHeaders
            .AnyAsync(h => h.TA001 == ta001 && h.TA002 == ta002);
    }

    public async Task AddAsync(SampleHeader entity)
    {
        await _context.SampleHeaders.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SampleHeader entity)
    {
        _context.SampleHeaders.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string ta001, string ta002)
    {
        var entity = await _context.SampleHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.TA001 == ta001 && h.TA002 == ta002);

        if (entity != null)
        {
            _context.SampleHeaders.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SampleDetail?> GetDetailByKeyAsync(string ta001, string ta002, string tb003)
    {
        return await _context.SampleDetails
            .FirstOrDefaultAsync(d => d.TB001 == ta001 && d.TB002 == ta002 && d.TB003 == tb003);
    }

    public async Task AddDetailAsync(SampleDetail entity)
    {
        await _context.SampleDetails.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDetailAsync(SampleDetail entity)
    {
        _context.SampleDetails.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDetailAsync(string ta001, string ta002, string tb003)
    {
        var entity = await _context.SampleDetails
            .FirstOrDefaultAsync(d => d.TB001 == ta001 && d.TB002 == ta002 && d.TB003 == tb003);

        if (entity != null)
        {
            _context.SampleDetails.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
