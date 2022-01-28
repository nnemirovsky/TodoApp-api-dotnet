using TodoApp.Filters;

namespace TodoApp.Services;

public interface IUriService
{
    public Uri GetPageUri(PaginationFilter filter, string route);
}
