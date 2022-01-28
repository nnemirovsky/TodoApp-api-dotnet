using TodoApp.Filters;
using TodoApp.Services;
using TodoApp.Wrappers;

namespace TodoApp.Helpers;

public class PaginationHelper
{
    public static PagedResponse CreatePagedResponse(object pagedData, PaginationFilter validFilter, int totalRecords,
        IUriService uriService, string route)
    {
        var response = new PagedResponse(pagedData, validFilter.PageNumber, validFilter.PageSize);
        var totalPages = (double) totalRecords / validFilter.PageSize;
        int roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));

        if (validFilter.PageNumber >= 1 && validFilter.PageNumber < roundedTotalPages)
            response.NextPage = uriService.GetPageUri(
                new PaginationFilter(validFilter.PageNumber + 1, validFilter.PageSize), route);
        if (validFilter.PageNumber - 1 >= 1 && validFilter.PageNumber <= roundedTotalPages)
            response.PreviousPage = uriService.GetPageUri(
                new PaginationFilter(validFilter.PageNumber - 1, validFilter.PageSize), route);

        response.FirstPage = uriService.GetPageUri(new PaginationFilter(1, validFilter.PageSize), route);
        response.LastPage = uriService.GetPageUri(
            new PaginationFilter(roundedTotalPages, validFilter.PageSize), route);
        response.TotalPages = roundedTotalPages;
        response.TotalRecords = totalRecords;
        return response;
    }
}
