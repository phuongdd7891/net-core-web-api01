using WebApi.Models;
using MongoDB.Driver;
using System.Text;
using WebApi.Models.Requests;

namespace WebApi.Services;

public class BooksService
{
    private readonly IMongoCollection<Book> _booksCollection;
    private readonly IMongoCollection<BookCategory> _categoryCollection;
    
    public BooksService(
        AppDBContext _context
    )
    {
        _booksCollection = _context.Books;
        _categoryCollection = _context.BookCategories;
    }

    public async Task<List<Book>> GetAsync() =>
        await _booksCollection.Find(_ => true).SortByDescending(a => a.CreatedDate).ToListAsync();

    public async Task<Book?> GetAsync(string id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Book newBook, IFormFile? coverData = null)
    {
        if (coverData?.Length > 0)
        {
            var fileName = await UploadFile(coverData);
            newBook.CoverPicture = fileName;
        }
        newBook.CreatedDate = DateTime.Now;
        await _booksCollection.InsertOneAsync(newBook);
    }

    public async Task UpdateAsync(Book updatedBook, IFormFile? coverData = null)
    {
        if (coverData?.Length > 0)
        {
            var fileName = await UploadFile(coverData);
            updatedBook.CoverPicture = fileName;
        }
        updatedBook.ModifiedDate = DateTime.Now;
        await _booksCollection.ReplaceOneAsync(x => x.Id == updatedBook.Id, updatedBook);
    }

    public async Task<bool> RemoveAsync(string id)
    {
        try
        {
            await _booksCollection.DeleteOneAsync(x => x.Id == id);
        }
        catch
        {
            return false;
        }
        return true;
    }

    public string GetBookCoverPath() => Path.Combine(Directory.GetCurrentDirectory(), @"../", "BookCover");

    private async Task<string> UploadFile(IFormFile file)
    {
        if (file?.Length > 0)
        {
            var filePath = GetBookCoverPath();
            DirectoryInfo dirInfo = new DirectoryInfo(filePath);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            var fileName = string.Format("{0}{1}", Guid.NewGuid(), Path.GetExtension(file.FileName));
            var path = Path.Combine(filePath, fileName);
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                ms.Position = 0;
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    await ms.CopyToAsync(fs);
                    return fileName;
                }
            }
        }
        return string.Empty;
    }

    #region Category
    public async Task<List<BookCategory>> GetCategoriesAsync() =>
        await _categoryCollection.Find(_ => true).ToListAsync();

    public async Task<List<BookCategory>> GetCategoriesByParentAsync(string parentId) =>
        await _categoryCollection.Find(a => !string.IsNullOrEmpty(a.ParentPath) && a.ParentPath.Contains($".{parentId}")).ToListAsync();

    public async Task<BookCategory?> GetCategoryAsync(string id) =>
        await _categoryCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateCategoryAsync(CreateBookCateogryRequest request)
    {
        var category = new BookCategory {
            CategoryName = request.Name
        };
        if (!string.IsNullOrEmpty(request.Parent))
        {
            var parent = await GetCategoryAsync(request.Parent);
            if (parent != null)
            {
                category.ParentPath = $"{parent.ParentPath}.{request.Parent}";
            }
        }
        if (string.IsNullOrEmpty(request.Id))
        {
            await _categoryCollection.InsertOneAsync(category);    
        }
        else
        {
            category.Id = request.Id;
            await _categoryCollection.ReplaceOneAsync(a => a.Id == request.Id, category);
        }
    }

    #endregion

}