using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MemoryModelTests.Publication;

public class ObjectConstructors
{

/// <summary>
/// Demonstrates memory reordering issues as described in:
/// Igor Ostrovsky - "The C# Memory Model in Theory and Practice" (MSDN Magazine, December 2012)
/// </summary>

    private readonly ITestOutputHelper _testOutputHelper;

    public ObjectConstructors(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// BROKEN: This class demonstrates a memory reordering bug.
    /// Print() can output "0" even though Init() writes 42 before setting _initialized to true.
    /// This happens because:
    /// 1. Write 1 and Write 2 can be reordered (writes in Init)
    /// 2. Read 1 and Read 2 can be reordered (reads in Print)
    /// </summary>
    public class DataInit
    {
        private int _data = 0;
        private bool _initialized = false;

        public void Init()
        {
            _data = 42;            // Write 1
            _initialized = true;   // Write 2
        }

        public int? Print()
        {
            if (_initialized)            // Read 1
            {
                return _data;            // Read 2
            }
            return null;
        }
    }

    /// <summary>
    /// FIXED: Using volatile keyword prevents reordering.
    /// volatile bool provides acquire-release semantics:
    /// - volatile write (release): can't be reordered with PRIOR operations
    /// - volatile read (acquire): can't be reordered with SUBSEQUENT operations
    /// </summary>
    public class DataInitFixed
    {
        private int _data = 0;
        private volatile bool _initialized = false;

        public void Init()
        {
            _data = 42;            // Write 1 (ordinary)
            _initialized = true;   // Write 2 (volatile - can't move before Write 1)
        }

        public int? Print()
        {
            if (_initialized)            // Read 1 (volatile - can't move after Read 2)
            {
                return _data;            // Read 2 (ordinary)
            }
            return null;
        }
    }

    [Fact]
    public void DemonstrateReorderingIssue_BrokenVersion()
    {
        _testOutputHelper.WriteLine("=== Testing BROKEN DataInit (non-volatile) ===");
        _testOutputHelper.WriteLine("Possible outputs: null (not initialized), 42 (correct), or 0 (REORDERING BUG)");
        _testOutputHelper.WriteLine("");

        var results = new Dictionary<string, int>
        {
            { "null", 0 },
            { "0", 0 },
            { "42", 0 }
        };

        // Run many iterations to try to trigger the race condition
        const int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            var dataInit = new DataInit();
            int? result = null;

            var t1 = Task.Run(() => dataInit.Init());
            var t2 = Task.Run(() => result = dataInit.Print());

            Task.WaitAll(t1, t2);

            string resultKey = result?.ToString() ?? "null";
            if (results.ContainsKey(resultKey))
                results[resultKey]++;
        }

        _testOutputHelper.WriteLine($"Results after {iterations} iterations:");
        _testOutputHelper.WriteLine($"  null (not initialized): {results["null"]}");
        _testOutputHelper.WriteLine($"  0 (REORDERING BUG!):   {results["0"]}");
        _testOutputHelper.WriteLine($"  42 (correct):          {results["42"]}");
        _testOutputHelper.WriteLine("");
        
        if (results["0"] > 0)
        {
            _testOutputHelper.WriteLine($"⚠️  REORDERING DETECTED! Saw value 0 {results["0"]} times!");
        }
        else
        {
            _testOutputHelper.WriteLine("ℹ️  No reordering detected in this run (but still possible in theory)");
            _testOutputHelper.WriteLine("   On x86/x64, this bug is rare because hardware has strong ordering guarantees.");
            _testOutputHelper.WriteLine("   On ARM/ARM64, this bug would be much more common.");
        }
    }

    [Fact]
    public void DemonstrateReorderingFix_VolatileVersion()
    {
        _testOutputHelper.WriteLine("=== Testing FIXED DataInitFixed (with volatile) ===");
        _testOutputHelper.WriteLine("Expected outputs: null (not initialized) or 42 (correct)");
        _testOutputHelper.WriteLine("Should NEVER see 0 due to volatile preventing reordering");
        _testOutputHelper.WriteLine("");

        var results = new Dictionary<string, int>
        {
            { "null", 0 },
            { "0", 0 },
            { "42", 0 }
        };

        // Run many iterations
        const int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            var dataInit = new DataInitFixed();
            int? result = null;

            var t1 = Task.Run(() => dataInit.Init());
            var t2 = Task.Run(() => result = dataInit.Print());

            Task.WaitAll(t1, t2);

            string resultKey = result?.ToString() ?? "null";
            if (results.ContainsKey(resultKey))
                results[resultKey]++;
        }

        _testOutputHelper.WriteLine($"Results after {iterations} iterations:");
        _testOutputHelper.WriteLine($"  null (not initialized): {results["null"]}");
        _testOutputHelper.WriteLine($"  0 (should be ZERO):    {results["0"]}");
        _testOutputHelper.WriteLine($"  42 (correct):          {results["42"]}");
        _testOutputHelper.WriteLine("");

        Assert.Equal(0, results["0"]); // Should NEVER see 0 with volatile
        
        _testOutputHelper.WriteLine("✓ No reordering bug! volatile keyword successfully prevents the issue.");
    }

    [Fact]
    public void ExplainVolatileSemantics()
    {
        _testOutputHelper.WriteLine("=== Volatile Keyword Semantics ===");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("ACQUIRE SEMANTICS (volatile READ):");
        _testOutputHelper.WriteLine("  - Forms a ONE-WAY fence");
        _testOutputHelper.WriteLine("  - Preceding operations CAN pass it");
        _testOutputHelper.WriteLine("  - Subsequent operations CANNOT pass it");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("RELEASE SEMANTICS (volatile WRITE):");
        _testOutputHelper.WriteLine("  - Forms a ONE-WAY fence");
        _testOutputHelper.WriteLine("  - Prior operations CANNOT pass it");
        _testOutputHelper.WriteLine("  - Subsequent operations CAN pass it");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("In DataInitFixed:");
        _testOutputHelper.WriteLine("  Init():  _data = 42; _initialized = true;");
        _testOutputHelper.WriteLine("           ^^^^^^^^   ^^^^^^^^^^^^^^^^^^^");
        _testOutputHelper.WriteLine("           Write 1    Write 2 (volatile)");
        _testOutputHelper.WriteLine("           Write 2 CANNOT move before Write 1 (release semantics)");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("  Print(): if (_initialized) { return _data; }");
        _testOutputHelper.WriteLine("              ^^^^^^^^^^^^           ^^^^^^");
        _testOutputHelper.WriteLine("              Read 1 (volatile)      Read 2");
        _testOutputHelper.WriteLine("              Read 2 CANNOT move before Read 1 (acquire semantics)");
    }

    [Fact]
    public void StressTest_TryToTriggerReordering()
    {
        _testOutputHelper.WriteLine("=== STRESS TEST: Attempting to trigger reordering ===");
        _testOutputHelper.WriteLine("Running 100,000 iterations with aggressive parallelism...");
        _testOutputHelper.WriteLine("");

        const int iterations = 100000;
        int reorderingCount = 0;
        int correctCount = 0;
        int uninitializedCount = 0;

        Parallel.For(0, iterations, i =>
        {
            var dataInit = new DataInit();
            int? result = null;

            var t1 = Task.Run(() => dataInit.Init());
            var t2 = Task.Run(() => result = dataInit.Print());

            Task.WaitAll(t1, t2);

            if (result == 0)
                Interlocked.Increment(ref reorderingCount);
            else if (result == 42)
                Interlocked.Increment(ref correctCount);
            else
                Interlocked.Increment(ref uninitializedCount);
        });

        _testOutputHelper.WriteLine($"Results after {iterations} iterations:");
        _testOutputHelper.WriteLine($"  Uninitialized (null): {uninitializedCount:N0}");
        _testOutputHelper.WriteLine($"  Reordering bug (0):   {reorderingCount:N0}");
        _testOutputHelper.WriteLine($"  Correct (42):         {correctCount:N0}");
        _testOutputHelper.WriteLine("");

        double reorderingPercentage = (reorderingCount * 100.0) / iterations;
        _testOutputHelper.WriteLine($"Reordering bug rate: {reorderingPercentage:F4}%");
        _testOutputHelper.WriteLine("");
        
        if (reorderingCount > 0)
        {
            _testOutputHelper.WriteLine("⚠️  REORDERING BUG DETECTED!");
            _testOutputHelper.WriteLine("This proves that the non-volatile version is broken.");
            Assert.Fail();
        }
        else
        {
            _testOutputHelper.WriteLine("ℹ️  Architecture info:");
            _testOutputHelper.WriteLine($"   Processor Count: {Environment.ProcessorCount}");
            _testOutputHelper.WriteLine($"   OS: {Environment.OSVersion}");
            _testOutputHelper.WriteLine("");
            _testOutputHelper.WriteLine("   No reordering detected, but this doesn't mean it's safe!");
            _testOutputHelper.WriteLine("   x86/x64 have strong memory ordering, so this bug is rare.");
            _testOutputHelper.WriteLine("   On ARM/ARM64 or with JIT optimizations, it would be common.");
        }
    }

    /// <summary>
    /// BROKEN: Demonstrates unsafe publication with object initializer.
    /// Object initializer assignments happen AFTER constructor, creating a race window.
    /// Another thread can see the object reference before seeing initialized property values.
    /// </summary>
    public class Config
    {
        public int MaxConnections { get; set; }
        public int Timeout { get; set; }
        public bool IsValid { get; set; }
    }

    public class ConfigManagerBroken
    {
        private Config? _config = null; // Non-volatile!

        public void Initialize()
        {
            // Object initializer expands to:
            // 1. var temp = new Config();        // Constructor
            // 2. temp.MaxConnections = 100;      // Property assignments
            // 3. temp.Timeout = 30;
            // 4. temp.IsValid = true;
            // 5. _config = temp;                 // Publish reference
            //
            // Without volatile, steps 2-5 can be REORDERED!
            _config = new Config 
            { 
                MaxConnections = 100,  // Write 1
                Timeout = 30,          // Write 2
                IsValid = true         // Write 3
            };                         // Write 4 (publish _config reference)
        }

        public (int maxConn, int timeout, bool valid)? Read()
        {
            var cfg = _config;  // Read 1 (read reference)
            if (cfg != null)
            {
                // Read 2, 3, 4 (read properties)
                // These reads can be reordered with Read 1!
                return (cfg.MaxConnections, cfg.Timeout, cfg.IsValid);
            }
            return null;
        }
    }

    public class ConfigManagerFixed
    {
        private volatile Config? _config = null; // Volatile!

        public void Initialize()
        {
            _config = new Config 
            { 
                MaxConnections = 100,
                Timeout = 30,
                IsValid = true
            };
            // Volatile write ensures all property writes happen-before publish
        }

        public (int maxConn, int timeout, bool valid)? Read()
        {
            var cfg = _config;  // Volatile read
            if (cfg != null)
            {
                // Acquire semantics ensure property reads see initialized values
                return (cfg.MaxConnections, cfg.Timeout, cfg.IsValid);
            }
            return null;
        }
    }

    [Fact]
    public void DemonstrateObjectInitializerReordering_Broken()
    {
        _testOutputHelper.WriteLine("=== Object Initializer + Non-Volatile Field (BROKEN) ===");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("Code:");
        _testOutputHelper.WriteLine("  _config = new Config { MaxConnections = 100, Timeout = 30, IsValid = true };");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("This expands to:");
        _testOutputHelper.WriteLine("  1. temp = new Config()  // Constructor");
        _testOutputHelper.WriteLine("  2. temp.MaxConnections = 100");
        _testOutputHelper.WriteLine("  3. temp.Timeout = 30");
        _testOutputHelper.WriteLine("  4. temp.IsValid = true");
        _testOutputHelper.WriteLine("  5. _config = temp  // Publish reference");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("WITHOUT volatile: steps 2-5 can REORDER!");
        _testOutputHelper.WriteLine("Reader might see: _config != null BUT properties still have default values!");
        _testOutputHelper.WriteLine("");

        var results = new Dictionary<string, int>
        {
            { "null", 0 },
            { "correct", 0 },
            { "partial", 0 }  // Saw object but wrong property values
        };

        const int iterations = 50000;
        for (int i = 0; i < iterations; i++)
        {
            var manager = new ConfigManagerBroken();
            (int maxConn, int timeout, bool valid)? result = null;

            var t1 = Task.Run(() => manager.Initialize());
            var t2 = Task.Run(() => result = manager.Read());

            Task.WaitAll(t1, t2);

            if (result == null)
            {
                results["null"]++;
            }
            else if (result.Value.maxConn == 100 && result.Value.timeout == 30 && result.Value.valid == true)
            {
                results["correct"]++;
            }
            else
            {
                results["partial"]++;
            }
        }

        _testOutputHelper.WriteLine($"Results after {iterations} iterations:");
        _testOutputHelper.WriteLine($"  null (not initialized):     {results["null"]:N0}");
        _testOutputHelper.WriteLine($"  correct (100, 30, true):    {results["correct"]:N0}");
        _testOutputHelper.WriteLine($"  PARTIAL (wrong values!):    {results["partial"]:N0}");
        _testOutputHelper.WriteLine("");

        if (results["partial"] > 0)
        {
            
            _testOutputHelper.WriteLine($"⚠️  UNSAFE PUBLICATION DETECTED!");
            _testOutputHelper.WriteLine($"   Reader saw object reference but stale property values {results["partial"]} times!");
            Assert.Fail();
        }
        else
        {
            _testOutputHelper.WriteLine("ℹ️  No unsafe publication detected in this run.");
            _testOutputHelper.WriteLine("   Still theoretically possible - especially on ARM or with JIT optimizations.");
        }
    }

    [Fact]
    public void DemonstrateObjectInitializerReordering_Fixed()
    {
        _testOutputHelper.WriteLine("=== Object Initializer + Volatile Field (FIXED) ===");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("Code:");
        _testOutputHelper.WriteLine("  volatile Config? _config;");
        _testOutputHelper.WriteLine("  _config = new Config { MaxConnections = 100, ... };");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("WITH volatile:");
        _testOutputHelper.WriteLine("  • Write: Property assignments happen-before volatile _config write");
        _testOutputHelper.WriteLine("  • Read: Volatile _config read happens-before property reads");
        _testOutputHelper.WriteLine("  • Result: If reader sees _config != null, it MUST see all properties!");
        _testOutputHelper.WriteLine("");

        var results = new Dictionary<string, int>
        {
            { "null", 0 },
            { "correct", 0 },
            { "partial", 0 }
        };

        const int iterations = 50000;
        for (int i = 0; i < iterations; i++)
        {
            var manager = new ConfigManagerFixed();
            (int maxConn, int timeout, bool valid)? result = null;

            var t1 = Task.Run(() => manager.Initialize());
            var t2 = Task.Run(() => result = manager.Read());

            Task.WaitAll(t1, t2);

            if (result == null)
            {
                results["null"]++;
            }
            else if (result.Value.maxConn == 100 && result.Value.timeout == 30 && result.Value.valid == true)
            {
                results["correct"]++;
            }
            else
            {
                results["partial"]++;
            }
        }

        _testOutputHelper.WriteLine($"Results after {iterations} iterations:");
        _testOutputHelper.WriteLine($"  null (not initialized):     {results["null"]:N0}");
        _testOutputHelper.WriteLine($"  correct (100, 30, true):    {results["correct"]:N0}");
        _testOutputHelper.WriteLine($"  partial (should be ZERO):   {results["partial"]:N0}");
        _testOutputHelper.WriteLine("");

        Assert.Equal(0, results["partial"]);
        _testOutputHelper.WriteLine("✓ Safe publication! Volatile ensures visibility of all properties.");
    }

    [Fact]
    public void ExplainObjectInitializerPublicationProblem()
    {
        _testOutputHelper.WriteLine("=== Object Initializer Publication Pattern ===");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("THE PROBLEM:");
        _testOutputHelper.WriteLine("  Thread 1 (Writer):");
        _testOutputHelper.WriteLine("    _config = new Config { Prop1 = val1, Prop2 = val2 };");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("  Thread 2 (Reader):");
        _testOutputHelper.WriteLine("    var cfg = _config;");
        _testOutputHelper.WriteLine("    if (cfg != null) { use cfg.Prop1, cfg.Prop2 }");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("  Race window:");
        _testOutputHelper.WriteLine("    1. Constructor creates object");
        _testOutputHelper.WriteLine("    2. Properties get assigned");
        _testOutputHelper.WriteLine("    3. Reference gets published to _config");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("  Without volatile, steps 2 and 3 can be REORDERED!");
        _testOutputHelper.WriteLine("  → Reader sees _config != null but properties have default values");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("THE SOLUTION:");
        _testOutputHelper.WriteLine("  private volatile Config? _config;  // ← Make the reference volatile!");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("  This ensures:");
        _testOutputHelper.WriteLine("    • All property writes complete BEFORE _config is visible");
        _testOutputHelper.WriteLine("    • Reader sees ALL property values when it sees _config != null");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("SIMILAR PATTERNS:");
        _testOutputHelper.WriteLine("  • Lazy initialization (lazy field set via object initializer)");
        _testOutputHelper.WriteLine("  • Caching (cache entry with multiple properties)");
        _testOutputHelper.WriteLine("  • Double-checked locking with object initializers");
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("ALTERNATIVE SOLUTIONS:");
        _testOutputHelper.WriteLine("  • Use Lazy<T> for lazy initialization");
        _testOutputHelper.WriteLine("  • Use locks if performance isn't critical");
        _testOutputHelper.WriteLine("  • Use Interlocked.CompareExchange for the reference");
    }
}  