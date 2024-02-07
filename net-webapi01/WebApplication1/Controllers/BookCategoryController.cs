using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.Requests;
using WebApi.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/book-category")]
public class BookCategoryController : BaseController
{
    private readonly BooksService _booksService;

    public BookCategoryController(BooksService booksService) =>
        _booksService = booksService;

    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    [HttpGet]
    public async Task<DataResponse<List<BookCategory>>> Get()
    {
        var data = await _booksService.GetCategoriesAsync();
        var list = data.Where(a => string.IsNullOrEmpty(a.ParentPath)).ToList();
        var listData = new List<BookCategory>();
        list.ForEach(a => {
            listData.Add(a);
            AddCategoryChild(listData, data, a.Id!);
        });
        return new DataResponse<List<BookCategory>>
        {
            Data = listData
        };
    }

    private void AddCategoryChild(List<BookCategory> result, List<BookCategory> listAll, string parentId)
    {
        var childs = listAll.FindAll(x => !string.IsNullOrEmpty(x.ParentPath) && x.ParentPath == $".{parentId}");
        childs.ForEach(a => {
            result.Add(a);
            AddCategoryChild(result, listAll, $"{(string.IsNullOrEmpty(a.ParentPath) ? "" : a.ParentPath.Substring(1) + ".")}{a.Id}");
        });
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    public async Task<DataResponse<BookCategory>> Get(string id)
    {
        var book = await _booksService.GetCategoryAsync(id);
        ErrorStatuses.ThrowNotFound("Category not found", book == null);

        return new DataResponse<BookCategory>
        {
            Data = book
        };
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    public async Task<IActionResult> Post([FromBody] CreateBookCateogryRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid name", string.IsNullOrEmpty(request.Name));
        ErrorStatuses.ThrowBadRequest("Invalid parent", !string.IsNullOrEmpty(request.Parent) && !string.IsNullOrEmpty(request.Id) && string.Compare(request.Parent, request.Id) == 0);
        if (!string.IsNullOrEmpty(request.Id))
        {
            var catsByParent = await _booksService.GetCategoriesByParentAsync(request.Id);
            var cat = await _booksService.GetCategoryAsync(request.Id);
            ErrorStatuses.ThrowBadRequest("Category is being a parent category", !string.IsNullOrEmpty(request.Parent) && (!cat?.ParentPath?.Contains($".{request.Parent}") ?? true) && catsByParent?.Count > 0);
        }
        await _booksService.CreateCategoryAsync(request);

        return Ok(new DataResponse());
    }
}