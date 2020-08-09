using System;

namespace Common.Exceptions
{
    public class VariableNotDefinedException: Exception
    {
        public VariableNotDefinedException():base(){ }
        public VariableNotDefinedException(string varName):base($"Переменная '{varName} не определена'"){ }
    }
}