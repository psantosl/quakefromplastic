class SampleClass
{
    static void Main(string[] args)
    {
// foreach arg
        foreach( string s in args )
        {
            if( s != null )
                Print(s);
        }
    }

    static void Print(string s)
    {
        // prints the string
        Console.Writeline(s);
    }
}

