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

Add this to the Test C# Projects .csproj file: 
```xml
  <ItemGroup>
    <Analyzer Include="/Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.Package/bin/Debug/netstandard2.0/ThreadSafetClassAnalyser.dll" />
    <Analyzer Include="/Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.CodeFixes/bin/Debug/netstandard2.0/ThreadSafetClassAnalyser.CodeFixes.dll" />
  </ItemGroup>
```

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