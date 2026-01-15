using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SlackRiverDotNet.Host.Models.Manager;

namespace SlackRiverDotNet.Host.Models.Services;

public class QuerySlackService(HttpClient httpClient, IOptions<SlackServiceOptions> options) : IQuerySlackService
{
    private readonly ConcurrentDictionary<string, SlackUser> _users = new();
    private static readonly SlackUser DefaultUser = new("Alice", "Alice");
    
    public async IAsyncEnumerable<SlackMessage> GetMessagesAsync(DateTimeOffset start)
    {
        var orderString = start.ToUnixTimeSeconds().ToString();
        while (true)
        {
            var messages = await GetSlackMessagesFromApiAsync(orderString);

            if (messages is not null && messages.Any())
            {
                foreach (var m in messages)
                {
                    var replacedText = await ReplaceMentionToSlackUserNameAsync(m.Text);

                    yield return new SlackMessage(DefaultUser, replacedText, ToDateTimeOffset(m.Timestamp));
                }

                orderString = messages[0].Timestamp;
            }
            
            await Task.Delay(options.Value.ApiIntervalSeconds * 1000);
        }

        static DateTimeOffset ToDateTimeOffset(string slackUnixTimeStrings)
            => DateTimeOffset.FromUnixTimeSeconds(long.Parse(slackUnixTimeStrings.Split('.')[0]));
    }

    private async Task<IList<ConversationsHistoryMessage>?> GetSlackMessagesFromApiAsync(string orderString)
    {
        var response = await httpClient.GetAsync($"conversations.history?channel={options.Value.SlackChannelId}&limit=10&oldest={orderString}");
        var typedResponse = await JsonSerializer.DeserializeAsync<ConversationsHistoryApiResponse>(await response.Content.ReadAsStreamAsync());

        if (typedResponse?.Ok is not true)
            return null;

        return typedResponse.Messages;
    }

    private async ValueTask<string> ReplaceMentionToSlackUserNameAsync(string text)
    {
        var result = text;
        foreach (Match match in SlackMentionRegex.MentionPattern().Matches(text))
        {
            var userId = match.Groups[1].Value;
            var user = await GetSlackUserAsync(userId);

            if(!string.IsNullOrWhiteSpace(user?.DisplayName))
                result = result.Replace(match.Groups[0].Value, '@' + user.DisplayName + ' ');
            else if (!string.IsNullOrWhiteSpace(user?.Name))
                result = result.Replace(match.Groups[0].Value, '@' + user.Name + ' ');
        }

        return result;
    }
    
    private async ValueTask<SlackUser?> GetSlackUserAsync(string slackUserId)
    {
        if (_users.TryGetValue(slackUserId, out var user))
            return user;
        
        var response = await httpClient.GetAsync($"users.info?user={slackUserId}");
        var typedResponse = await JsonSerializer.DeserializeAsync<UsersInfoApiResponse>(await response.Content.ReadAsStreamAsync());

        if (typedResponse?.Ok is not true)
            return null;

        var newUser = new SlackUser(typedResponse.User!.Profile.DisplayName, typedResponse.User!.Profile.RealName);
        
        _users.TryAdd(slackUserId, newUser);
        return newUser;
    }
}

public interface IQuerySlackService
{
    IAsyncEnumerable<SlackMessage> GetMessagesAsync(DateTimeOffset start);
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
    
    [JsonPropertyName("subtype")]
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