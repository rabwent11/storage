var target = Argument<string>("Target", "Default");
var configuration = Argument<string>("Configuration", "Release");
bool publishWithoutBuild = Argument<bool>("PublishWithoutBuild", false);
string nugetPrereleaseTextPart = Argument<string>("PrereleaseText", "alpha");

var artifactsDirectory = Directory("./artifacts");
var testResultDir = "./temp/";
var isRunningOnBuildServer = !BuildSystem.IsLocalBuild;

var msBuildSettings = new DotNetCoreMSBuildSettings();

// Maps text used in prerelease part in NuGet package to PyPI package
var prereleaseVersionTextMapping = new Dictionary<string, string>
{
	{"alpha", "a"},
	{"beta", "b"},
	{"rc", "rc"}
};

string pythonPrereleaseTextPart = prereleaseVersionTextMapping[nugetPrereleaseTextPart];

msBuildSettings.WithProperty("PythonPreReleaseTextPart", pythonPrereleaseTextPart);

if (HasArgument("BuildNumber"))
{
    msBuildSettings.WithProperty("BuildNumber", Argument<string>("BuildNumber"));
    msBuildSettings.WithProperty("VersionSuffix", nugetPrereleaseTextPart + Argument<string>("BuildNumber"));
}

if (HasArgument("VersionPrefix"))
{
    msBuildSettings.WithProperty("VersionPrefix", Argument<string>("VersionPrefix"));
}

if (HasArgument("PythonVersion"))
{
    msBuildSettings.WithProperty("PythonVersion", Argument<string>("PythonVersion"));
}

Task("Add-NuGetSource")
    .Does(() =>
    {
		if (isRunningOnBuildServer)
		{
			// Get the access token
			string accessToken = EnvironmentVariable("SYSTEM_ACCESSTOKEN");
			if (string.IsNullOrEmpty(accessToken))
			{
				throw new InvalidOperationException("Could not resolve SYSTEM_ACCESSTOKEN.");
			}

			NuGetRemoveSource("Cmdty", "https://pkgs.dev.azure.com/cmdty/_packaging/cmdty/nuget/v3/index.json");

			// Add the authenticated feed source
			NuGetAddSource(
				"Cmdty",
				"https://pkgs.dev.azure.com/cmdty/_packaging/cmdty/nuget/v3/index.json",
				new NuGetSourcesSettings
				{
					UserName = "VSTS",
					Password = accessToken
				});
		}
		else
		{
			Information("Not running on build so no need to add Cmdty NuGet source");
		}
    });

Task("Clean-Artifacts")
    .Does(() =>
{
    CleanDirectory(artifactsDirectory);
});

Task("Build")
	.IsDependentOn("Add-NuGetSource")
	.IsDependentOn("Clean-Artifacts") // Necessary as msbuild tasks in Cmdty.Storage.Excel.csproj copy the add-ins into the artifacts directory
    .Does(() =>
{
    var dotNetCoreSettings = new DotNetCoreBuildSettings()
            {
                Configuration = configuration,
                MSBuildSettings = msBuildSettings
            };
    DotNetCoreBuild("Cmdty.Storage.sln", dotNetCoreSettings);
});

Task("Test-C#")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Cleaning test output directory");
    CleanDirectory(testResultDir);

    var projects = GetFiles("./tests/**/*.Test.csproj");
    
    foreach(var project in projects)
    {
        Information("Testing project " + project);
        DotNetCoreTest(
            project.ToString(),
            new DotNetCoreTestSettings()
            {
//                ArgumentCustomization = args=>args.Append($"/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura"),
                Logger = "trx",
                ResultsDirectory = testResultDir,
                Configuration = configuration,
                NoBuild = true
            });
    }
});

Task("Build-Samples")
    .IsDependentOn("Add-NuGetSource")
	.Does(() =>
{
	var dotNetCoreSettings = new DotNetCoreBuildSettings()
        {
            Configuration = configuration,
        };
	DotNetCoreBuild("samples/csharp/Cmdty.Storage.Samples.sln", dotNetCoreSettings);
});

Task("Pack-NuGet")
	.IsDependentOn("Build-Samples")
	.IsDependentOn("Test-C#")
	.Does(() =>
{
	var dotNetPackSettings = new DotNetCorePackSettings()
                {
                    Configuration = configuration,
                    OutputDirectory = artifactsDirectory,
                    NoRestore = true,
                    NoBuild = true,
                    MSBuildSettings = msBuildSettings
                };
	DotNetCorePack("src/Cmdty.Storage/Cmdty.Storage.csproj", dotNetPackSettings);
});	

Task("Pack-Python")
//    .IsDependentOn("Test-Python")
    .IsDependentOn("Build")
	.Does(setupContext =>
{
    CleanDirectory("src/Cmdty.Storage.Python/build");
    CleanDirectory("src/Cmdty.Storage.Python/dist");
    var originalWorkingDir = setupContext.Environment.WorkingDirectory;
    string pythonProjDir = System.IO.Path.Combine(originalWorkingDir.ToString(), "src", "Cmdty.Storage.Python");
    setupContext.Environment.WorkingDirectory = pythonProjDir;
    try
    {    
        StartProcessThrowOnError("python", "setup.py", "sdist", "bdist_wheel");
    }
    finally
    {
        setupContext.Environment.WorkingDirectory = originalWorkingDir;
    }
});

Task("Push-NuGetToCmdtyFeed")
    .IsDependentOn("Add-NuGetSource")
    .IsDependentOn("Pack-NuGet")
    .Does(() =>
{
    var nupkgPath = GetFiles(artifactsDirectory.ToString() + "/*.nupkg").Single();
    Information($"Pushing NuGetPackage in {nupkgPath} to Cmdty feed");
    NuGetPush(nupkgPath, new NuGetPushSettings 
    {
        Source = "Cmdty",
        ApiKey = "VSTS"
    });
});

private string GetEnvironmentVariable(string envVariableName)
{
    string envVariableValue = EnvironmentVariable(envVariableName);
    if (string.IsNullOrEmpty(envVariableValue))
        throw new ApplicationException($"Environment variable '{envVariableName}' has not been set.");
    return envVariableValue;
}

private void StartProcessThrowOnError(string applicationName, params string[] processArgs)
{
    var argsBuilder = new ProcessArgumentBuilder();
    foreach(string processArg in processArgs)
    {
        argsBuilder.Append(processArg);
    }
    int exitCode = StartProcess(applicationName, new ProcessSettings {Arguments = argsBuilder});
    if (exitCode != 0)
        throw new ApplicationException($"Starting {applicationName} in new process returned non-zero exit code of {exitCode}");
}

var publishNuGetTask = Task("Publish-NuGet")
    .Does(() =>
{
    string nugetApiKey = GetEnvironmentVariable("NUGET_API_KEY");

    var nupkgPath = GetFiles(artifactsDirectory.ToString() + "/*.nupkg").Single();

    NuGetPush(nupkgPath, new NuGetPushSettings 
    {
        ApiKey = nugetApiKey,
        Source = "https://api.nuget.org/v3/index.json"
    });
});

if (!publishWithoutBuild)
{
    publishNuGetTask.IsDependentOn("Pack-NuGet");
}
else
{
    Information("Publishing without first building as PublishWithoutBuild variable set to true.");
}

Task("Default")
	.IsDependentOn("Pack-NuGet");

Task("CI")
	.IsDependentOn("Push-NuGetToCmdtyFeed");

RunTarget(target);
