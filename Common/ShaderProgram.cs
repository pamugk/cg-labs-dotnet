using System.Linq;
using System.Collections.Generic;
using Common.Exceptions;
using Silk.NET.OpenGL;

namespace Common
{
    public class ShaderProgram
    {
        private GL gl;
        public uint Handler {get; private set;}
        private Dictionary<ShaderType, Shader> shaders;
        private Dictionary<string, ShaderVariable> variables;

        public ShaderProgram(GL gl)
        {
            this.gl = gl;
            shaders = new Dictionary<ShaderType, Shader>();
            variables = new Dictionary<string, ShaderVariable>();
        }

        public void AddShaders(params Shader[] shaders)
        {
            foreach (var shader in shaders)
                this.shaders.Add(shader.Type, shader);
        }

        public void AddVariables(params ShaderVariable[] variables)
        {
            foreach (var variable in variables)
                this.variables.Add(variable.Name, variable);
        }

        public void BindVariableValues()
        {
            foreach (var variable in variables.Values)
            {
                switch (variable.Type)
                {
                    case UniformType.Bool:
                    {
                        gl.Uniform1(variable.Handler, variable.GetValue<bool>() ? 1 : 0);
                        break;
                    }
                    case UniformType.BoolVec2:
                    {
                        var vector = variable.GetValue<bool[]>().Select(val => val ? 1 : 0).ToArray();
                        gl.Uniform2(variable.Handler, vector[0], vector[1]);
                        break;
                    } 
                    case UniformType.BoolVec3:
                    {
                        var vector = variable.GetValue<bool[]>().Select(val => val ? 1 : 0).ToArray();
                        gl.Uniform3(variable.Handler, vector[0], vector[1], vector[2]);
                        break;
                    }
                    case UniformType.BoolVec4:
                    {
                        var vector = variable.GetValue<bool[]>().Select(val => val ? 1 : 0).ToArray();
                        gl.Uniform4(variable.Handler, vector[0], vector[1], vector[2], vector[3]);
                        break;
                    }

                    case UniformType.Double:
                    {
                        gl.Uniform1(variable.Handler, variable.GetValue<double>());
                        break;
                    }
                    case UniformType.DoubleMat2:
                    {
                        gl.UniformMatrix2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat2x3:
                    {
                        gl.UniformMatrix2x3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat2x4:
                    {
                        gl.UniformMatrix2x4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"],
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat3:
                    {
                        gl.UniformMatrix3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat3x2:
                    {
                        gl.UniformMatrix3x2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat3x4:
                    {
                        gl.UniformMatrix3x4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat4:
                    {
                        gl.UniformMatrix4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat4x2:
                    {
                        gl.UniformMatrix4x2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleMat4x3:
                    {
                        gl.UniformMatrix4x3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<double[]>()
                        );
                        break;
                    }
                    case UniformType.DoubleVec2:
                    {
                        var vector = variable.GetValue<double[]>();
                        gl.Uniform2(variable.Handler, vector[0], vector[1]);
                        break;
                    } 
                    case UniformType.DoubleVec3:
                    {
                        var vector = variable.GetValue<double[]>();
                        gl.Uniform3(variable.Handler, vector[0], vector[1], vector[2]);
                        break;
                    }
                    case UniformType.DoubleVec4:
                    {
                        var vector = variable.GetValue<double[]>();
                        gl.Uniform4(variable.Handler, vector[0], vector[1], vector[2], vector[3]);
                        break;
                    }

                    case UniformType.Float:
                    {
                        gl.Uniform1(variable.Handler, variable.GetValue<float>());
                        break;
                    }
                    case UniformType.FloatMat2:
                    {
                        gl.UniformMatrix2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat2x3:
                    {
                        gl.UniformMatrix2x3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat2x4:
                    {
                        gl.UniformMatrix2x4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat3:
                    {
                        gl.UniformMatrix3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat3x2:
                    {
                        gl.UniformMatrix3x2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat3x4:
                    {
                        gl.UniformMatrix3x4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat4:
                    {
                        gl.UniformMatrix4(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat4x2:
                    {
                        gl.UniformMatrix4x2(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatMat4x3:
                    {
                        gl.UniformMatrix4x3(
                            variable.Handler, 1, 
                            (bool)variable.Options["transpose"], 
                            variable.GetValue<float[]>()
                        );
                        break;
                    }
                    case UniformType.FloatVec2:
                    {
                        var vector = variable.GetValue<float[]>();
                        gl.Uniform2(variable.Handler, vector[0], vector[1]);
                        break;
                    } 
                    case UniformType.FloatVec3:
                    {
                        var vector = variable.GetValue<float[]>();
                        gl.Uniform3(variable.Handler, vector[0], vector[1], vector[2]);
                        break;
                    }
                    case UniformType.FloatVec4:
                    {
                        var vector = variable.GetValue<float[]>();
                        gl.Uniform4(variable.Handler, vector[0], vector[1], vector[2], vector[3]);
                        break;
                    }

                    case UniformType.Int:
                    {
                        gl.Uniform1(variable.Handler, variable.GetValue<int>());
                        break;
                    }
                    case UniformType.IntVec2:
                    {
                        var vector = variable.GetValue<int[]>();
                        gl.Uniform2(variable.Handler, vector[0], vector[1]);
                        break;
                    } 
                    case UniformType.IntVec3:
                    {
                        var vector = variable.GetValue<int[]>();
                        gl.Uniform3(variable.Handler, vector[0], vector[1], vector[2]);
                        break;
                    }
                    case UniformType.IntVec4:
                    {
                        var vector = variable.GetValue<int[]>();
                        gl.Uniform4(variable.Handler, vector[0], vector[1], vector[2], vector[3]);
                        break;
                    }

                    case UniformType.UnsignedInt:
                    {
                        gl.Uniform1(variable.Handler, variable.GetValue<uint>());
                        break;
                    }
                    case UniformType.UnsignedIntVec2:
                    {
                        var vector = variable.GetValue<uint[]>();
                        gl.Uniform2(variable.Handler, vector[0], vector[1]);
                        break;
                    } 
                    case UniformType.UnsignedIntVec3:
                    {
                        var vector = variable.GetValue<uint[]>();
                        gl.Uniform3(variable.Handler, vector[0], vector[1], vector[2]);
                        break;
                    }
                    case UniformType.UnsignedIntVec4:
                    {
                        var vector = variable.GetValue<uint[]>();
                        gl.Uniform4(variable.Handler, vector[0], vector[1], vector[2], vector[3]);
                        break;
                    }
                }
            }
        }

        public T GetVariableValue<T>(string varName)
        {
            if (!variables.ContainsKey(varName))
                throw new VariableNotDefinedException(varName);
            return variables[varName].GetValue<T>();
        }

        public void Make()
        {            
            Handler = gl.CreateProgram();
            foreach (var shader in shaders.Values) {
                if (shader.Handler == 0)
                    shader.Make();
                gl.AttachShader(Handler, shader.Handler);
            }
            
            gl.LinkProgram(Handler);
            string infoLog = gl.GetProgramInfoLog(Handler);
            if (!string.IsNullOrWhiteSpace(infoLog)) {
                gl.DeleteProgram(Handler);
                Handler = 0;
                throw new CompilationFailureException($"Ошибка связывания шейдерной программы: {infoLog}");
            }

            foreach (var variable in variables.Values)
                variable.Handler = gl.GetUniformLocation(Handler, variable.Name);

            foreach (var shader in shaders.Values) {
                gl.DetachShader(Handler, shader.Handler);
                gl.DeleteShader(shader.Handler);
            }
        }

        public void RemoveShader(ShaderType type) =>
            shaders.Remove(type);

        public void RemoveVariable(string name) =>
            variables.Remove(name);

        public void SetVariableValue<T>(string varName, T value)
        {
            if (!variables.ContainsKey(varName))
                throw new VariableNotDefinedException(varName);
            variables[varName].Value = value;
        }
    }
}