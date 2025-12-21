using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SlackRiverDotNet.Host.Models.Services;

public class QuerySlackService(HttpClient httpClient, IOptions<SlackServiceOptions> options, ILogger<QuerySlackService> logger) : IQuerySlackService
{
    private readonly ConcurrentDictionary<string, SlackUser> _users = new();
    private static readonly SlackUser DefaultUser = new("Alice", "Alice");
    
    public async IAsyncEnumerable<IList<SlackMessage>> GetMessagesAsync(DateTimeOffset start)
    {
        var orderString = start.ToUnixTimeSeconds().ToString();
        while (true)
        {
            var messages = await GetSlackMessagesFromApiAsync(orderString);

            if (messages is not null && messages.Any())
            {
                var slackMessages = messages
                    .Select(x => new SlackMessage(DefaultUser, x.Text, ToDateTimeOffset(x.Timestamp)))
                    .Reverse().ToList();

                yield return slackMessages;

                orderString = slackMessages[0].Timestamp.ToString();
            }
            
            await Task.Delay(options.Value.ApiIntervalSeconds * 1000);
        }

        static DateTimeOffset ToDateTimeOffset(string slackUnixTimeStrings)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(slackUnixTimeStrings.Split('.')[0]));
    }

    private async Task<IList<ConversationsHistoryMessage>?> GetSlackMessagesFromApiAsync(string orderString)
    {
        logger.LogInformation($"oldest = {orderString}");
        var response = await httpClient.GetAsync($"conversations.history?channel={options.Value.SlackChannelId}&limit=10&oldest={orderString}");
        var typedResponse = await JsonSerializer.DeserializeAsync<ConversationsHistoryApiResponse>(await response.Content.ReadAsStreamAsync());

        if (typedResponse?.Ok is not true)
            return null;

        return typedResponse.Messages;
    }
    
    private async ValueTask<SlackUser?> GetSlackUserAsync(string slackUserId)
    {
        if (_users.TryGetValue(slackUserId, out var user))
            return user;
        
        var response = await httpClient.GetAsync($"users.info?user={slackUserId}");
        var typedResponse = await JsonSerializer.DeserializeAsync<UsersInfoApiResponse>(await response.Content.ReadAsStreamAsync());

        if (typedResponse?.Ok is not true)
            return null;

        var newUser = new SlackUser(typedResponse.User!.Profile.DisplayName, typedResponse.User!.Profile.DisplayName);
        
        _users.TryAdd(slackUserId, newUser);
        return newUser;
    }
}

public interface IQuerySlackService
{
    IAsyncEnumerable<IList<SlackMessage>> GetMessagesAsync(DateTimeOffset start);
}

//一旦雑にモデルをここに置いておく
public class UsersInfoApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [MemberNotNullWhen(true, nameof(Ok))]
    [JsonPropertyName("user")]
    public UsersInfoUser? User { get; set; }
}

public class UsersInfoUser
{
    [JsonPropertyName("profile")]
    public UsersInfoUserProfile Profile { get; set; }
}

public class UsersInfoUserProfile
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }
    
    [JsonPropertyName("real_name")]
    public string RealName { get; set; }
}

public class ConversationsHistoryApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [MemberNotNullWhen(true, nameof(Ok))]
    [JsonPropertyName("messages")]
    public List<ConversationsHistoryMessage>? Messages { get; set; }
}

public class ConversationsHistoryMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("subType")]
    public string? SubType { get; set; }

    [JsonPropertyName("user")]
    public string User { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("ts")]
    public string Timestamp { get; set; }
}



public record SlackMessage(SlackUser User, string Content, DateTimeOffset Timestamp);

public record SlackUser(string Name, string DisplayName);

public record SlackServiceOptions
{
    public string SlackChannelId { get; init; }
    public int ApiIntervalSeconds { get; init; }
};