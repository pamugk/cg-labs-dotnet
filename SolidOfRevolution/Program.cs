using System;
using System.Collections.Generic;
using System.Drawing;
using Common;
using Silk.NET.Input;
using Silk.NET.Input.Common;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace SolidOfRevolution
{
    using KeyHandler = System.Action<IKeyboard, Key, int>;
    class Program
    {
        private static IWindow window;
        private static GL gl;

        enum States {
            BasePoints,
            Curve,
            SolidOfRevolution
        };

        private static States state;
        private static Dictionary<States, KeyHandler> keyHandlers = new Dictionary<States, KeyHandler>
        {
            {
                States.BasePoints,
                BasicPointsKeyDown
            },
            {
                States.Curve,
                CurveKeyDown
            },
            {
                States.SolidOfRevolution,
                SolidOfResolutionKeyDown
            }
        };

        private static Model model;
        private static float[] vertices;
        private static uint[] indices;
        private static uint g_shaderProgram;

        private static int g_uMVP; //MVP
        private static MVPMatrix v;
        private static int g_n;

        #region Shaders
        const string vsh =
@"#version 330

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_color;

uniform mat4 u_mvp;
uniform mat3 u_n;
uniform float u_mph;
uniform float u_mc;

out vec3 v_color;
out vec3 v_pos;
out vec3 v_normal;

void main()
{
    v_color = a_color;
    v_pos = a_position;
    v_normal = vec3(0.0,0.0,0.0);
    gl_Position = u_mvp * vec4(a_position, 1.0);
}";

const string fsh =
@"#version 330

uniform vec3 u_olpos;
uniform vec3 u_olcol;
uniform vec3 u_oeye;
uniform float u_odmin;
uniform float u_osfoc;
uniform bool u_lie;

in vec3 v_color;
in vec3 v_pos;
in vec3 v_normal;

layout(location = 0) out vec4 o_color;

void main()
{
   vec3 l = normalize(v_pos - u_olpos);
   float cosa = dot(l, v_normal);
   float d = max(cosa, u_odmin);
   vec3 r = reflect(l, v_normal);
   vec3 e = normalize(u_oeye - v_pos);
   float s = max(pow(dot(r, e), u_osfoc), 0.0) * (int(cosa >= 0.0));
   o_color = vec4(v_color, 1.0);
}";
        #endregion
        #region Optics
            private static LED led;
            private static (
                    int g_oeye, int g_odmin, int g_osfoc,
                    float[] Position, float[] Color, float[] Eye,
                    float DMin, float SFoc
                ) g_optics = (
                    0, 0, 0,
                    new float[]{ 0.0f, 0.0f, 0.0f },
                    new float[]{ 1.0f, 1.0f, 1.0f}, 
                    new float[]{ 0.0f, 0.0f, 0.0f }, 
                    0.5f, 4.0f
                );
        #endregion
        
        const int countOfSpeeds = 9;
        private static float[] degrees = new float[countOfSpeeds];
        private static float degree;

        const float lightMovementBorder = 3.0f;
        const float lightStep = 0.05f;

        static void Main(string[] args)
        {
            vertices = new float[0];
            indices = new uint[0];

            v = MVPMatrix.GetIdentityMatrix().Move(0.0f, 0.0f, 2.0f);
            model = new Model();
            model.PointRadius = 0.015; 

            led = new LED();
            led.Enabled = false;
            model.SetColor(0.25f, 0.5f, 0.25f);
            state = States.BasePoints;
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
            Console.WriteLine("Развлекайтесь!");

            var options = WindowOptions.Default;
            options.Size = new Size(1024, 768);
            options.Title = "Пример динамического формирования модели";
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
            
            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Mice[i].Click += OnMouseButtonPress;
            gl = GL.GetApi(window);

            gl.ClearColor(Color.FromArgb(255, 255, 255, 255));
            gl.Enable(EnableCap.DepthTest);

            CreateModel();
            MakeShaderProgram();
        }

        private static unsafe void CreateModel()
        {
            model.Vao = gl.GenVertexArray();
            gl.BindVertexArray(model.Vao);
            model.Vbo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, model.Vbo);
            model.Ibo = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, model.Ibo);

            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
        }

        private static unsafe void UpdateModel()
        {
            switch (state)
            {
                case States.BasePoints:
                {
                    vertices = model.BaseVertices.ToArray();
                    indices = model.BaseIndices.ToArray();
                    break;
                }
                case States.Curve:
                {
                    vertices = model.CurveVertices.ToArray();
                    indices = model.CurveIndices.ToArray();
                    break;
                }
                case States.SolidOfRevolution:
                {
                    vertices = model.ModelVertices.ToArray();
                    indices = model.ModelIndices.ToArray();
                    break;
                }
            }

            gl.BindVertexArray(model.Vao);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, model.Vbo);
            gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), vertices.AsSpan(), BufferUsageARB.StaticDraw);

            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, model.Ibo);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)), indices.AsSpan(), BufferUsageARB.StaticDraw);

            gl.VertexAttribPointer(0, Model.VerCoordCount, VertexAttribPointerType.Float, false, Model.VertexSize * sizeof(float), (void*)0);
            gl.VertexAttribPointer(1, Model.ColorCount, VertexAttribPointerType.Float, false, Model.VertexSize * sizeof(float), (void*)(Model.VerCoordCount * sizeof(float)));
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

            led.PositionHandler = gl.GetUniformLocation(g_shaderProgram, "u_olpos");
            led.ColorHandler = gl.GetUniformLocation(g_shaderProgram, "u_olcol");
            g_optics.g_oeye = gl.GetUniformLocation(g_shaderProgram, "u_oeye");
            g_optics.g_odmin = gl.GetUniformLocation(g_shaderProgram, "u_odmin");
            g_optics.g_osfoc = gl.GetUniformLocation(g_shaderProgram, "u_osfoc");
            led.StateHandler = gl.GetUniformLocation(g_shaderProgram, "u_lie");

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
            keyHandlers[state](keyboard, key, arg3);
        }

        private static void BasicPointsKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Space:
                {
                    model.ClearBasePoints();
                    UpdateModel();
                    break;
                }
                case Key.E:
                {
                    model.FormCurve();
                    state = States.Curve;
                    UpdateModel();
                    break;
                }
            }
        }

        private static void CurveKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Q:
                {
                    model.ClearCurve();
                    state = States.BasePoints;
                    UpdateModel();
                    break;
                }
                case Key.E:
                {
                    model.FormSolidOfRevolution(15.0f);
                    state = States.SolidOfRevolution;
                    led.Enabled = true;
                    UpdateModel();
                    break;
                }
            }
        }

        private static void SolidOfResolutionKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key >= Key.Number1 && key < Key.Number1 + countOfSpeeds)
            {
                degree = degrees[key - Key.Number1];
                return;
            }
            switch (key)
            {
                case Key.Q:
                {
                    model.ClearSolidOfRevolution();
                    model.FormCurve();
                    led.Enabled = false;
                    state = States.Curve;
                    UpdateModel();
                    break;
                }
                case Key.Left:
                {
                    model.RotateAboutY(degree);
                    break;
                }
                case Key.Right:
                {
                    model.RotateAboutY(-degree);
                    break;
                }
                case Key.Up:
                {
                    model.RotateAboutX(degree);
                    break;
                }
                case Key.Down:
                {
                    model.RotateAboutX(-degree);
                    break;
                }
                case Key.W:
                {
                    model.RotateAboutZ(degree);
                    break;
                }
                case Key.S:
                {
                    model.RotateAboutZ(-degree);
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

                case Key.Escape:
                {
                    window.Close();
                    break;
                }
            }
        }

        private static void OnMouseButtonPress(IMouse mouse, MouseButton button)
        {
            if (button != MouseButton.Left || state != States.BasePoints)
                return;
            
            double w_2 = window.Size.Width / 2, h_2 = window.Size.Height / 2;
            model.AddBasePoint(new Point2D(
                (mouse.Position.X - w_2) / w_2, 
                ((window.Size.Height - mouse.Position.Y) - h_2) / h_2)
            );
            UpdateModel();
        }

        private static unsafe void OnRender(double obj)
        {
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            
            gl.UseProgram(g_shaderProgram);
            gl.BindVertexArray(model.Vao);
            
            MVPMatrix mv = v * model.m;
            MVPMatrix p = MVPMatrix.GetParallelProjectionMatrix(-1.0f, 1.0f, -1.0f, 1.0f, -3.0f, 3.0f);
            gl.UniformMatrix4(g_uMVP, 1, false, (p * mv).Content);
            float[] nMatrix = mv.GetNMatrix();
            gl.UniformMatrix3(g_n, 1, true, nMatrix);
            
            gl.Uniform3(led.PositionHandler, 1, led.Position);
            gl.Uniform3(led.ColorHandler, 1, led.Color);
            gl.Uniform3(g_optics.g_oeye, 1, g_optics.Eye);
            gl.Uniform1(g_optics.g_odmin, g_optics.DMin);
            gl.Uniform1(g_optics.g_osfoc, g_optics.SFoc);

            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
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
