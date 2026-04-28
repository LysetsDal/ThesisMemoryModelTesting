#!/usr/bin/env bash

# 1. Check if the current user is 'asgerlysdahl'
if [ "$USER" != "asgerlysdahl" ]; then
    echo "Current user is $USER. Skipping custom rebuild script."
    exit 0
fi

# Exit immediately if a command exits with a non-zero status
set -e

# Use the root directory of your project
PROJECT_ROOT="/Users/asgerlysdahl/RiderProjects/ThesisMemoryModelTesting/ThreadSafetyAnalyser/ThreadSafetClassAnalyser"

cd "$PROJECT_ROOT"

echo "🧹 Cleaning and Restoring..."
dotnet clean
dotnet restore

echo "🛠️ Building Analyzer..."
dotnet build ThreadSafetClassAnalyser/ThreadSafetClassAnalyser.csproj -c Debug

echo "🛠️ Building CodeFixes..."
dotnet build ThreadSafetClassAnalyser.CodeFixes/ThreadSafetClassAnalyser.CodeFixes.csproj -c Debug

echo "📦 Building Package..."
dotnet build ThreadSafetClassAnalyser.Package/ThreadSafetClassAnalyser.Package.csproj -c Debug

echo "✅ Build complete!"