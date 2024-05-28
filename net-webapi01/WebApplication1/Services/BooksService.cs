using WebApi.Models;
using MongoDB.Driver;
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

    public async Task<List<Book>> GetAsync(GetBooksRequest req, int skip = 0, int limit = 10)
    {
        var filterDate = Builders<Book>.Filter.Where(a => a.CreatedDate >= req.CreatedFrom && a.CreatedDate < req.CreatedTo 
            || !req.CreatedFrom.HasValue && !req.CreatedTo.HasValue
            || a.CreatedDate >= req.CreatedFrom && !req.CreatedTo.HasValue
            || a.CreatedDate < req.CreatedTo && !req.CreatedFrom.HasValue);
        return string.IsNullOrEmpty(req.SearchKey) ? await _booksCollection
            .Find(filterDate)
            .SortByDescending(a => a.CreatedDate).Skip(skip).Limit(limit).ToListAsync()
            : await _booksCollection.Find(Builders<Book>.Filter.And(
                    Builders<Book>.Filter.Text(req.SearchExact ? $"\"{req.SearchKey}\"" : $"{req.SearchKey}"),
                    filterDate
                ))
                // .Project<Book>(Builders<Book>.Projection.MetaTextScore("TextScore"))
                // .Sort(Builders<Book>.Sort.MetaTextScore("TextScore"))
                .Skip(skip).Limit(limit).ToListAsync();
    }

    public async Task<Book?> GetAsync(string id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<long> GetCount() =>
        await _booksCollection.CountDocumentsAsync(_ => true);

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

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private int baseCountCopy = 1000;
    public async Task<long> CreateCopyAsync(Book sourceBook, int numOfCopy = 1, string? createdBy = null)
    {
        try
        {
            await semaphoreSlim.WaitAsync();
            int count = 1, skip = 0;
            var task = new List<Task<BulkWriteResult<Book>>>();
            var listWrites = new List<WriteModel<Book>>();
            while (count <= numOfCopy)
            {
                var cloneBook = new Book
                {
                    BookName = $"{sourceBook.BookName} - copy {count + 1}",
                    Author = sourceBook.Author,
                    Category = sourceBook.Category,
                    CoverPicture = sourceBook.CoverPicture,
                    Summary = sourceBook.Summary,
                    Price = sourceBook.Price,
                    CreatedDate = DateTime.Now,
                    CreatedBy = createdBy
                };
                listWrites.Add(new InsertOneModel<Book>(cloneBook));
                if (count % baseCountCopy == 0)
                {
                    task.Add(_booksCollection.BulkWriteAsync(listWrites.Skip(skip * baseCountCopy)));
                    skip++;
                }
                count++;
            };
            var remainCount = numOfCopy % baseCountCopy;
            if (remainCount > 0)
            {
                task.Add(_booksCollection.BulkWriteAsync(listWrites.Skip(listWrites.Count - remainCount)));
            }
            var result = await Task.WhenAll(task);
            return result.Sum(a => a.InsertedCount);
        }
        finally
        {
            semaphoreSlim.Release();
        }
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
            var result = await _booksCollection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> RemoveManyAsync(string[] ids, DateTime? from, DateTime? to)
    {
        try
        {
            await semaphoreSlim.WaitAsync();
            if (from.HasValue && to.HasValue)
            {
                int count = 1, skip = 0;
                var task = new List<Task<BulkWriteResult<Book>>>();
                var listWrites = new List<WriteModel<Book>>();
                var books = await _booksCollection.Find(x => x.CreatedDate >= from && x.CreatedDate < to).ToListAsync();
                foreach (var bookId in books.Select(x => x.Id))
                {
                    listWrites.Add(new DeleteOneModel<Book>(Builders<Book>.Filter.Eq("Id", bookId)));
                    if (count % baseCountCopy == 0)
                    {
                        task.Add(_booksCollection.BulkWriteAsync(listWrites.Skip(skip * baseCountCopy)));
                        skip++;
                    }
                    count++;
                }
                var remainCount = books.Count % baseCountCopy;
                if (remainCount > 0)
                {
                    task.Add(_booksCollection.BulkWriteAsync(listWrites.Skip(listWrites.Count - remainCount)));
                }
                var result = await Task.WhenAll(task);
                return result.Sum(a => a.DeletedCount);
            }
            else
            {
                var result = await _booksCollection.DeleteManyAsync(x => ids.Contains(x.Id));
                return result.DeletedCount;
            }
        }
        finally
        {
            semaphoreSlim.Release();
        }
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
        var category = new BookCategory
        {
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