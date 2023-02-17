using System;
using System.IO;

var targetPath = args[0];
var publishDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

ReadOnlySpan<char> GetFileNameSegmentBeforeDot(ReadOnlySpan<char> fileName)
{
    var dotIndex = fileName.IndexOf('.');
    if (dotIndex != -1)
        return fileName[.. dotIndex];
    return fileName;
}

Directory.CreateDirectory(targetPath);

foreach (var file in Directory.EnumerateFiles(publishDirectory))
{
    var fileName = file.AsSpan()[(publishDirectory.Length + 1) ..];
    var startSegment = GetFileNameSegmentBeforeDot(fileName);
    if (startSegment is "fetch" or "copy")
        continue;

    var destPath = Path.Join(targetPath, fileName);
    File.Copy(file, destPath, overwrite: true);
}