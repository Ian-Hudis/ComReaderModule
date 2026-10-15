// Simple test to verify buffering and parsing logic
// Usage: test via string fragments

using System;
using ComReaderModule;
using static ComReaderModule.USBSniff;

class TestParser
{
    static void testparse()
    {
        Console.WriteLine("=== USB Data Buffer & Parse Test ===\n");

        // Test Case 1: Complete message in one call (baseline)
        Console.WriteLine("Test 1: Complete message in single call");
        TestMessage("#99214021291090028000012900200020000FE");

        // Test Case 2: Fragmented by character (worst case)
        Console.WriteLine("\n\nTest 2: Message fragmented by character");
        string msg = "#99214021291090028000012900200020000FE";
        foreach (char c in msg)
        {
            Console.WriteLine($"  --> Sending: '{c}'");
            DataInterpreter.DataExtraction(c.ToString(), DataDirection.MachineToPc);
            System.Threading.Thread.Sleep(10);
        }

        // Test Case 3: Fragmented by word
        Console.WriteLine("\n\nTest 3: Message fragmented by word");
        DataInterpreter.ResetState();
        TestMessage("#9921");
        System.Threading.Thread.Sleep(100);
        TestMessage("4021291090028");
        System.Threading.Thread.Sleep(100);
        TestMessage("000012900200020000FE");

        Console.WriteLine("\n=== All tests complete ===");
        Console.WriteLine("Check UIData.json for Variable1, Variable2, Variable3 values.");
    }

    static void TestMessage(string msg)
    {
        Console.WriteLine($"  --> Sending: '{msg}'");
        DataInterpreter.DataExtraction(msg, DataDirection.MachineToPc);
        System.Threading.Thread.Sleep(100);
    }
}
