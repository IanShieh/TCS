namespace TCS.Core.Common;

public class CrudResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static CrudResult<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static CrudResult<T> ErrorResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
