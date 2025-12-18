// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace avallama.Utilities;

// TODO: unit testing

/// <summary>
/// Provides utility methods for search operations, including fuzzy matching and relevance scoring algorithms.
/// </summary>
public static class SearchUtilities
{
    // Scoring weights definitions for easy tuning
    private const int PrefixMatchScore = 100;
    private const int ContainsMatchScore = 40;
    private const int MaxFuzzyScore = 30;

    /// <summary>
    /// Calculates a relevance score for a given text against a search query.
    /// Higher values indicate a better match.
    /// </summary>
    /// <param name="text">The text to search within (e.g., a model name).</param>
    /// <param name="searchQuery">The search term provided by the user.</param>
    /// <returns>
    /// A numeric score representing the match quality.
    /// Returns 0 if either the text or the query is empty.
    /// </returns>
    /// <remarks>
    /// The scoring logic is prioritized as follows:
    /// <list type="bullet">
    /// <item><description>Prefix match: Highest priority (+100 points).</description></item>
    /// <item><description>Substring match: Medium priority (+40 points).</description></item>
    /// <item><description>Fuzzy match (Levenshtein): Low priority (0-30 points based on distance).</description></item>
    /// </list>
    /// </remarks>
    public static int CalculateMatchScore(string? text, string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery) || string.IsNullOrWhiteSpace(text))
            return 0;

        // Normalize inputs to ensure case-insensitive comparison
        var normalizedText = text.ToLowerInvariant();
        var normalizedSearch = searchQuery.ToLowerInvariant();

        var score = 0;

        // 1. Prefix Match (Starts With) - Strongest indicator
        if (normalizedText.StartsWith(normalizedSearch, StringComparison.Ordinal))
        {
            score += PrefixMatchScore;
        }

        // 2. Substring Match (Contains) - Medium indicator
        // Note: We check this independently so a prefix match gets both scores (140),
        // prioritizing it above a simple internal match (40).
        if (normalizedText.Contains(normalizedSearch, StringComparison.Ordinal))
        {
            score += ContainsMatchScore;
        }

        // 3. Fuzzy Match (Levenshtein Distance) - For typos
        // Only calculate fuzzy score if it's not a perfect prefix match to save performance,
        // or keep it to distinguish between "llama2" and "llama3" when searching "llama".
        // Here we calculate it always to provide granular ranking.
        var distance = CalculateLevenshteinDistance(normalizedText, normalizedSearch);

        // The closer the distance (0), the higher the score.
        // If distance > MaxFuzzyScore, we add 0.
        var fuzzyScore = Math.Max(0, MaxFuzzyScore - distance);
        score += fuzzyScore;

        return score;
    }

    /// <summary>
    /// Calculates the Levenshtein edit distance between two strings using a memory-optimized algorithm.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string to compare against.</param>
    /// <returns>The minimum number of single-character edits (insertions, deletions or substitutions) required to change source into target.</returns>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        // Optimization: Ensure the inner loop iterates over the shorter string to minimize memory usage for the row arrays.
        if (source.Length > target.Length)
        {
            (source, target) = (target, source);
        }

        var currentRow = new int[target.Length + 1];
        var previousRow = new int[target.Length + 1];

        // Initialize the previous row (0, 1, 2, ..., m)
        for (var i = 0; i <= target.Length; i++)
        {
            previousRow[i] = i;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= target.Length; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,    // Insertion
                        previousRow[j] + 1),      // Deletion
                    previousRow[j - 1] + cost);   // Substitution
            }

            // Swap rows for the next iteration to reuse arrays without allocation
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[target.Length];
    }
}
