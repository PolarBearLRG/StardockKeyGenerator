# StardockKeyGenerator
This tool is designed to automate trial license generation for Stardock programs using the [mail.tm](mail.tm) API. Currently, it is heavily focused on Start 11 / WindowBlinds 11, but I would like to expand its features to include other Windows applications. Aimed at Windows x64 arch.

The repo can be built and published in Visual Studio using .NET 9.0 SDK. This will output a standalone EXE.
```
dotnet publish
```
Credit to:
[@discriminating](https://github.com/discriminating): Created the original registry editor batch code that inspired this project.
[@SmorcIRL](https://github.com/SmorcIRL/mail.tm): Created the Mail.tm .NET API wrapper used in this project.
