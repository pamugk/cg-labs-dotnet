using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        #region Глобальные переменные
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
        private static ShaderProgram shaderProgram;

        private static MVPMatrix v;
        private static MVPMatrix p;
        
        private const int countOfSpeeds = 9;
        private static float[] degrees = new float[countOfSpeeds];
        private static float degree;

        private const float lightMovementBorder = 3.0f;
        private const float lightStep = 0.05f;
        #endregion

        static void Main(string[] args)
        {
            vertices = new float[0];
            indices = new uint[0];

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

            v = MVPMatrix.GetIdentityMatrix().Move(0.0f, 0.0f, 2.0f);
            p = MVPMatrix.GetParallelProjectionMatrix(-1.0f, 1.0f, -1.0f, 1.0f, -3.0f, 3.0f);
            model = new Model();
            model.PointRadius = 0.015;
            model.SetColor(0.25f, 0.5f, 0.25f);

            CreateModel();

            shaderProgram = new ShaderProgram(gl);

            shaderProgram.AddShaders(
                new Shader(gl, File.ReadAllText("Shaders/VertexShader.glsl"), ShaderType.VertexShader),
                new Shader(gl, File.ReadAllText("Shaders/FragmentShader.glsl"), ShaderType.FragmentShader)
            );

            var mvpVar = new ShaderVariable("u_mvp", UniformType.FloatMat4);
            mvpVar.SetOption("transpose", false);

            var nVar = new ShaderVariable("u_n", UniformType.FloatMat3);
            nVar.SetOption("transpose", true);

            shaderProgram.AddVariables(
                mvpVar, nVar,
                new ShaderVariable("u_olpos", UniformType.FloatVec3)
                {
                    Value = new float[]{ 0.0f, 0.0f, 0.0f }
                },
                new ShaderVariable("u_olcol", UniformType.FloatVec3)
                {
                    Value = new float[]{ 1.0f, 1.0f, 1.0f}
                },
                new ShaderVariable("u_oeye", UniformType.FloatVec3)
                {
                    Value = new float[]{ 0.0f, 0.0f, 0.0f }
                },
                new ShaderVariable("u_odmin", UniformType.Float)
                {
                    Value = 0.5f
                },
                new ShaderVariable("u_osfoc", UniformType.Float)
                {
                    Value = 4.0f
                },
                new ShaderVariable("u_lie", UniformType.Bool)
                {
                    Value = false
                }
            );

            shaderProgram.Make();
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

        #region Обработчики нажатий на клавиши
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
                    shaderProgram.SetVariableValue("u_lie", true);
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
                    shaderProgram.SetVariableValue("u_lie", false);
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
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[0] -= lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
                    break;
                }
                case Key.Keypad6:
                {
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[0] += lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
                    break;
                }
                case Key.Keypad5:
                {
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[1] -= lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
                    break;
                }
                case Key.Keypad8:
                {
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[1] += lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
                    break;
                }
                case Key.Keypad7:
                {
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[2] -= lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
                    break;
                }
                case Key.Keypad9:
                {
                    var position = shaderProgram.GetVariableValue<float[]>("u_olpos");
                    position[2] += lightStep;
                    shaderProgram.SetVariableValue("u_olpos", position);
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
        #endregion

        private static unsafe void OnRender(double obj)
        {
            gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            gl.UseProgram(shaderProgram.Handler);
            gl.BindVertexArray(model.Vao);
            MVPMatrix mv = v * model.m;
            shaderProgram.SetVariableValue("u_mvp", (p * mv).Content);
            shaderProgram.SetVariableValue("u_n", mv.GetNMatrix());
            shaderProgram.BindVariableValues();
            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
        }

        private static void OnResize(Size newSize) =>
            gl.Viewport(newSize);

        private static void OnClose()
        {
            gl.DeleteBuffer(model.Vbo);
            gl.DeleteBuffer(model.Ibo);
            gl.DeleteVertexArray(model.Vao);
            gl.DeleteProgram(shaderProgram.Handler);
        }
    }
}
