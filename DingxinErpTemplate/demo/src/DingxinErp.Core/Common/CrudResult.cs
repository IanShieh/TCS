namespace DingxinErp.Core.Common;

/// <summary>
/// CRUD 操作統一回傳格式
/// </summary>
public class CrudResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static CrudResult<T> SuccessResult(T data, string message = "操作成功")
    {
        return new CrudResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static CrudResult<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new CrudResult<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
