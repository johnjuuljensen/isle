﻿using System.Runtime.CompilerServices;

namespace Isle.Extensions.Logging.Caching;

internal sealed class LiteralNode : Node
{
    public LiteralNode(Node parent, string rawLiteral) : base(parent)
    {
        RawLiteral = rawLiteral;
        EscapedLiteral = LiteralUtils.EscapeLiteral(rawLiteral);
    }

    public string RawLiteral { get; }

    public string EscapedLiteral { get; }
}