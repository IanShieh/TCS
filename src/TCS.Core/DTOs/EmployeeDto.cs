namespace TCS.Core.DTOs;

/// <summary>員工查詢回應 DTO（唯讀，映射 CMSMV view）</summary>
public record EmployeeDto(
    string EmployeeId,
    string Name,
    string? Department,
    string? HireDate);
