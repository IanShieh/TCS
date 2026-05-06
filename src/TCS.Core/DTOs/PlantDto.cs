namespace TCS.Core.DTOs;

/// <summary>廠別回應 DTO（唯讀，映射 CMSMB view）</summary>
public record PlantDto(
    string PlantCode,
    string? PlantName);
