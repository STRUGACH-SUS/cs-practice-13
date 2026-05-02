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
    public required string Column {get;set;}
    public required string TypeOfCSharp {get;set;}
    public required string Nullable {get;set;}
}

public record BookModel : BookBody
{
    public required int Id {get;set;}
}