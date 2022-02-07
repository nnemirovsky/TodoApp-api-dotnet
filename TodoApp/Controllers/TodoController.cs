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
        var user = HelperMethods.GetUserByClaims(User, _context);
        var lists = _context.ItemLists.Where(il => il.Author == user).OrderBy(il => il.Id);
        var pagedLists = lists.Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);
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
        }), filter, totalRecords, _uriService, Request.Path.Value!);
        return Ok(pagedResponse);
    }

    [Authorize]
    [HttpPost("lists")]
    public IActionResult CreateList(CreateListWithItemsDto list)
    {
        var createdList = _context.ItemLists.Add(
            new ItemList(list.Name, HelperMethods.GetUserByClaims(User, _context))).Entity;
        List<Item> createdItems = list.Items is not null
            ? list.Items.Select(item => _context.Items.Add(new Item(item.Name, createdList)).Entity).ToList()
            : new List<Item>();
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
    public async Task<IActionResult> GetList(long id)
    {
        var list = await _context.ItemLists.FindAsync(id);
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
        var list = await _context.ItemLists.FindAsync(id);
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

        _context.Items.Add(new Item(item.Name, list));
        await _context.SaveChangesAsync();
        return Accepted(new ApiResponse {Message = $"Item added to list '{list.Name}' successfully."});
    }

    [Authorize]
    [HttpPatch("items/{id:long}")]
    public IActionResult UpdateItem(long id, UpdateItemDto item)
    {
        Item itemFound;
        try
        {
            itemFound = _context.Items.Include(i => i.List).First(i => i.Id == id);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        }

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
        Item item;
        try
        {
            item = _context.Items.Include(i => i.List).First(i => i.Id == id);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ApiResponse {Message = "List not found.", Succeeded = false});
        }

        if (HelperMethods.GetUserByClaims(User, _context) != item.List.Author)
            return Forbid();
        _context.Items.Remove(item);
        _context.SaveChanges();
        return Ok(new ApiResponse() {Message = "Item removed successfully."});
    }
}
