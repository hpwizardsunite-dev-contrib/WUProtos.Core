#addin nuget:?package=Cake.Git

var gitRepository = "https://github.com/Furtif/WUProtos.git";
var branch = EnvironmentVariable("WUPROTOS_TAG") ?? "master";

var dirProtos = "./WUProtos";
var dirTools = "./tools";
var dirSource = "./src";
var dirSourceCopy = "./srcCopy";

Information(branch);

Task("Clean").Does(() => {
    if (DirectoryExists(dirProtos)) {
        DeleteDirectory(dirProtos, true);
    }

    if (DirectoryExists(dirSourceCopy)) {
        DeleteDirectory(dirSourceCopy, true);
    }
});

Task("Copy").Does(() => {
    CopyDirectory(dirSource, dirSourceCopy);
});

Task("WUProtos-Tools").Does(() => {
    NuGetInstall("Google.Protobuf.Tools", new NuGetInstallSettings {
        ExcludeVersion = true,
        OutputDirectory = dirTools,
        Version = "3.8.0-rc.1"
    });
});

Task("WUProtos-Clone").Does(() => {
    Information("Cloning branch '" + branch + "'...");

    StartProcess("git.exe", new ProcessSettings()
        .WithArguments(args => 
            args.Append("clone")
                .Append("--quiet")                   
                .Append("--branch")
                .AppendQuoted(branch)
                .Append(gitRepository)
                .Append("WUProtos")));
});

Task("WUProtos-Compile").Does(() => {
    StartProcess("python.exe", new ProcessSettings()
        .WithArguments(args => 
            args.AppendQuoted(System.IO.Path.GetFullPath(dirProtos + "/compile.py"))
                .Append("-p")
                .AppendQuoted(System.IO.Path.GetFullPath(dirTools + "/Google.Protobuf.Tools/tools/windows_x64/protoc.exe"))
                .Append("-o")
                .AppendQuoted(System.IO.Path.GetFullPath(dirProtos + "/out"))
                .Append("csharp")));
});

Task("WUProtos-Move").Does(() => {
    CopyDirectory(dirProtos + "/out/WUProtos", dirSourceCopy + "/WUProtos.Core");
});

Task("Version").Does(() =>
{
    // Read version
    var version = System.IO.File.ReadAllText(dirProtos + "/.current-version");
    // Fix version
    version = System.Text.RegularExpressions.Regex.Replace(version, @"\s+", string.Empty);

    // Apply version
    var projectFile = dirSourceCopy + "/WUProtos.Core/WUProtos.Core.csproj";
    var updatedProjectFile = System.IO.File
        .ReadAllText(projectFile)
        .Replace("<Version>1.0.0-rc</Version>", "<Version>" + version + "</Version>");

    System.IO.File.WriteAllText(projectFile, updatedProjectFile);

    Information("Applied version '" + version + "' to '" + projectFile + "'.");
});

Task("Default")
  .IsDependentOn("Clean")
  .IsDependentOn("Copy")
  .IsDependentOn("WUProtos-Tools")
  .IsDependentOn("WUProtos-Clone")
  .IsDependentOn("WUProtos-Compile")
  .IsDependentOn("WUProtos-Move")
  .IsDependentOn("Version")
  .Does(() =>
{
  DotNetCoreRestore(dirSourceCopy + "/WUProtos.Core");
  DotNetCorePack(dirSourceCopy + "/WUProtos.Core", new DotNetCorePackSettings {
      Configuration = "Release"
  });
});

RunTarget("Default");
