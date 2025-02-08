using System.IO;

namespace Nellebot.Services.HtmlToImage;

public record GenerateImageFileResult(string ImageFileName, FileStream ImageFileStream, FileStream HtmlFileStream);
