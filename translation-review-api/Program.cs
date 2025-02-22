using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using translation_review_api;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Configuration
builder.Services.Configure<MyDbSettings>(builder.Configuration.GetSection("MongoDB"));

MongoUtility.RegisterConventions();
var settings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<MyDbSettings>>().Value;
var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") ?? settings.ConnectionString;
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("MONGODB_CONNECTION_STRING environment variable is not set.");
    try
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        // perform a ping test to ensure the connection is successful
        database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();

        return database;
    }
    catch (MongoConnectionException ex)
    {
        // Log the exception and provide a meaningful error message
        Console.WriteLine($"Error connecting to MongoDB {settings.ConnectionString}: {ex.Message}");
        throw new ApplicationException("Failed to connect to the MongoDB server. Please check your connection settings.", ex);
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine($"Connection to MongoDB {settings.ConnectionString} timed out: {ex.Message}");
        throw new ApplicationException("MongoDB connection timed out. Please check if the server is running and accessible.", ex);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred while connecting to MongoDB: {ex.Message}");
        throw new ApplicationException("An unexpected error occurred while connecting to the MongoDB server.", ex);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapPost("/save", async (HttpContext context, IMongoDatabase database) =>
{
    try
    {
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("ReviewedQuestions");

        var reviewData = await context.Request.ReadFromJsonAsync<ReviewData>();
        if (reviewData == null || string.IsNullOrEmpty(reviewData.Username) || reviewData.Reviews == null || !reviewData.Reviews.Any())
        {
            return Results.BadRequest(new { Message = "Invalid data. Username and reviews are required." });
        }

        var filter = Builders<BsonDocument>.Filter.Eq("Username", reviewData.Username);
        var update = Builders<BsonDocument>.Update
            .SetOnInsert("_id", ObjectId.GenerateNewId())  // Ensure a new document gets an ObjectId if inserted
            .Set("Username", reviewData.Username)
            .Set("Reviews", BsonArray.Create(reviewData.Reviews.Select(r => r.ToBsonDocument())))
            .Set("LastModified", DateTime.UtcNow)
            .Set("LastReviewedIndex", reviewData.LastReviewedIndex);

        var updateResult = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

        if (updateResult.MatchedCount == 0)
        {
            return Results.Created($"/save/{reviewData.Username}", new { Message = "New review record created." });
        }

        return Results.Ok(new { Message = "Reviews updated successfully." });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error saving reviews: {ex.Message}");
        return Results.Problem("An internal server error occurred. Error:" + ex.Message);
    }
});

app.MapGet("/reviews/{username}", async (string username, IMongoDatabase database) =>
{
    try
    {
        var collection = database.GetCollection<BsonDocument>("ReviewedQuestions");

        var reviewData = await collection.Find(Builders<BsonDocument>.Filter.Eq("Username", username))
                                 .FirstOrDefaultAsync();

        if (reviewData == null)
        {
            return Results.NotFound(new { Message = "No reviewed data found for this username." });
        }
        var reviewsArray = reviewData.GetValue("Reviews", new BsonArray()).AsBsonArray;
        var lastReviewedIndex = reviewData.GetValue("LastReviewedIndex", 0).AsInt32;
        var reviews = BsonSerializer.Deserialize<List<ReviewedQuestion>>(reviewsArray.ToJson());

        return Results.Json(new { lastReviewedIndex, reviews });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error retrieving reviews: {ex.Message}");
        return Results.Problem("An internal server error occurred. Error: " + ex.Message);
    }
});

app.MapPost("/login", async (LoginRequest request, IMongoDatabase database) =>
{
    var usersCollection = database.GetCollection<User>("ReviewUsers");

    var user = await usersCollection.Find(u => u.Username == request.Username).FirstOrDefaultAsync();

    if (user == null || user.Password != request.Password) // Use hashed password comparison in production
    {
        return Results.BadRequest(new { Message = "Invalid username or password" });
    }

    if (user.Language != "all" && user.Language != request.SelectedLanguage)
    {
        return Results.BadRequest(new { message = "You can only select your assigned language!" });
    }

    return Results.Ok(new { username = user.Username, selectedLanguage = request.SelectedLanguage });
});

var version = builder.Configuration.GetValue<string>("ApiVersion");
app.MapGet("/hello", () => $"Hello World! v{version}");

app.Run();
