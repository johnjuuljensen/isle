﻿using System.Runtime.CompilerServices;

namespace Isle.Extensions.Logging.Caching;

internal sealed class LiteralNode : Node
{
    public LiteralNode(Node parent, string rawLiteral) : base(parent)
    {
        RawLiteral = rawLiteral;

        var rawLiteralSpan = rawLiteral.AsSpan();
        int braceIndex = rawLiteralSpan.IndexOfAny('{', '}');
        EscapedLiteral = braceIndex >= 0 ? BuildEscapedString(braceIndex, rawLiteralSpan) : rawLiteral;
    }

    public string RawLiteral { get; }

    public string EscapedLiteral { get; }

    [SkipLocalsInit]
    private static string BuildEscapedString(int firstBraceIndex, in ReadOnlySpan<char> strSpan)
    {
        int remainingLength = strSpan.Length - firstBraceIndex;
        // Assume that the brace count does not exceed 12.5%.
        int builderInitialCapacity = firstBraceIndex + remainingLength + Math.Max(remainingLength >> 3, 4);
        var builder = builderInitialCapacity <= 1024
            ? new ValueStringBuilder(stackalloc char[builderInitialCapacity])
            : new ValueStringBuilder(builderInitialCapacity);
        builder.Append(strSpan.Slice(0, firstBraceIndex));

        int start = firstBraceIndex;
        for (int end = firstBraceIndex; end < strSpan.Length; end++)
        {
            var c = strSpan[end];
            if (c is '{' or '}')
            {
                var length = end - start;
                if (length > 0)
                {
                    builder.Append(strSpan.Slice(start, length));
                }

                builder.Append(c);
                builder.Append(c);
                start = end + 1;
            }
        }

        if (start < strSpan.Length)
        {
            builder.Append(strSpan.Slice(start));
        }

        return builder.ToString();
    }
}