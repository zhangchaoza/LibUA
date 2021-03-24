///////////////////////////////////////////////////////////////////////////////
// TOOLS / ADDINS
///////////////////////////////////////////////////////////////////////////////

#tool GitVersion.CommandLine&version=5.6.6
// #tool gitreleasemanager
// #tool xunit.runner.console
// #tool vswhere
#addin Cake.Figlet&version=2.0.0

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "All");
var verbosity = DotNetCoreVerbosity.Minimal;
var description = Argument("description", "Open-source OPC UA client and server library for .NET and .NET Core based on IEC 62541");

///////////////////////////////////////////////////////////////////////////////
// PREPARATION
///////////////////////////////////////////////////////////////////////////////

var repoName = "LibUA";
var isLocal = BuildSystem.IsLocalBuild;

// // Set build version
if (isLocal == false || verbosity >= DotNetCoreVerbosity.Normal)
{
    GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.BuildServer });
}
GitVersion gitVersion = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });

// var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var branchName = gitVersion.BranchName;
var isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", branchName);
var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("master", branchName);
var isTagged = AppVeyor.Environment.Repository.Tag.IsTag;
var shortSha = gitVersion.Sha.Substring(0,7);

// Directories and Paths
var solution = "./LibUA.sln";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    if (!IsRunningOnWindows())
    {
        throw new NotImplementedException($"{repoName} will only build on Windows because it's not possible to target WPF and Windows Forms from UNIX.");
    }

    Information(Figlet($"{repoName} (*^-^*)"));

    Information("Informational   Version: {0}", gitVersion.InformationalVersion);
    Information("SemVer          Version: {0}", gitVersion.SemVer);
    Information("FullSemVer      Version: {0}", gitVersion.FullSemVer);
    Information("ShortSha        Version: {0}", shortSha);
    Information("AssemblySemVer  Version: {0}", gitVersion.AssemblySemVer);
    Information("MajorMinorPatch Version: {0}", gitVersion.MajorMinorPatch);
    Information("IsLocalBuild           : {0}", isLocal);
    Information("Target                 : {0}", target);
    Information("Branch                 : {0}", branchName);
    Information("IsDevelopBranch        : {0}", isDevelopBranch);
    Information("OsReleaseBranch        : {0}", isReleaseBranch);
    Information("IsTagged               : {0}", isTagged);
});

Teardown(ctx =>
{
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Ver")
    .Description("show gitversion")
    .ContinueOnError()
    .Does(() =>
    {
    });

Task("Clean")
    .Description("clean project")
    .ContinueOnError()
    .Does(() =>
    {
        // clean build files
        var directoriesToDelete = GetDirectories("./**/obj")
            .Concat(GetDirectories("./**/bin"))
            .Concat(GetDirectories("./**/Publish"))
            .Concat(GetDirectories("./packages"))
            .Concat(GetDirectories("./publish"));
        DeleteDirectories(directoriesToDelete, new DeleteDirectorySettings { Recursive = true, Force = true });

        var settings = new DotNetCoreCleanSettings
        {
            Verbosity = verbosity,
        };
        DotNetCoreClean(solution,settings);
    });

Task("Restore")
    .Description($"restore project. [{solution}]")
    .Does(() =>
    {
        var settings = new DotNetCoreRestoreSettings
        {
            Verbosity = verbosity,
        };
        DotNetCoreRestore(solution,settings);
    });

// Task("Test")
//     .Description($"test project. [{solution}]")
//     .IsDependentOn("Restore")
//     .Does(() =>
//     {
//         DotNetCoreTest(solution);
//     });

Task("Release")
    .Description($"build with release configuration. [{solution}]")
    .Does(() =>
    {
        var buildConfiguration = Argument("configuration", "Release");
        var msBuildSettings = new DotNetCoreMSBuildSettings {
            ArgumentCustomization= args => args
                .Append("-nodeReuse:false")
            , BinaryLogger = new MSBuildBinaryLoggerSettings() { Enabled = isLocal }
        };

        msBuildSettings = msBuildSettings
            .SetMaxCpuCount(2)
            .SetConfiguration(buildConfiguration)
            .WithProperty("Description", description)
            .WithProperty("Version", gitVersion.MajorMinorPatch)
            .WithProperty("AssemblyVersion", gitVersion.AssemblySemVer)
            .WithProperty("FileVersion", gitVersion.AssemblySemFileVer)
            .WithProperty("InformationalVersion", gitVersion.InformationalVersion);

        var buildSetting=new DotNetCoreBuildSettings{
            MSBuildSettings = msBuildSettings,
            Verbosity = verbosity,
        };
        DotNetCoreBuild(solution,buildSetting);
    });

///////////////////////////////////////////////////////////////////////////////
// TASK TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("All")
    .Description("clean and build all configuration")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Release");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);