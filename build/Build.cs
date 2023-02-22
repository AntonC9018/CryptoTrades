using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Unity;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Restore);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Setup => _ => _
        .Executes(() =>
        {
            RestoreUnityPackages();
            CreateUnitySolution();
            SlnGenCreateSolution();
        });
    
    Target CreateSolution => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            CreateUnitySolution();
            SlnGenCreateSolution();
        });
    
    Target Restore => _ => _
        .Executes(() =>
        {
            RestoreUnityPackages();
        });

    Target OpenUnity => _ => _
        .Executes(() =>
        {
            OpenUnityEditor();
        });
    
    Target UnityBuildWindows => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            RunUnityBuildWindows();
        });

    AbsolutePath UnityProjectFolder => RootDirectory / "CryptoTrades";
    AbsolutePath GeneratedUnityCsProj => UnityProjectFolder / "Assembly-CSharp.csproj";
    AbsolutePath UnityPackageRestoreDestination => UnityProjectFolder / "Assets" / "lib";
    AbsolutePath ToolsDirectory => RootDirectory / "tools";
    AbsolutePath CliToolsDirectory => ToolsDirectory / "cli";
    AbsolutePath PackageInstallToolDirectory => CliToolsDirectory / "package_install";
    AbsolutePath UnityProjectVersionFile => UnityProjectFolder / "ProjectSettings" / "ProjectVersion.txt";
    AbsolutePath NukeBuildDirectory => RootDirectory / "build";
    AbsolutePath NukeCsProjFile => NukeBuildDirectory / "_build.csproj";
    AbsolutePath ArtefactsDirectory => RootDirectory / "artefacts";
    AbsolutePath UnityWindowsBuildDirectory => ArtefactsDirectory / "unity-windows";

    [Parameter] [CanBeNull] readonly AbsolutePath UnityEditorPath;
    AbsolutePath _cachedEditorPath;
    
    [PackageExecutable(
        packageId: "Microsoft.VisualStudio.SlnGen.Tool",
        packageExecutable: "slngen.exe",
        // Must be set for tools shipping multiple versions
        Framework = "net7.0")]
    readonly Tool SlnGen;
    
    IReadOnlyCollection<Output> RestoreUnityPackages()
    {
        return DotNetRun(_ => _
            .SetProjectFile(PackageInstallToolDirectory / "copy" / "copy.csproj")
            .SetConfiguration("Release")
            .SetApplicationArguments(UnityPackageRestoreDestination));
    }
    
    public static string GetProgramFiles()
    {
        return EnvironmentInfo.SpecialFolder(
            EnvironmentInfo.Is32Bit
                ? SpecialFolders.ProgramFilesX86
                : SpecialFolders.ProgramFiles);
    }
    
    public static string GetToolPathViaHubVersion(string version)
    {
        return EnvironmentInfo.Platform switch
        {
            PlatformFamily.Windows => $@"{GetProgramFiles()}\Unity\Hub\Editor\{version}\Editor\Unity.exe",
            PlatformFamily.OSX => $"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity",
            _ => null,
        };
    }

    private string GetUnityEditorPath()
    {
        if (_cachedEditorPath is not null)
            return _cachedEditorPath;
        
        var editorPath = UnityEditorPath;
        if (editorPath is not null)
        {
            if (File.Exists(editorPath))
                return _cachedEditorPath = editorPath;
            throw new Exception($"Unity editor path passed in is wrong. The path is {editorPath}");
        }

        var editorVersion = File
            .ReadAllLines(UnityProjectVersionFile)
            .Select(l =>
            {
                int colonIndex = l.IndexOf(":", StringComparison.Ordinal);
                if (colonIndex == -1)
                    return default;
                return (name: l[.. colonIndex].Trim(), value: l[(colonIndex + 1) ..].Trim());
            })
            .First(t => t.name == "m_EditorVersion")
            .value;
        var defaultPath = GetToolPathViaHubVersion(editorVersion);
        if (defaultPath is not null && File.Exists(defaultPath))
            return _cachedEditorPath = (AbsolutePath) defaultPath;
        throw new Exception("The right unity editor version has not been found in the default location. Pass it as a parameter.");
    }

    IProcess Unity(Arguments args)
    {
        var unityEditorPath = GetUnityEditorPath();
        args.Add("-projectPath {value}", UnityProjectFolder);
        return ProcessTasks.StartProcess(unityEditorPath, args.RenderForExecution());
    }
    
    void CreateUnitySolution()
    {
        if (GeneratedUnityCsProj.Exists())
            return;
        var args = new Arguments();
        args.Add("-executeMethod {value}", "UnityEditor.SyncVS.SyncSolution");
        args.Add("-nographics");
        args.Add("-quit");
        args.Add("-batchmode");
        var p = Unity(args);
        p.WaitForExit();
        if (GeneratedUnityCsProj.Exists())
            return;

        Serilog.Log.Error("This task can fail if your Unity editor doesn't have the Visual Studio"
            + " or the Rider editor selected as the default editor. Change it in Project Settings > External Tools");
        throw new ProcessException(p);
    }

    string GetLastPathSegment(string p)
    {
        int separatorIndex = p.LastIndexOfAny(new[] {'\\', '/'});
        return p[(separatorIndex + 1) ..];
    }

    string RemoveBasePath(string from, string basePath)
    {
        if (from.StartsWith(basePath))
        {
            var s = from.AsSpan()[(basePath.Length + 1) ..];
            if (s.StartsWith("/") || s.StartsWith(@"\"))
                return s[1 ..].ToString();
            return s.ToString();
        }
        return from;
    }

    IReadOnlyCollection<Output> SlnGenCreateSolution()
    {
        return SlnGen(new Arguments()
            .Add(RemoveBasePath(GeneratedUnityCsProj, RootDirectory))
            .Add(RemoveBasePath(NukeCsProjFile, RootDirectory))
            .Add("--solutionfile {value}", RootDirectory / GetLastPathSegment(RootDirectory) + ".sln")
            .Add("--launch {value}", "false")
            .RenderForExecution());
    }
    
    IProcess OpenUnityEditor() => Unity(new());

    void RunUnityBuildWindows()
    {
        EnsureCleanDirectory(UnityWindowsBuildDirectory);
        
        var args = new Arguments();
        args.Add("-executeMethod {value}", "Build.BuildHelper.BuildWindows");
        args.Add("-nographics");
        args.Add("-quit");
        args.Add("-batchmode");
        Serilog.Log.Information("Building unity project into {path}", UnityWindowsBuildDirectory);
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = GetUnityEditorPath(),
            Arguments = args.RenderForExecution(),
            RedirectStandardInput = true,
        });
        p.StandardInput.WriteLine(UnityWindowsBuildDirectory);
        p.StandardInput.Flush();
        p.WaitForExit();
        
        if (p.ExitCode != 0)
            throw new Exception("Unity build failed");
        
        // zip the output
        AbsolutePath zipPath = ArtefactsDirectory / "CryptoTrades-Windows.zip";
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        
        ZipFile.CreateFromDirectory(UnityWindowsBuildDirectory, zipPath!);
        Serilog.Log.Information("Project built to {zipPath}", zipPath);
    }
}
