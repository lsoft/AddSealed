# AddSealed

This is an analyzer and codefixer for adding `sealed` modifier to the appropriate classes.

This analyzer is a project-wide analyzer, so it will not run in coding time in Visual Studio. This analyzer will run only in compile time and can be run manually with `dotnet format your.sln analyzers --diagnostics AddSealed`.

An appropriate nuget can be found [here](https://www.nuget.org/packages/AddSealed/)

