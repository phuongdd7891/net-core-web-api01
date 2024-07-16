using WebApi.Models;
using WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Requests;
using CoreLibrary.Utils;
using WebApi.Authentication;
using System.ComponentModel;
using Nest;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly BooksService _booksService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(
        BooksService booksService,
        ILogger<BooksController> logger)
    {
        _booksService = booksService;
        _logger = logger;
        _logger.LogInformation("books controller constructor");
    }

    [UserAuthorize]
    [HttpGet]
    [Description("get list of books")]
    public async Task<DataResponse<GetBooksReply>> GetList([FromQuery] GetBooksRequest request)
    {
        var data = await _booksService.GetAsync(request, HttpContext.User().Username!);
        var categories = await _booksService.GetCategoriesAsync();
        var list = data.GroupJoin(categories, a => a.Category, c => c.Id, (a, c) => new { Book = a, Cats = c })
            .SelectMany(a => a.Cats.DefaultIfEmpty(), (a, c) =>
            {
                var book = a.Book;
                book.Category = c?.CategoryName;
                return book;
            }).ToList();
        return new DataResponse<GetBooksReply>
        {
            Data = new GetBooksReply
            {
                List = list,
                Total = await _booksService.GetCount()
            }
        };
    }

    [HttpGet("{id:length(24)}")]
    [UserAuthorize]
    [Description("get a book by id")]
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
    [UserAuthorize]
    public async Task<IActionResult> Post([FromForm] CreateBookRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid name", string.IsNullOrEmpty(request.Data.BookName));
        ErrorStatuses.ThrowBadRequest("Invalid author", string.IsNullOrEmpty(request.Data.Author));
        request.Data.CreatedBy = HttpContext.User().Username;
        await _booksService.CreateAsync(request.Data, request.FileData);

        //return CreatedAtAction(nameof(Get), new { id = request.Data.Id });
        return Ok(new DataResponse());
    }

    [HttpPost("copy")]
    [UserAuthorize]
    public async Task<IActionResult> Copy([FromQuery] string id, [FromQuery] int qty)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        var result = await _booksService.CreateCopyAsync(book!, qty, HttpContext.User().Username);
        return Ok(new DataResponse
        {
            Data = string.Format("Inserted {0}", result)
        });
    }

    [HttpPut("{id:length(24)}")]
    [UserAuthorize]
    public async Task<IActionResult> Update(string id, [FromForm] CreateBookRequest request)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        book!.BookName = request.Data.BookName;
        book.Author = request.Data.Author;
        book.Category = request.Data.Category;
        book.ModifiedBy = HttpContext.User().Username;
        await _booksService.UpdateAsync(book, request.FileData);

        return Ok(new DataResponse());
    }

    [HttpDelete("{id:length(24)}")]
    [UserAuthorize]
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

    [HttpDelete("delete-many")]
    [UserAuthorize]
    public async Task<IActionResult> DeleteMany([FromQuery] string[] ids, string from, string to)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", (ids == null || ids.Length == 0) && string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to));

        var result = await _booksService.RemoveManyAsync(ids!, Convert.ToDateTime(from), Convert.ToDateTime(to));
        return Ok(new DataResponse
        {
            Data = string.Format("Deleted {0}", result)
        });
    }

    [HttpGet("download-cover")]
    [UserAuthorize]
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