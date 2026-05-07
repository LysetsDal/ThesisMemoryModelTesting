using Microsoft.CodeAnalysis;

namespace ThreadSafetClassAnalyser
{
    /// <summary>
    /// A wrapper class that simplifies the creation of localizable strings for Roslyn diagnostics.
    /// This class maps a single prefix to the three required resource keys used by a
    /// <see cref="DiagnosticDescriptor"/>.
    /// </summary>
    public class AnalyserMetadata
    {
        public LocalizableString Title { get; }
        public LocalizableString MessageFormat { get; }
        public LocalizableString Description { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyserMetadata"/> class.
        /// </summary>
        /// <param name="resourceNamePrefix">
        /// The base name of the resource keys in <c>Resources.resx</c>.
        /// For example, if "FieldAccessedExternally" is provided, the class will look for:
        /// <list type="bullet">
        /// <item><description>FieldAccessedExternallyTitle</description></item>
        /// <item><description>FieldAccessedExternallyMessageFormat</description></item>
        /// <item><description>FieldAccessedExternallyDescription</description></item>
        /// </list>
        /// </param>
        /// <remarks>
        /// This constructor links the keys to the <see cref="Resources.ResourceManager"/> to enable 
        /// the LocalizationManager to fetch the correct translated strings at runtime based on the IDE's culture settings.
        /// </remarks>
        public AnalyserMetadata(string resourceNamePrefix)
        {
            // We use the prefix to find the matching keys in your Resources.resx
            Title = new LocalizableResourceString($"{resourceNamePrefix}Title", 
                Resources.ResourceManager, typeof(Resources));
            
            MessageFormat = new LocalizableResourceString($"{resourceNamePrefix}MessageFormat", 
                Resources.ResourceManager, typeof(Resources));
            
            Description = new LocalizableResourceString($"{resourceNamePrefix}Description", 
                Resources.ResourceManager, typeof(Resources));
        }
    }
}