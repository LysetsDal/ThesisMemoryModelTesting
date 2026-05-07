# ThesisMemoryModelTesting

Build Analyser dll on Asger's Machine
```bash
cd /Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser ;
dotnet clean ; dotnet restore ; 
dotnet build ThreadsafeClassAnalyser.Annotations/ThreadsafeClassAnalyser.Annotations.csproj ;
dotnet build ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.csproj ; 
dotnet build ThreadSafetClassAnalyser.CodeFixes/ThreadSafetClassAnalyser.CodeFixes.csproj ;
dotnet build ThreadSafetClassAnalyser.Package/ThreadSafetClassAnalyser.Package.csproj
```
---
## Base
Add this to the Test C# Projects .csproj file: 
```xml
  <ItemGroup>
    <Analyzer Include="/Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.Package/bin/Debug/netstandard2.0/ThreadSafetClassAnalyser.dll" />
    <Analyzer Include="/Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.CodeFixes/bin/Debug/netstandard2.0/ThreadSafetClassAnalyser.CodeFixes.dll" />
  </ItemGroup>
```
## Annotation for specific analysis
Add this to the Repo you want the analyser to run on for the annotations:
```xml
<ItemGroup>
    <!-- Reference so we can use [ThreadSafe] annotation -->
    <ProjectReference Include="../ThreadsafeClassAnalyser.Annotations/ThreadsafeClassAnalyser.Annotations.csproj" />
    <!-- Reference to the analyser -->
    <ProjectReference Include="../ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```
## Base + analyzer information.
For analyzer stats.
This needs to be pasted into the .csproj files that we wanna analyze.
```xml
<PropertyGroup>
  <!-- Insert after whatever native propertygroups there are. (Or in its own PropertyGroup)-->
  <_SarifTimestamp Condition="'$(_SarifTimestamp)' == ''">$([System.DateTime]::Now.ToString("yyyyMMdd_HHmmss"))</_SarifTimestamp>
  <ErrorLog>$(SolutionDir).sarif\$(MSBuildProjectName)_$(_SarifTimestamp).json;version=2.1</ErrorLog>
  <ReportAnalyzer>true</ReportAnalyzer>
</PropertyGroup>
  <!-- Inject all these libraries in the project-->
<ItemGroup>
  <Analyzer Include="C:\Users\dvh\Desktop\Github\ITU\ThesisMemoryModelTesting\ThreadSafetyAnalyser\ThreadSafetClassAnalyser\ThreadSafetClassAnalyser\bin\Debug\netstandard2.0\ThreadSafetClassAnalyser.dll" />
  <Analyzer Include="C:\Users\dvh\Desktop\Github\ITU\ThesisMemoryModelTesting\ThreadSafetyAnalyser\ThreadSafetClassAnalyser\ThreadSafetClassAnalyser.CodeFixes\bin\Debug\netstandard2.0\ThreadSafetClassAnalyser.CodeFixes.dll" />
</ItemGroup>
<!--Ensure that there is a directory to output the report into-->
<Target Name="CreateSarifDirectory" BeforeTargets="CoreCompile">
    <MakeDir Directories="$(SolutionDir).sarif" />
</Target>
```

Tools -> Options -> "Build and Run" -> "MsBuild Project build output verbosity" set to detailed.
or
```
dotnet clean && dotnet build -verbosity:detailed > log.log
cat log.log | sed -n '/Time (s)    %   Analyzer/,/task "Csc"/p'
```
