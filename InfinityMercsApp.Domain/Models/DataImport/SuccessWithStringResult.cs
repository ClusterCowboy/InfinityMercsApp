namespace InfinityMercsApp.Domain.Models.DataImport;

/// <summary>
/// Represents a success or failure from a data import.
/// </summary>
/// <param name="IsSuccessful"></param>
/// <param name="Message"></param>
public record SuccessWithStringResult(bool IsSuccessful, string Message);
