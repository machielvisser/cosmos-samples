using Microsoft.Azure.Cosmos;
using Shared;

try
{
    // Connection
    var cosmosClient = new CosmosClient(Settings.EndpointUrl, Settings.AuthorizationKey, new CosmosClientOptions() { AllowBulkExecution = true });

    // Create database and containers
    var database = (await cosmosClient.CreateDatabaseIfNotExistsAsync(Settings.DatabaseName)).Database;

    var usersContainer = (await database.CreateContainerIfNotExistsAsync(new ContainerProperties("users", "/id"))).Container;
    var postsContainer = (await database.CreateContainerIfNotExistsAsync(new ContainerProperties("posts", "/id"))).Container;

    // Define data
    var users = new Bogus
        .Faker<User>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.username, f => f.Internet.UserName())
        .Generate(500);

    await InsertData(usersContainer, users, user => user.id.ToString());

    var lorem = new Bogus.DataSets.Lorem("en");
    var posts = new Bogus
        .Faker<Post>()
        .RuleFor(o => o.id, f => Guid.NewGuid())
        .RuleFor(o => o.userId, f => f.PickRandom(users).id)
        .RuleFor(o => o.content, f => lorem.Paragraphs(5))
        .Generate(5000);

    await InsertData(postsContainer, posts, post => post.id.ToString());
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