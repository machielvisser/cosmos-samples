using Microsoft.Azure.Cosmos;

namespace Shared
{
    public static class QueryableExtensions
    {
       public static async Task<(IEnumerable<T> Results, double Charge)> Execute<T>(this Container container, string query, string? partition = default)
       {
            var requestOptions = new QueryRequestOptions { PartitionKey = partition is null ? null : new PartitionKey(partition) };
            var iterator = container
                .GetItemQueryIterator<T>(new QueryDefinition(query), requestOptions: requestOptions);

            var results = new List<T>();
            var charge = 0d;

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();

                results.AddRange(response.Resource);
                charge += response.RequestCharge;
            }

            return (results, charge);
        }
    }
}
