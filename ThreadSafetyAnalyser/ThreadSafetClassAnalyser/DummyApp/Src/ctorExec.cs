namespace DummyApp.Model
{
    class A
    {
        public A()
        {
            PrintFields(); // Invokes B's PrintFields();
        }

        public virtual void PrintFields() { }
    }

    class B : A
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
}
