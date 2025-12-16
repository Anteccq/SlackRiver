using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlackRiverDotNet.Host.Models.Services;

public class QuerySlackService(HttpClient httpClient, SlackServiceOptions options) : IQuerySlackService
{
    private readonly ConcurrentDictionary<string, SlackUser> _users = new();
    
    public async IAsyncEnumerable<IEnumerable<SlackMessage>> GetMessagesAsync()
    {
        var defaultUser = new SlackUser(string.Empty, string.Empty);
        while (true)
        {
            var messages = await GetSlackMessagesFromApiAsync();

            if (messages is not null)
            {
                yield return messages
                    .Select(x => new SlackMessage(defaultUser, x.Text, DateTimeOffset.FromUnixTimeSeconds(long.Parse(x.Timestamp.Split('.')[0]))));
            }
            
            await Task.Delay(options.ApiIntervalSeconds * 1000);
        }
    }

    private async Task<IEnumerable<ConversationsHistoryMessage>?> GetSlackMessagesFromApiAsync()
    {
        var response = await httpClient.GetAsync($"conversations.history?channel={options.SlackChannelId}&limit=10");
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
    IAsyncEnumerable<IEnumerable<SlackMessage>> GetMessagesAsync();
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

public record SlackServiceOptions(string SlackChannelId, int ApiIntervalSeconds);