using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WebApi;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<DataContext>(ef =>
{
    ef.UseSqlite("Data Source=App1.db");
} );

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DataContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/books", async ( 
    [FromBody]BookBody body, 
    [FromServices]DataContext dataContext,
    CancellationToken ct) =>
{
    var book = new Book
    {
        Name = body.Name,
        Author = body.Author, 
        ReleaseDate = DateOnly.FromDateTime(DateTime.Now)
    };
    dataContext.Books.Add(book);
    
    await dataContext.SaveChangesAsync(ct);
    
    return new BookModel
    {
        Id = book.Id,
        Name = book.Name,
        Author = book.Author,
        ReleaseDate = book.ReleaseDate
    };
});

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
        Name = book.Name,
        Author = book.Author,
        ReleaseDate = book.ReleaseDate
    });
});

app.MapGet("/books", async (
    [FromServices] DataContext dataContext,
    CancellationToken ct,
    [FromQuery] string? search = null) =>
{
    var query = dataContext.Books.AsQueryable();
    if (string.IsNullOrEmpty(search) is false)
    {
        query = query.Where(x => EF.Functions.Like(
            x.Name.ToLower(), 
            $"%{search.ToLower()}%"));
    }

    return await query
        .Select(x => new BookModel
        {
            Id = x.Id,
            Name = x.Name,
            Author = x.Author,
            ReleaseDate = x.ReleaseDate
        })
        .OrderByDescending(x => x.Id)
        .ToListAsync(ct);
});

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

    book.Name = body.Name;
    await dataContext.SaveChangesAsync(ct);

    return TypedResults.Ok(new BookModel
    {
        Id = book.Id,
        Name = book.Name,
        Author = book.Author,
        ReleaseDate = book.ReleaseDate
    });
});

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
    
app.Run();
public record BookBody
{
    public required string Name {get;set;}
    public required string Author {get;set;}
    public required DateOnly ReleaseDate {get;set;}
}

public record BookModel : BookBody
{
    public required int Id {get;set;}
}