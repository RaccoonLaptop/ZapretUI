using Xunit;
using ZapretUI.Services;

namespace ZapretUI.Tests;

public class StrategyBatParserTests
{
    [Fact]
    public void Parse_substitutes_bin_lists_and_disabled_game_filter()
    {
        var root = Path.Combine(Path.GetTempPath(), "zapretui-parser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        Directory.CreateDirectory(Path.Combine(root, "lists"));
        Directory.CreateDirectory(Path.Combine(root, "utils"));
        File.WriteAllText(Path.Combine(root, "service.bat"), "@echo off\r\n");
        File.WriteAllText(Path.Combine(root, "general.bat"),
            "start \"zapret\" /min \"%BIN%winws.exe\" --wf-tcp=80 --filter-udp=%GameFilterUDP% --hostlist=\"%LISTS%list-general.txt\"\r\n");

        try
        {
            var args = StrategyBatParser.Parse(new ZapretPaths(root), "general.bat");
            Assert.True(args.Contains("list-general.txt", StringComparison.OrdinalIgnoreCase), args);
            Assert.Contains("--filter-udp=12", args, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--wf-tcp=80", args, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("%BIN%", args, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("%LISTS%", args, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("%GameFilterUDP%", args, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Parse_returns_empty_when_strategy_file_is_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "zapretui-parser-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        File.WriteAllText(Path.Combine(root, "service.bat"), "@echo off\r\n");

        try
        {
            Assert.Equal("", StrategyBatParser.Parse(new ZapretPaths(root), "missing.bat"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
