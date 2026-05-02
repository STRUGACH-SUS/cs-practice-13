using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi;
using Microsoft.AspNetCore.Http.HttpResults;

public class CRUD
{
    public static async Task Create(WebApplication app)
    {
        app.MapPost("/books", async ( 
            [FromBody]BookBody body, 
            [FromServices]DataContext dataContext,
            CancellationToken ct) =>
        {
            var book = new Book
            {
                Column = body.Column,
                TypeOfCSharp = body.TypeOfCSharp, 
                Nullable = body.Nullable
            };
            dataContext.Books.Add(book);
    
            await dataContext.SaveChangesAsync(ct);
    
            return new BookModel
            {
                Id = book.Id,
                Column = book.Column,
                TypeOfCSharp = book.TypeOfCSharp,
                Nullable = book.Nullable
            };
        });
        await Task.CompletedTask;
    }

    public static async Task ReadById(WebApplication app)
    {
        app.MapGet("/books/{id:int}", async Task<Results<NotFound, Ok<BookModel>>> (
            [FromRoute] int id,
            [FromServices] DataContext dataContext,
            CancellationToken ct) =>
        {
            var book = await dataContext.Books.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (book is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(new BookModel
            {
                Id = book.Id,
                Column = book.Column,
                TypeOfCSharp = book.TypeOfCSharp,
                Nullable = book.Nullable
            });
        });
        await Task.CompletedTask;
    }

    public static async Task Read(WebApplication app)
    {
        app.MapGet("/books", async (
            [FromServices] DataContext dataContext,
            CancellationToken ct,
            [FromQuery] string? search = null) =>
        {
            var query = dataContext.Books.AsQueryable();
            if (string.IsNullOrEmpty(search) is false)
            {
                query = query.Where(x => EF.Functions.Like(
                    x.Column.ToLower(), 
                    $"%{search.ToLower()}%"));
            }

            return await query
                .Select(x => new BookModel
                {
                    Id = x.Id,
                    Column = x.Column,
                    TypeOfCSharp = x.TypeOfCSharp,
                    Nullable = x.Nullable
                })
                .OrderByDescending(x => x.Id)
                .ToListAsync(ct);
        });
        await Task.CompletedTask;
    }

    public static async Task Update(WebApplication app)
    {
        app.MapPut("/books/{id:int}", async Task<Results<NotFound, Ok<BookModel>>> (
            [FromRoute] int id,
            [FromBody] BookBody body,
            [FromServices] DataContext dataContext,
            CancellationToken ct) =>
        {
            var book = await dataContext.Books.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (book is null)
            {
                return TypedResults.NotFound();
            }
    
            book.Column = body.Column;
            await dataContext.SaveChangesAsync(ct);

            return TypedResults.Ok(new BookModel
            {
                Id = book.Id,
                Column = book.Column,
                TypeOfCSharp = book.TypeOfCSharp,
                Nullable = book.Nullable
            });
        });
        await Task.CompletedTask;
    }

    public static async Task Delete(WebApplication app)
    {
        app.MapDelete("/books/{id:int}", async Task<Results<NotFound, NoContent>> (
            [FromRoute] int id,
            DataContext dataContext,
            CancellationToken ct) =>
        {
            var book = await dataContext.Books.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (book is null)
            {
                return TypedResults.NotFound();
            }
    
            dataContext.Books.Remove(book);
            await dataContext.SaveChangesAsync(ct);
    
            return TypedResults.NoContent();
        });
        await Task.CompletedTask;
    }
}