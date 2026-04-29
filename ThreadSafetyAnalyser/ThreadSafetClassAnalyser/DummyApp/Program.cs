using System;
using DummyApp.Model;

namespace DummyApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        Test test = new Test();

        test.GetCount();
        test.GetCountLocked();
        
    }
}