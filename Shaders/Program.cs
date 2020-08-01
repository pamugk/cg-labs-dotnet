using System;
using System.Collections.Generic;
using System.Drawing;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace Shaders
{
    class Program
    {
        enum Variants
        {
            LINES,
            WAVES,
            CIRCLES
        }

        private static Variants variant;

        #region Vertices
        private static readonly float[] vertices = {
            -1.0f, -1.0f, -1.0f, -1.0f, 0.0f, //0
             1.0f, -1.0f, -1.0f,  1.0f, 0.0f, //1
             1.0f,  1.0f,  1.0f,  1.0f, 0.0f, //2
            -1.0f,  1.0f,  1.0f, -1.0f, 0.0f //3
        };

        private static readonly uint[] indices = {
            0, 1, 2, 2, 3, 0
        };
        #endregion

        #region Shaders
        private const string vsh = @"
        #version 330

        layout(location = 0) in vec2 a_position; 
        layout(location = 1) in vec3 a_color; 

        out vec3 v_color; 

        void main()
        {
            v_color = a_color;
            gl_Position = vec4(a_position, 0.0f, 1.0f);
        }
        ";

        private static Dictionary<Variants, string> fragmentShaders = new Dictionary<Variants, string>
        {
            {
                Variants.LINES,
                @"
                #version 330
                
                in vec3 v_color;
                
                layout(location = 0) out vec4 o_color;
                
                void main()
                {
                    o_color = vec4(sin(v_color[0] * 10 * 3.1415926) * 0.5 + 0.5, 0.0, 0.0, 1.0);
                }"
            },
            {
                Variants.WAVES,
                @"
                #version 330

                in vec3 v_color;

                layout(location = 0) out vec4 o_color;

                void main()
                {
                    o_color = vec4(sin(20 * v_color[0] * 3.1415926 + 5 * sin(10 * 3.1415926 * v_color[1])), 0.0, 0.0, 1.0);
                }"
            },
            {
                Variants.CIRCLES,
                @"
                #version 330

                in vec3 v_color;

                layout(location = 0) out vec4 o_color;

                void main()
                {
                    o_color = vec4(sin(100 * length(v_color) * 3.1415926), 0.0, 0.0, 1.0);
                }"
            }
        };
        #endregion
        
        private static IWindow window;
        private static GL gl;
        private static uint vbo; //VertexBufferObject
        private static uint ibo; //IndexBufferObject
        private static uint vao; //VertexArrayObject
        private static uint program;

        static void Main(string[] args)
        {
            variant = Variants.LINES;
            if (
                args.Length > 0 &&
                Enum.TryParse(args[0], true, out Variants passedVariant)
            )
                variant = passedVariant;
                
            var options = WindowOptions.Default;
            options.Size = new Size(1024, 768);
            options.Title = "Пример использования шейдеров в OpenGL";
            window = Window.Create(options);

            window.Load += OnLoad;
            window.Render += OnRender;
            window.Resize += OnResize;
            window.Closing += OnClose;

            window.Run();
        }

        private static void OnLoad()
        {
            IInputContext input = window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Keyboards[i].KeyDown += KeyDown;
            gl = GL.GetApi(window);
            CreateModel();
            MakeShaderProgram();
            gl.ClearColor(Color.FromArgb(255, 255, 255, 255));
        }

        private static unsafe void CreateModel()
        {
            vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            gl.BufferData(
                BufferTargetARB.ArrayBuffer, 
                (uint)vertices.Length * sizeof(float),
                vertices.AsSpan(), BufferUsageARB.StaticDraw
            );

            ibo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer,ibo);
            gl.BufferData(
                BufferTargetARB.ElementArrayBuffer, 
                (uint)indices.Length * sizeof(uint),
                indices.AsSpan(), BufferUsageARB.StaticDraw
            );

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));
        }

        private static void MakeShaderProgram()
        {
            uint vShader = MakeShader(vsh, ShaderType.VertexShader);
            uint fShader = MakeShader(fragmentShaders[variant], ShaderType.FragmentShader);

            program = gl.CreateProgram();
            gl.AttachShader(program, vShader);
            gl.AttachShader(program, fShader);
            gl.LinkProgram(program);
            string infoLog = gl.GetProgramInfoLog(program);
            if (!string.IsNullOrWhiteSpace(infoLog)) {
                Console.WriteLine($"Error linking shader: {infoLog}");
                gl.DeleteProgram(program);
            }

            gl.DetachShader(program, vShader);
            gl.DeleteShader(vShader);

            gl.DetachShader(program, fShader);
            gl.DeleteShader(fShader);
        }

        private static uint MakeShader(string code, ShaderType type)
        {
            uint shader = gl.CreateShader(type);
            gl.ShaderSource(shader, code);
            gl.CompileShader(shader);

            string infoLog = gl.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(infoLog))
                Console.WriteLine($"Error compiling vertex shader: {infoLog}");
            return shader;
        }

        private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
                window.Close();
        }

        private static unsafe void OnRender(double obj)
        {
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            gl.UseProgram(program);
            gl.BindVertexArray(vao);

            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
        }

        private static void OnResize(Size newSize)
        {
            gl.Viewport(newSize);
        }

        private static void OnClose()
        {
            gl.DeleteBuffer(vbo);
            gl.DeleteBuffer(ibo);
            gl.DeleteVertexArray(vao);
            gl.DeleteProgram(program);
        }
    }
}
