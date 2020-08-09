using System;

namespace Common.Exceptions
{
    public class CompilationFailureException:Exception
    {
        public CompilationFailureException():base(){ }
        public CompilationFailureException(string message):base(message){ }
    }
}