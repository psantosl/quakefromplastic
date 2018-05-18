class SampleClass
{
    static void Main(string[] args)
    {
        foreach( string s in args )
        {
            if( s != null )
                Print(s);
        }
    }

    static void Print(string s)
    {
        // again
        Console.Writeline(s);
    }
}

