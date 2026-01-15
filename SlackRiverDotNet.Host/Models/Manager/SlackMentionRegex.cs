using System.Text.RegularExpressions;

namespace SlackRiverDotNet.Host.Models.Manager;

public partial class SlackMentionRegex
{
    [GeneratedRegex(@"<@([A-Z0-9]+)(?:\|[^>]*)?>")]
    internal static partial Regex MentionPattern();
}