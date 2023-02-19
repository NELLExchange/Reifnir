﻿using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Nellebot.Services;

public class PuppeteerFactory
{
    public async Task<IBrowser> BuildBrowser()
    {
        string downloadPath = Path.Combine(Path.GetTempPath(), $"puppeteer-nellebot");

        var browserFetcherOptions = new BrowserFetcherOptions { Path = downloadPath };
        var browserFetcher = new BrowserFetcher(browserFetcherOptions);
        await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        var executableDirectory = Directory.EnumerateDirectories(
                                     Directory.EnumerateDirectories(downloadPath)
                                    .First())
                                .First();

        var executableFilename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                ? "chrome.exe"
                                : "chrome";

        var executablePath = Path.Combine(executableDirectory, executableFilename);

        var options = new LaunchOptions { Headless = true, ExecutablePath = executablePath };

        var browser = await Puppeteer.LaunchAsync(options);

        return browser;
    }
}
