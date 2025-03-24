using dotenv.net;
using System.CommandLine;
using yUtil;
using yUtil.Analyser;

DotEnv.Load();
CI.Load();

Command analyseCommand = new("analyse", "Analyse resources for conflicts")
{
    new Argument<string>(
        name: "dir",
        description: "Directory or file to analyse"
    ),
    new Option<string>(
        name: "--ext",
        description: "File extensions to include",
        getDefaultValue: () => string.Join(',', Analyser.DefaultExtensions)
    )
};

analyseCommand.SetHandler(async (string dir, string extensions) =>
{
    AnalysisJob analysisJob = new(extensions);
    analysisJob.Init();
    await analysisJob.Run(dir);
},
(Argument<string>)analyseCommand.Arguments[0],
(Option<string>)analyseCommand.Options[0]);

Command calcCommand = new("calc", "Calculate flags and extents")
{
    new Argument<string>(
        name: "dir",
        description: "Directory containing .ymaps"
    ),
};

calcCommand.SetHandler(async (string dir) =>
{
    CalcJob calcJob = new();
    calcJob.Init();
    await calcJob.Run(dir);
},
(Argument<string>)calcCommand.Arguments[0]);

Command dependencyCommand = new("dep", "Analyse archetype dependencies")
{
    new Argument<string>(
        name: "dir",
        description: "Directory to analyse"
    ),
    new Option<string>(
        name: "--ext",
        description: "File extensions to include",
        getDefaultValue: () => ".ymap,.ytyp"
    )
};

dependencyCommand.SetHandler(async (string dir, string extensions) =>
{
    DependencyJob dependencyJob = new(extensions);
    dependencyJob.Init();
    await dependencyJob.Run(dir);
},
(Argument<string>)dependencyCommand.Arguments[0],
(Option<string>)dependencyCommand.Options[0]);

Command intersectCommand = new("intersect", "Intersect YMAPs")
{
    new Argument<string>(
        name: "dir",
        description: "Directory containing .ymaps"
    ),
    new Argument<string>(
        name: "out-dir",
        description: "Directory to write the intersected .ymaps to"
    ),
    new Option<string>(
        name: "--ymap",
        description: "YMAP to intersect",
        getDefaultValue: () => "*.ymap"
    )
};

intersectCommand.SetHandler(async (string dir, string outDir, string ymapName) =>
{
    IntersectJob intersectJob = new(outDir, ymapName);
    intersectJob.Init();
    await intersectJob.Run(dir);
},
(Argument<string>)intersectCommand.Arguments[0],
(Argument<string>)intersectCommand.Arguments[1],
(Option<string>)intersectCommand.Options[0]);

RootCommand rootCommand = new("A tool to help manage FiveM resources")
{
    analyseCommand,
    dependencyCommand,
    new Command("ymaps", "YMAP Commands")
    {
        calcCommand,
        intersectCommand,
    },
};

int result = await rootCommand.InvokeAsync(args);
if (result != 0)
{
    return result;
}

return CI.Enabled ? CI.ExitCode : 0;
