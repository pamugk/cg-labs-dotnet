using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace Common
{
    public class ShaderVariable
    {
        public int Handler { get; set; }
        public string Name { get; }
        public Dictionary<string, object> Options {get;}
        public UniformType Type { get; }
        public object Value { get; set; }

        public ShaderVariable(string name, UniformType type)
        {
            Name = name;
            Type = type;
            Options = new Dictionary<string, object>();
        }

        public T GetValue<T>() => (T)Value;

        public void SetOption(string name, object option)
        {
            if (Options.ContainsKey(name))
                Options[name] = option;
            else
                Options.Add(name, option);
        }
    }
}