using System.Net;

namespace WebApi.Tests;

public class Test(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly HttpClient _client = fixture.Api.CreateClient();
    
    [Fact]
    public async Task CreateBook_ThenGetById_ReturnsSameBook()
    {
        // Arrange
        var newBook = new BookBody("Text", "Text", "Text");

        // Act & Assert - Create
        var createResponse = await _client.PostAsync("/books", newBook.ToJsonContent());
        var createdBook = await createResponse.ReadFromJsonAsync<BookModel>();
        
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(createdBook);
        Assert.Equal(newBook.Column, createdBook.Column);
        Assert.Equal(newBook.TypeOfCSharp, createdBook.TypeOfCSharp);
        Assert.Equal(newBook.Nullable, createdBook.Nullable);

        // Act & Assert - Get by Id
        var getResponse = await _client.GetAsync($"/books/{createdBook.Id}");
        var fetchedBook = await getResponse.ReadFromJsonAsync<BookModel>();
        
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(createdBook.Id, fetchedBook!.Id);
        Assert.Equal(createdBook.Column, fetchedBook.Column);
    }
    
    [Fact]
    public async Task GetAllBooks_ReturnsList()
    {
        // Arrange
        var book1 = new BookBody("1", "1", "1");
        var book2 = new BookBody("2", "2", "2");
        
        await _client.PostAsync("/books", book1.ToJsonContent());
        await _client.PostAsync("/books", book2.ToJsonContent());

        // Act
        var response = await _client.GetAsync("/books");
        var books = await response.ReadFromJsonAsync<List<BookModel>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(books);
        Assert.Equal(2, books.Count);
        Assert.Contains(books, b => b.Column == "1");
        Assert.Contains(books, b => b.Column == "2");
    }
    
    [Fact]
    public async Task SearchBooks_ByColumn_ReturnsFilteredResult()
    {
        // Arrange
        await _client.PostAsync("/books", new BookBody("3", "3", "3").ToJsonContent());
        await _client.PostAsync("/books", new BookBody("4", "4", "4").ToJsonContent());

        // Act
        var response = await _client.GetAsync("/books?search=3");
        var books = await response.ReadFromJsonAsync<List<BookModel>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(books!);
        Assert.Contains("3", books[0].Column);
    }
    
    [Fact]
    public async Task UpdateBook_ChangesColumnValue()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/books", new BookBody("5", "5", "5").ToJsonContent());
        var book = await createResponse.ReadFromJsonAsync<BookModel>();
        
        // Act
        var updatedBody = new BookBody("6", "8", "12");
        var updateResponse = await _client.PutAsync($"/books/{book!.Id}", updatedBody.ToJsonContent());
        var updatedBook = await updateResponse.ReadFromJsonAsync<BookModel>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("6", updatedBook!.Column);
        
        var getResponse = await _client.GetAsync($"/books/{book.Id}");
        var fetchedBook = await getResponse.ReadFromJsonAsync<BookModel>();
        Assert.Equal("6", fetchedBook!.Column);
    }
    
    [Fact]
    public async Task DeleteBook_RemovesFromDatabase()
    {
        // Arrange
        var createResponse = await _client.PostAsync("/books", new BookBody("11", "11", "11").ToJsonContent());
        var book = await createResponse.ReadFromJsonAsync<BookModel>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/books/{book!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        var getResponse = await _client.GetAsync($"/books/{book.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task GetNonExistentBook_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/books/99999");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task UpdateNonExistentBook_ReturnsNotFound()
    {
        var response = await _client.PutAsync("/books/99999", 
            new BookBody("52", "67", "69").ToJsonContent());
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteNonExistentBook_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/books/99999");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}