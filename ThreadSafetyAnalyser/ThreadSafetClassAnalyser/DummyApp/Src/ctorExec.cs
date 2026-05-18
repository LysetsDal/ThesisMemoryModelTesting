using Annotations;

namespace DummyApp.Src;

[ThreadSafe]
public class ctorExec
{
    int[] _buffer = new int[10];    // SP001: mutable array field — array contents can be modified after construction

    public ctorExec()
    {
        PrintFields(); // Invokes B's PrintFields();
    }

    public virtual void PrintFields() { }
}
[ThreadSafe]
class B : ctorExec
{
    int hello = 1;
    int world;

    public B() // here's a hidden :base(), this is only possible as A has an empty ctor.
    {
        // 1. Variable initializations
        // x = 1
        // 2. "Parent" constructor invocation.
        // a()
        // 3. Statements declared in the constructor body.
        world = -1;
    } // 4. Exit constuctor
        // 5. Object initilizations if any.

    public override void PrintFields() =>
        Console.WriteLine($"x = {hello}, y = {world}");
}
/*
    * x = 1, y = 0
    * x = 1, y = -1
    */

