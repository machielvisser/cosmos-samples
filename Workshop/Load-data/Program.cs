﻿using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Shared.Settings;

try
{
    var settings = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .AddJsonFile("secrets.json", true)
        .AddUserSecrets<Program>()
        .Build()
        .GetSection(nameof(Cosmos))
        .Get<Cosmos>();

    // Connection
    var cosmosClient = new CosmosClient(settings.EndpointUrl, settings.AuthorizationKey, new CosmosClientOptions() { AllowBulkExecution = true });

    // Create database
    var database = (await cosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName)).Database;


    // Create containers (index configuration: https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.database.definecontainer?view=azure-dotnet)
    var usersContainer = (await database.CreateContainerIfNotExistsAsync(new ContainerProperties("users", "/id"))).Container;
    var postsContainer = (await database.CreateContainerIfNotExistsAsync(new ContainerProperties("posts", "/postId"))).Container;

    // Define data (using Bogus: https://github.com/bchavez/Bogus)
    var lorem = new Bogus.DataSets.Lorem("en");

    var users = new Bogus
        .Faker<User>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.username, f => f.Internet.UserName())
        .Generate(100);
    await InsertData(usersContainer, users, user => user.id.ToString());

    var posts = new Bogus
        .Faker<Post>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.userId, f => f.PickRandom(users).id)
        .RuleFor(o => o.title, f => lorem.Sentence())
        .RuleFor(o => o.content, f => lorem.Paragraphs(5))
        .RuleFor(o => o.creationDate, f => DateTime.UtcNow)
        .Generate(500);
    await InsertData(postsContainer, posts, post => post.id.ToString());

    var comments = new Bogus
        .Faker<Comment>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.userId, f => f.PickRandom(users).id)
        .RuleFor(o => o.postId, f => f.PickRandom(posts).id)
        .RuleFor(o => o.content, f => lorem.Paragraphs(1))
        .RuleFor(o => o.creationDate, f => DateTime.UtcNow)
        .Generate(2000);
    await InsertData(postsContainer, comments, comment => comment.postId.ToString());

    var likes = new Bogus
        .Faker<Like>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.userId, f => f.PickRandom(users).id)
        .RuleFor(o => o.postId, f => f.PickRandom(posts).id)
        .RuleFor(o => o.creationDate, f => DateTime.UtcNow)
        .Generate(5000);
    await InsertData(postsContainer, likes, like => like.postId.ToString());
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

async Task InsertData<T>(Container container, IList<T> items, Func<T, string> partitionKeySelector)
{
    Console.WriteLine($"Starting adding {typeof(T).FullName}s...");

    await Task.WhenAll(items
        .Select(item => container
            .CreateItemAsync(item, new PartitionKey(partitionKeySelector(item)))
            .ContinueWith(itemResponse =>
            {
                if (!itemResponse.IsCompletedSuccessfully)
                {
                    var innerExceptions = itemResponse.Exception.Flatten();
                    if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                        Console.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                    else
                        Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                }
            })));

    Console.WriteLine($"Finished writing {items.Count} {typeof(T).FullName}s.");
}