class SampleClass
{
    static void Main(string[] args)
    {
	// non conflicting changes
        foreach( string s in args )
        {
            if( s != null )
                Print(s);
        }
    }

    static void Print(string s)
    {
        // again and again and again
        Console.Writeline(s);
    }
}

