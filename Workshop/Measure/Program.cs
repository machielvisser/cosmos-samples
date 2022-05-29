using Microsoft.Azure.Cosmos;
using Shared;
using System.Diagnostics;

try
{
    // Connection
    var cosmosClient = new CosmosClient(Settings.EndpointUrl, Settings.AuthorizationKey, new CosmosClientOptions() { AllowBulkExecution = true });

    // Get database and container references
    var database = cosmosClient.GetDatabase(Settings.DatabaseName);
    var usersContainer = database.GetContainer("users");
    var postsContainer = database.GetContainer("posts");

    // Measure queries
    var totalCharge = 0d;
    var stopwatch = Stopwatch.StartNew();

    var posts = await postsContainer.Execute<Post>("select top 100 * from x where x.type = 'post' order by x.creationDate desc");
    totalCharge += posts.Charge;

    foreach (var post in posts.Results)
    {
        var (comments, commentCharge) = await postsContainer
            .Execute<Comment>($"select count(1) from x where x.postId = '{post.postId}' and x.type = 'comment'", post.id.ToString());
        totalCharge += commentCharge;

        var (likes, likesCharge) = await postsContainer
            .Execute<Like>($"select count(1) from x where x.postId = '{post.postId}' and x.type = 'like'", post.id.ToString());
        totalCharge += likesCharge;

        var (users, usersCharge) = await postsContainer
            .Execute<Like>($"select x.username from x where x.id = '{post.userId}'", post.id.ToString());
        totalCharge += usersCharge;
    }

    stopwatch.Stop();

    Console.WriteLine($"Total request charge: {totalCharge} in {stopwatch.Elapsed}");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}