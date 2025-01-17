﻿using System.Reflection;
using BenchmarkDotNet.Attributes;
using Isle.Configuration;
using Isle.Extensions.Logging.Benchmarks.Serilog;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Isle.Extensions.Logging.Benchmarks;

[MemoryDiagnoser]
public class SerilogBenchmark
{
    private ILoggerFactory _loggerFactory = null!;
    private ILogger<MELBenchmarks> _logger = null!;

    private static readonly Rect Rect = new(0, 0, 3, 4);
    private static readonly int Area = Rect.Width * Rect.Height;
    private static readonly int Perimeter = 2 * (Rect.Width + Rect.Height);

    [ParamsAllValues]
    public bool IsEnabled { get; set; } = true;

    [ParamsAllValues]
    public bool EnableCaching { get; set; }

    [GlobalSetup(Target = nameof(Standard))]
    public void GlobalSetupStandard()
    {
        GlobalSetup();
    }

    [GlobalSetup(Target = nameof(InterpolatedWithManualDestructuring))]
    public void GlobalSetupWithManualDestructuring()
    {
        GlobalSetup();
        IsleConfiguration.Configure(builder => builder
            .ConfigureExtensionsLogging(cfg => cfg.EnableMessageTemplateCaching = EnableCaching));
    }

    [GlobalSetup(Targets = new[] { nameof(InterpolatedWithExplicitAutomaticDestructuring), nameof(InterpolatedWithImplicitAutomaticDestructuring) })]
    public void GlobalSetupWithAutoDestructuring()
    {
        GlobalSetup();
        IsleConfiguration.Configure(builder => builder.WithAutomaticDestructuring()
            .ConfigureExtensionsLogging(cfg => cfg.EnableMessageTemplateCaching = EnableCaching));
    }

    private void GlobalSetup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            var minLogLevel = IsEnabled ? LogLevel.Information : LogLevel.Error;
            builder
                .SetMinimumLevel(minLogLevel)
                .AddSerilog(new LoggerConfiguration()
                    .MinimumLevel.Is(IsEnabled ? LogEventLevel.Information : LogEventLevel.Error)
                    .WriteTo.BenchmarkSink()
                    .CreateLogger(), true);
        });
        _logger = _loggerFactory.CreateLogger<MELBenchmarks>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        IsleConfiguration.Reset();
        _loggerFactory.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Standard()
    {
        _logger.LogInformation("The area of rectangle {@Rect} is Width * Height = {Area} and its perimeter is 2 * (Width + Height) = {Perimeter}.", Rect, Area, Perimeter);
    }

    [Benchmark]
    public void InterpolatedWithManualDestructuring()
    {
        _logger.LogInformation($"The area of rectangle {@Rect} is Width * Height = {Area} and its perimeter is 2 * (Width + Height) = {Perimeter}.");
    }

    [Benchmark]
    public void InterpolatedWithExplicitAutomaticDestructuring()
    {
        _logger.LogInformation($"The area of rectangle {@Rect} is Width * Height = {Area} and its perimeter is 2 * (Width + Height) = {Perimeter}.");
    }

    [Benchmark]
    public void InterpolatedWithImplicitAutomaticDestructuring()
    {
        _logger.LogInformation($"The area of rectangle {Rect} is Width * Height = {Area} and its perimeter is 2 * (Width + Height) = {Perimeter}.");
    }
}