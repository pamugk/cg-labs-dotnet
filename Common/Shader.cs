using Common.Exceptions;
using Silk.NET.OpenGL;

namespace Common
{
    public class Shader
    {
        private GL gl;
        public uint Handler {get; private set;}
        public string Text {get;set;}
        public ShaderType Type {get;}

        public Shader(GL gl)
        {
            this.gl = gl;
        }

        public Shader(GL gl, string text, ShaderType type)
        {
            this.gl = gl;
            Text = text;
            Type = type;
        }

        public void Make()
        {
            Handler = gl.CreateShader(Type);
            gl.ShaderSource(Handler, Text);
            gl.CompileShader(Handler);

            string infoLog = gl.GetShaderInfoLog(Handler);
            if (!string.IsNullOrWhiteSpace(infoLog)) {
                gl.DeleteShader(Handler);
                Handler = 0;
                throw new CompilationFailureException($"Ошибка компиляции шейдера: {infoLog}");
            }
        }
    }
}