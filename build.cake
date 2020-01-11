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

if (HasArgument("PrereleaseNumber"))
{
    msBuildSettings.WithProperty("PrereleaseNumber", Argument<string>("PrereleaseNumber"));
    msBuildSettings.WithProperty("VersionSuffix", nugetPrereleaseTextPart + Argument<string>("PrereleaseNumber"));
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

            StartProcessThrowOnError("nuget", "sources remove -Name cmdty -Source https://pkgs.dev.azure.com/cmdty/_packaging/cmdty/nuget/v3/index.json -ConfigFile NuGet.config");
			//NuGetRemoveSource("cmdty", "https://pkgs.dev.azure.com/cmdty/_packaging/cmdty/nuget/v3/index.json");

			// Add the authenticated feed source
			NuGetAddSource(
				"cmdty",
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
                ArgumentCustomization = args=>args.Append($"/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura"),
                Logger = "trx",
                ResultsDirectory = testResultDir,
                Configuration = configuration,
                NoBuild = true
            });
    }
});

string vEnvPath = System.IO.Path.Combine("src", "Cmdty.Storage.Python", "storage-venv");
string vEnvActivatePath = System.IO.Path.Combine(vEnvPath, "Scripts", "activate.bat");

Task("Create-VirtualEnv")
    .Does(() =>
{
    if (System.IO.File.Exists(vEnvActivatePath))
    {
        Information("storage-venv Virtual Environment already exists, so no need to create.");
    }
    else
    {
        Information("Creating storage-venv Virtual Environment.");
        StartProcessThrowOnError("python", "-m venv " + vEnvPath);
    }
});

Task("Install-VirtualEnvDependencies")
	.IsDependentOn("Create-VirtualEnv")
    .Does(() =>
{
    RunCommandInVirtualEnv("python -m pip install --upgrade pip", vEnvActivatePath);
    RunCommandInVirtualEnv("pip install pytest", vEnvActivatePath);
    RunCommandInVirtualEnv("pip install -r src/Cmdty.Storage.Python/requirements.txt", vEnvActivatePath);
    RunCommandInVirtualEnv("pip install -e src/Cmdty.Storage.Python", vEnvActivatePath);
});

var testPythonTask = Task("Test-Python")
    .IsDependentOn("Install-VirtualEnvDependencies")
	.IsDependentOn("Test-C#")
	.Does(() =>
{
    RunCommandInVirtualEnv("python -m pytest src/Cmdty.Storage.Python/tests --junitxml=junit/test-results.xml", vEnvActivatePath);
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
    .IsDependentOn("Test-Python")
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

private void RunCommandInVirtualEnv(string command, string vEnvActivatePath)
{
    Information("Running command in venv: " + command);
    string fullCommand = $"/k {vEnvActivatePath} & {command} & deactivate & exit";
    Information("Command to execute: " + fullCommand);
    StartProcessThrowOnError("cmd", $"/k {vEnvActivatePath} & {command} & deactivate & exit");
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
	.IsDependentOn("Pack-NuGet")
    .IsDependentOn("Pack-Python");

RunTarget(target);
