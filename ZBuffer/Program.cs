﻿using System;
using System.Drawing;
using Common;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace ZBuffer
{
    class Program
    {
        #region Vertices
        private static uint N;
        const int verCoordCount = 2;
        const int colorSize = 3;
        const int vertexSize = verCoordCount + colorSize;
        private static uint vertexArrayLength => (N + 1) * (N + 1) * vertexSize;
        private static uint indexArrayLength => N * N * 2 * 3;

        private static float[] vertices;
        private static uint[] indices;

        private static MVPMatrix m;
        private static MVPMatrix v;
        #endregion
        
        #region Shaders
        private const string vsh = 
@"#version 330 
            
layout(location = 0) in vec2 a_position;
layout(location = 1) in vec3 a_color;
            
uniform mat4 u_mvp;
uniform mat3 u_n;
uniform float u_mph;
uniform float u_mc;
            
out vec3 v_color;
out vec3 v_normal;
out vec3 v_pos;

void main()
{
    v_color = a_color;
    float x = a_position[0];
    float z = a_position[1];
    float y = u_mph * sin((x * x + z * z) * 3.14 * u_mc);
    v_pos = vec3(x, y, z);
    v_normal = normalize(u_n * vec3(-u_mph * 2.0 * x * u_mc * 3.14 * cos((x * x + z * z) * 3.14 * u_mc), 1.0, -u_mph * 2.0 * z * u_mc * 3.14 * cos((x * x + z * z) * 3.14 * u_mc)));
    gl_Position = u_mvp * vec4(v_pos, 1.0);
}";

        private static string fsh = 
@"#version 330

uniform vec3 u_olpos;
uniform vec3 u_olcol;
uniform vec3 u_oeye;
uniform float u_odmin;
uniform float u_osfoc;
            
in vec3 v_color;
in vec3 v_normal;
in vec3 v_pos;
            
layout(location = 0) out vec4 o_color;
            
void main()
{
    vec3 l = normalize(v_pos - u_olpos);
    float cosa = dot(l, v_normal);
    float d = max(cosa, u_odmin);
    vec3 r = reflect(l, v_normal);
    vec3 e = normalize(u_oeye - v_pos);
    float s = max(pow(dot(r, e), u_osfoc), 0.0) * (int(cosa >= 0.0));
    o_color = vec4(u_olcol * (d * v_color + s), 1.0);
}";
        #endregion

        private static IWindow window;
        private static GL gl;

        private static uint g_shaderProgram;
        private static int g_uMVP;
        private static int g_n;
        private static int g_mph;
        private static int g_mc;

        private static (
            uint Vbo, uint Ibo, uint Vao, uint IndexCount, 
            float PeaksHeight, float Concavity
         ) model = (0, 0, 0, 0, 1.0f, 10.0f);

        private static (
            int g_olpos, int g_olcol, int g_oeye, int g_odmin, int g_osfoc,
            float[] Position, float[] Color, float[] Eye,
            float DMin, float SFoc
        ) g_optics = (
            0, 0, 0, 0, 0,
            new float[]{ N, N, N },
            new float[]{ 1.0f, 1.0f, 1.0f}, 
            new float[]{ 0.0f, 0.0f, 0.0f }, 
            0.25f, SFoc: 4.0f
        );

        const int countOfSpeeds = 9;
        private static float[] degrees = new float[countOfSpeeds];
        private static float degree;

        const float lightMovementBorder = 3.0f;
        const float lightStep = 0.05f;

        static void Main(string[] args)
        {
            N = args.Length > 0 ? N = uint.Parse(args[0]) : 100;

            for (int i = 0; i < countOfSpeeds; i++)
			    degrees[i] = (i + 1) * 0.05f;
            degree = degrees[1];
            Console.WriteLine(
@"Управление:
    Стрелки Лево/Право: вращать вокруг оси Y;
    Стрелки Вверх/Вниз: вращать вокруг оси X;
    Клавиши W/S: вращать вокруг оси Z;");
            Console.WriteLine("\t1-9: выбор скорости вращения.");
            Console.WriteLine(
@"  4/6 (Цифровая клавиатура): перемещение освещения по оси X;
    5/8 (Цифровая клавиатура): перемещение освещения о оси Y;
    7/9 (Цифровая клавиатура): перемещение освещения по оси Z;");
            Console.WriteLine(
@"  -/+: изменение высоты пиков модели;
    -/+(Цифровая клавиатура): изменение вогнутости фрагментов модели;");
            Console.WriteLine("Развлекайтесь!");

            var options = WindowOptions.Default;
            options.Size = new Size(1024, 768);
            options.Title = "Пример работы с Z-буфером (а по пути с освещением и клавиатурным управлением)";
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

            gl.ClearColor(Color.FromArgb(255, 255, 255, 255));
            gl.Enable(EnableCap.DepthTest);

            CreateModel();
            MakeShaderProgram();
        }

        private static unsafe void CreateModel()
        {
            m = MVPMatrix.GetIdentityMatrix().RotateAboutX(45);
            v = MVPMatrix.GetIdentityMatrix().Move(0.0f, 0.0f, -3.0f * N);

            model.Vao = gl.GenVertexArray();
            gl.BindVertexArray(model.Vao);
            model.Vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, model.Vbo);

            vertices = new float[vertexArrayLength];
            FillVertices();

            gl.BufferData(
                BufferTargetARB.ArrayBuffer, 
                vertexArrayLength * sizeof(float), 
                vertices.AsSpan(), BufferUsageARB.StaticDraw
            );
            model.Ibo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, model.Ibo);
            
            indices = new uint[indexArrayLength];
            FillIndices();
            
            model.IndexCount = indexArrayLength;
            gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                model.IndexCount * sizeof(uint),
                indices.AsSpan(), BufferUsageARB.StaticDraw
            );
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(
                0, verCoordCount, VertexAttribPointerType.Float, 
                false, vertexSize * sizeof(float), (void *)0
            );
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(
                1, colorSize, VertexAttribPointerType.Float, false, 
                vertexSize * sizeof(float), (void *)(verCoordCount * sizeof(float))
            );
        }

        private static void FillVertices()
        {
            int i = 0;
            for (int z = 0; z <= N; z++)
                for (int x = 0; x <= N; x++)
                {
                    vertices[i++] = x;
                    vertices[i++] = z;

                    vertices[i++] = 0.25f;
                    vertices[i++] = 0.5f;
                    vertices[i++] = 0.25f;
                }
        }

        private static void FillIndices()
        {
            int i = 0;
            for (uint z = 0; z < N; z++)
            {
                uint nextZ = z + 1;
                uint zN = z * N;
                uint nzN = nextZ * (N + 1);
                for (uint x = 0; x < N; x++)
                {
                    indices[i++] = zN + x + z;
                    indices[i++] = zN + x + nextZ;
                    indices[i++] = nzN + x;
                    indices[i++] = nzN + x;
                    indices[i++] = nzN + x + 1;
                    indices[i++] = zN + x + nextZ;
                }
            }
        }

        private static void MakeShaderProgram()
        {
            uint vertexShader, fragmentShader;
            vertexShader = MakeShader(vsh, ShaderType.VertexShader);
            fragmentShader = MakeShader(fsh, ShaderType.FragmentShader);
            
            g_shaderProgram = gl.CreateProgram();
            gl.AttachShader(g_shaderProgram, vertexShader);
            gl.AttachShader(g_shaderProgram, fragmentShader);
            gl.LinkProgram(g_shaderProgram);
            string infoLog = gl.GetProgramInfoLog(g_shaderProgram);
            if (!string.IsNullOrWhiteSpace(infoLog)) {
                Console.WriteLine($"Error linking shader: {infoLog}");
                gl.DeleteProgram(g_shaderProgram);
            }

            g_uMVP = gl.GetUniformLocation(g_shaderProgram, "u_mvp");
            g_n = gl.GetUniformLocation(g_shaderProgram, "u_n");

            g_optics.g_olpos = gl.GetUniformLocation(g_shaderProgram, "u_olpos");
            g_optics.g_olcol = gl.GetUniformLocation(g_shaderProgram, "u_olcol");
            g_optics.g_oeye = gl.GetUniformLocation(g_shaderProgram, "u_oeye");
            g_optics.g_odmin = gl.GetUniformLocation(g_shaderProgram, "u_odmin");
            g_optics.g_osfoc = gl.GetUniformLocation(g_shaderProgram, "u_osfoc");

            g_mph = gl.GetUniformLocation(g_shaderProgram, "u_mph");
            g_mc = gl.GetUniformLocation(g_shaderProgram, "u_mc");

            gl.DetachShader(g_shaderProgram, vertexShader);
            gl.DeleteShader(vertexShader);

            gl.DetachShader(g_shaderProgram, fragmentShader);
            gl.DeleteShader(fragmentShader);
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

        private static void KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key >= Key.Number1 && key < Key.Number1 + countOfSpeeds)
            {
                degree = degrees[key - Key.Number1];
                return;
            }

            switch (key)
            {
                case Key.Left:
                {
                    m = m.RotateAboutY(degree);
                    break;
                }
                case Key.Right:
                {
                    m = m.RotateAboutY(-degree);
                    break;
                }
                case Key.Up:
                {
                    m = m.RotateAboutX(degree);
                    break;
                }
                case Key.Down:
                {
                    m = m.RotateAboutX(-degree);
                    break;
                }
                case Key.W:
                {
                    m = m.RotateAboutZ(degree);
                    break;
                }
                case Key.S:
                {
                    m = m.RotateAboutZ(-degree);
                    break;
                }

                case Key.Keypad4:
                {
                    g_optics.Position[0] -= lightStep;
                    break;
                }
                case Key.Keypad6:
                {
                    g_optics.Position[0] += lightStep;
                    break;
                }
                case Key.Keypad5:
                {
                    g_optics.Position[1] -= lightStep;
                    break;
                }
                case Key.Keypad8:
                {
                    g_optics.Position[1] += lightStep;
                    break;
                }
                case Key.Keypad7:
                {
                    g_optics.Position[2] -= lightStep;
                    break;
                }
                case Key.Keypad9:
                {
                    g_optics.Position[2] += lightStep;
                    break;
                }

                case Key.Minus:
                {
                    if (model.PeaksHeight > 1.0f)
                        model.PeaksHeight -= 0.01f;
                    break;
                }
                case Key.Equal:
                {
                    model.PeaksHeight += 0.01f;
                    break;
                }
                case Key.KeypadSubtract:
                {
                    if (model.Concavity > 1.0f)
                        model.Concavity -= 0.01f;
                    break;
                }
                case Key.KeypadAdd:
                {
                    model.Concavity += 0.01f;
                    break;
                }

                case Key.Escape:
                {
                    window.Close();
                    break;
                }
            }
        }

        private static unsafe void OnRender(double obj)
        {
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.UseProgram(g_shaderProgram);
            gl.BindVertexArray(model.Vao);
            MVPMatrix mv = v * m;
            MVPMatrix p = MVPMatrix.GetPerspectiveProjectionMatrix(N, -N, window.Size.Width, window.Size.Height, 50.0f);
            gl.UniformMatrix4(g_uMVP, 1, false, (p * mv).Content);
            float[] nMatrix = mv.GetNMatrix();
            gl.UniformMatrix3(g_n, 1, true, nMatrix);
            
            gl.Uniform3(g_optics.g_olpos, 1, g_optics.Position);
            gl.Uniform3(g_optics.g_olcol, 1, g_optics.Color);
            gl.Uniform3(g_optics.g_oeye, 1, g_optics.Eye);
            gl.Uniform1(g_optics.g_odmin, g_optics.DMin);
            gl.Uniform1(g_optics.g_osfoc, g_optics.SFoc);
            gl.Uniform1(g_mph, model.PeaksHeight);
            gl.Uniform1(g_mc, model.Concavity);

            gl.DrawElements(PrimitiveType.Triangles, model.IndexCount, DrawElementsType.UnsignedInt, null);
        }

        private static void OnResize(Size newSize)
        {
            gl.Viewport(newSize);
        }

        private static void OnClose()
        {
            gl.DeleteBuffer(model.Vbo);
            gl.DeleteBuffer(model.Ibo);
            gl.DeleteVertexArray(model.Vao);
            gl.DeleteProgram(g_shaderProgram);
        }
    }
}
