using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Filters;
using TodoApp.Helpers;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Wrappers;

namespace TodoApp.Controllers;

[ApiController]
public class TodoController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly AppDbContext _context;
    private readonly IUriService _uriService;

    public TodoController(ILogger<AccountController> logger, AppDbContext context, IUriService uriService)
    {
        _logger = logger;
        _context = context;
        _uriService = uriService;
    }

    [Authorize]
    [HttpGet("lists")]
    public IActionResult GetAllLists([FromQuery] PaginationFilter filter)
    {
        var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
        var user = HelperMethods.GetUserByClaims(User, _context);
        var lists = _context.ItemLists.Where(il => il.Author == user).OrderBy(il => il.Id);
        var pagedLists = lists.Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
            .Take(validFilter.PageSize);
        var totalRecords = lists.Count();
        var pagedResponse = PaginationHelper.CreatePagedResponse(pagedLists.Select(il => new
        {
            il.Id,
            il.Name,
            Items = _context.Items.Where(item => item.List == il).Select(item => new ItemDetailDto
            {
                Id = item.Id,
                Name = item.Name,
                IsComplete = item.IsComplete
            }).ToList()
        }), validFilter, totalRecords, _uriService, Request.Path.Value!);
        return Ok(pagedResponse);
    }

    [Authorize]
    [HttpPost("lists")]
    public IActionResult CreateList(CreateListWithItemsDto list)
    {
        var createdList = _context.ItemLists.Add(new ItemList
            {Name = list.Name, Author = HelperMethods.GetUserByClaims(User, _context)}).Entity;
        var createdItems = new List<Item>();
        foreach (var item in list.Items)
            createdItems.Add(_context.Items.Add(new Item {List = createdList, Name = item.Name}).Entity);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetList), new {id = createdList.Id}, new ApiResponse(new
        {
            createdList.Id, createdList.Name,
            items = createdItems.Select(item => new ItemDetailDto
                {Id = item.Id, IsComplete = item.IsComplete, Name = item.Name})
        }));
    }

    [Authorize]
    [HttpGet("lists/{id:long}")]
    public IActionResult GetList(long id)
    {
        var list = _context.ItemLists.Find(id);
        if (list is null)
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        if (HelperMethods.GetUserByClaims(User, _context) != list.Author)
            return Forbid();
        return Ok(new ApiResponse(new
        {
            list.Id, list.Name, items = _context.Items.Where(item => item.List.Id == list.Id).Select(item =>
                new ItemDetailDto {Id = item.Id, IsComplete = item.IsComplete, Name = item.Name})
        }));
    }

    [Authorize]
    [HttpDelete("lists/{id:long}")]
    public async Task<IActionResult> DeleteList(long id)
    {
        var list = _context.ItemLists.Find(id);
        if (list is null)
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        if (HelperMethods.GetUserByClaims(User, _context) != list.Author)
            return Forbid();
        _context.ItemLists.Remove(list);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse {Message = "List removed successfully."});
    }

    [Authorize]
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(CreateItemDto item)
    {
        var list = await _context.ItemLists.FindAsync(item.ListId);
        if (list is null)
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        if (HelperMethods.GetUserByClaims(User, _context) != list.Author)
            return Forbid();

        _context.Items.Add(new Item {List = list, Name = item.Name});
        await _context.SaveChangesAsync();
        return Accepted(new ApiResponse {Message = $"Item added to list '{list.Name}' successfully."});
    }

    [Authorize]
    [HttpPatch("items/{id:long}")]
    public IActionResult UpdateItem(long id, UpdateItemDto item)
    {
        var itemFound = _context.Items.Include(i => i.List).FirstOrDefault(i => i.Id == id);
        if (itemFound is null)
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        if (HelperMethods.GetUserByClaims(User, _context) != itemFound.List.Author)
            return Forbid();
        if (item.Name is not null)
            itemFound.Name = item.Name;
        if (item.IsComplete != itemFound.IsComplete)
            itemFound.IsComplete = item.IsComplete;
        _context.Items.Update(itemFound);
        _context.SaveChanges();
        return Accepted(new ApiResponse() {Message = "Item updated successfully."});
    }

    [Authorize]
    [HttpDelete("items/{id:long}")]
    public IActionResult DeleteItem(long id)
    {
        var item = _context.Items.Include(i => i.List).FirstOrDefault(i => i.Id == id);
        if (item is null)
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        if (HelperMethods.GetUserByClaims(User, _context) != item.List.Author)
            return Forbid();
        _context.Items.Remove(item);
        _context.SaveChanges();
        return Ok(new ApiResponse() {Message = "Item removed successfully."});
    }
}
