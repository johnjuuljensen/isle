﻿using System.Buffers;

namespace Isle.Extensions.Logging.Caching;

internal sealed class TemplateNode
{
    public TemplateNode(Node parent)
    {
        if (parent.Depth == 0)
        {
            Segments = Array.Empty<Segment>();
            MessageTemplate = string.Empty;
            return;
        }

        var nodes = ArrayPool<Node>.Shared.Rent(parent.Depth);
        var node = parent;
        while (node.Depth > 0)
        {
            nodes[node.Depth - 1] = node;
            node = node.Parent!;
        }

        var builder = StringBuilderCache.Acquire(StringBuilderCache.MaxBuilderSize);
        var segments = new Segment[parent.Depth];
        for (int i = 0; i < parent.Depth; i++)
        {
            node = nodes[i];
            switch (node)
            {
                case FormatNode formatNode:
                    segments[i] = new Segment(formatNode.Format, formatNode.Alignment);
                    builder.Append('{');
                    builder.Append(formatNode.Name);
                    if (formatNode.Alignment != 0)
                    {
                        builder.Append(',');
                        builder.Append(formatNode.Alignment);
                    }
                    if (!string.IsNullOrEmpty(formatNode.Format))
                    {
                        builder.Append(':');
                        builder.Append(formatNode.Format);
                    }
                    builder.Append('}');
                    break;
                case LiteralNode literalNode:
                    segments[i] = new Segment(literalNode.RawLiteral);
                    builder.Append(literalNode.EscapedLiteral);
                    break;
            }
        }

        Segments = segments;
        MessageTemplate = StringBuilderCache.GetStringAndRelease(builder);
        ArrayPool<Node>.Shared.Return(nodes);
    }

    public Segment[] Segments { get; }

    public string MessageTemplate { get; }
}