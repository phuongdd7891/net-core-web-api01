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
public class BooksController : ControllerBase
{
    private readonly BooksService _booksService;

    public BooksController(BooksService booksService) =>
        _booksService = booksService;

    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthenticationHandler.API_KEY_HEADER}", Roles = Const.ACTION_LIST_BOOK)]
    [HttpGet]
    public async Task<List<Book>> Get() =>
        await _booksService.GetAsync();

    [HttpGet("{id:length(24)}")]
    [CustomAuthorize(Const.ACTION_GET_BOOK)]
    public async Task<ActionResult<Book>> Get(string id)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);

        return book!;
    }

    [HttpPost]
    [CustomAuthorize(Const.ACTION_CREATE_BOOK)]
    public async Task<IActionResult> Post([FromForm] CreateBookRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid name", string.IsNullOrEmpty(request.Data.BookName));
        ErrorStatuses.ThrowBadRequest("Invalid author", string.IsNullOrEmpty(request.Data.Author));
        await _booksService.CreateAsync(request.Data, request.FileData);

        return CreatedAtAction(nameof(Get), new { id = request.Data.Id }, request.Data);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Book updatedBook)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        updatedBook.Id = book!.Id;

        await _booksService.UpdateAsync(id, updatedBook);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);

        await _booksService.RemoveAsync(id);

        return NoContent();
    }

    [HttpGet("download-cover")]
    public async Task<IActionResult> DownloadCover(string id)
    {
        ErrorStatuses.ThrowBadRequest("Id cannot empty", string.IsNullOrEmpty(id));
        var book = await _booksService.GetAsync(id);
        ErrorStatuses.ThrowNotFound("Book not found", book == null);
        var picFile = new FileInfo(Path.Combine(_booksService.GetBookCoverPath(), book!.CoverPicture!));
        ErrorStatuses.ThrowBadRequest("File not found", !picFile.Exists);

        var content = Utils.ConvertToByteArrayChunked(Path.Combine(picFile.DirectoryName!, book!.CoverPicture!));
        return File(content, "image/png");
    }
}