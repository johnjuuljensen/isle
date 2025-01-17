﻿using System.Runtime.CompilerServices;
using System.Text;

namespace Isle.Extensions.Logging;

internal sealed class SimpleFormattedLogValuesBuilder : FormattedLogValuesBuilder
{
    private StringBuilder _originalFormatBuilder = null!;
    private FormattedLogValuesBase _formattedLogValues = null!;
    private Segment[] _segments = null!;
    private List<string> _literalList = null!;
    private int _valueIndex = 0;
    private int _segmentIndex = 0;

    public override bool IsCaching => false;

    protected override void Initialize(int literalLength, int formattedCount)
    {
        _originalFormatBuilder = StringBuilderCache.Acquire(Math.Max(literalLength + formattedCount * 16, StringBuilderCache.MaxBuilderSize));
        _formattedLogValues = FormattedLogValuesBase.Create(formattedCount);
        _segments = new Segment[formattedCount * 2 + 1];
    }
    
    protected override FormattedLogValuesBase BuildAndReset()
    {
        var result = _formattedLogValues;
        result.Values[_valueIndex] = new KeyValuePair<string, object?>(OriginalFormatName, StringBuilderCache.GetStringAndRelease(_originalFormatBuilder));
        result.Count = _valueIndex + 1;
        result.SetSegments(_segments.AsMemory(0, _segmentIndex));
        
        _formattedLogValues = null!;
        _originalFormatBuilder = null!;
        _segments = null!;
        _literalList = null!;
        _segmentIndex = 0;
        _valueIndex = 0;

        return result;
    }

    public override void AppendLiteral(string? str)
    {
        var start = _originalFormatBuilder.Length;
        _originalFormatBuilder.EscapeAndAppend(str);
        var end = _originalFormatBuilder.Length;
        var length = end - start;
        if (length > 0)
        {
            if (_segmentIndex > 0)
            {
                ref var prevSegment = ref _segments[_segmentIndex - 1];
                switch (prevSegment.Type)
                {
                    case Segment.SegmentType.Literal:
                    {
                        prevSegment = new Segment(prevSegment, str!, _literalList ??= new List<string>());
                        return;
                    }
                    case Segment.SegmentType.LiteralList:
                    {
                        prevSegment = new Segment(prevSegment, str!);
                        return;
                    }
                }
            }

            _segments[_segmentIndex++] = new Segment(str!);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void AppendFormatted(string name, object? value, int alignment = 0, string? format = null)
    {
        _originalFormatBuilder.Append('{');

        _originalFormatBuilder.Append(name);

        if (alignment != 0)
        {
            _originalFormatBuilder.Append(',');
            _originalFormatBuilder.Append(alignment);
        }

        if (!string.IsNullOrEmpty(format))
        {

            _originalFormatBuilder.Append(':');
            _originalFormatBuilder.Append(format);
        }

        _originalFormatBuilder.Append('}');
        _segments[_segmentIndex++] = new Segment(format, alignment);
        _formattedLogValues.Values[_valueIndex++] = new(name, value);
    }
}