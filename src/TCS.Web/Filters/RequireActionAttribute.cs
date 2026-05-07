namespace TCS.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireActionAttribute : Attribute
{
    public string Action { get; }
    public RequireActionAttribute(string action) => Action = action;
}
