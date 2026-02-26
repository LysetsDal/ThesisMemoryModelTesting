using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace MemoryModelTests.Utils;

public class RepeatAttribute : DataAttribute
{
    private readonly int _count;

    public RepeatAttribute(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        _count = count;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        for (int i = 0; i < _count; i++)
        {
            yield return new object[] { i };
        }
    }
}