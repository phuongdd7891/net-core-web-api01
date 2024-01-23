using WebApi.Models;
using WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Authentication;
using WebApi.Models.Requests;
using CoreLibrary.Utils;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : BaseController
{
    private readonly BooksService _booksService;

    public BooksController(BooksService booksService) =>
        _booksService = booksService;

    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}", Roles = Const.ACTION_LIST_BOOK)]
    [HttpGet]
    public async Task<DataResponse<List<Book>>> Get()
    {
        var data = await _booksService.GetAsync();
        var categories = await _booksService.GetCategoriesAsync();
        var catDict = categories.ToDictionary(a => a.Id!);
        var list = data.ToList();
        list.ForEach(a => {
            a.Category = catDict.ContainsKey(a.Category ?? "") ? catDict[a.Category!].CategoryName : string.Empty;
        });
        return new DataResponse<List<Book>>
        {
            Data = list
        };
    }

    [HttpGet("{id:length(24)}")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}", Roles = Const.ACTION_GET_BOOK)]
    public async Task<DataResponse<Book>> Get(string id)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);

        return new DataResponse<Book>
        {
            Data = book
        };
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}", Roles = Const.ACTION_CREATE_BOOK)]
    public async Task<IActionResult> Post([FromForm] CreateBookRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid name", string.IsNullOrEmpty(request.Data.BookName));
        ErrorStatuses.ThrowBadRequest("Invalid author", string.IsNullOrEmpty(request.Data.Author));
        var user = await GetRequestUser();
        if (user != null)
        {
            request.Data.CreatedBy = user.UserName;
        }
        await _booksService.CreateAsync(request.Data, request.FileData);

        //return CreatedAtAction(nameof(Get), new { id = request.Data.Id }, request.Data);
        return Ok(new DataResponse<string>());
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, [FromForm] CreateBookRequest request)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        book!.BookName = request.Data.BookName;
        book!.Author = request.Data.Author;
        book!.Category = request.Data.Category;
        var user = await GetRequestUser();
        if (user != null)
        {
            book.ModifiedBy = user.UserName;
        }

        await _booksService.UpdateAsync(book, request.FileData);

        return Ok(new DataResponse<string>());
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);

        var result = await _booksService.RemoveAsync(id);
        if (result)
        {
            if (!string.IsNullOrEmpty(book!.CoverPicture))
            {
                var picFile = new FileInfo(Path.Combine(_booksService.GetBookCoverPath(), book.CoverPicture));
                if (picFile.Exists)
                {
                    picFile.Delete();
                }
            }
        }
        return Ok(new DataResponse<bool>
        {
            Data = result
        });
    }

    [HttpGet("download-cover")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}", Roles = Const.ACTION_GET_BOOK)]
    public async Task<IActionResult> DownloadCover(string id)
    {
        ErrorStatuses.ThrowBadRequest("Id cannot empty", string.IsNullOrEmpty(id));
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        ErrorStatuses.ThrowNotFound("Image not found", string.IsNullOrEmpty(book!.CoverPicture));
        var picFile = new FileInfo(Path.Combine(_booksService.GetBookCoverPath(), book!.CoverPicture!));
        ErrorStatuses.ThrowBadRequest("File not found", !picFile.Exists);

        var content = Utils.ConvertToByteArrayChunked(Path.Combine(picFile.DirectoryName!, book!.CoverPicture!));
        var splitFileName = book.CoverPicture!.Split('.');
        return File(content, $"image/{splitFileName[splitFileName.Length - 1]}");
    }
}