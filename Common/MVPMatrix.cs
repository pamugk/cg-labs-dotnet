using System;
using System.Linq;

namespace Common
{
    class MVPMatrix
    {
        //Размерность матрицы
        private const int n = 4;
        //Число элементов в матрице
        private const int N = n * n;
        //Содержимое матрицы
        public float[] Content { get; set; }

        public MVPMatrix()
        {
            Content = new float[N];
        }
        
        //Конструктор матрицы, принимающий содержимое,
        //записанное по стандарту OpenGL
        MVPMatrix(float[] content)
        {
            if (content.Length != N)
                throw new ArgumentException();
            Content = content;
        }

        //Метод извлечения данных для N-матрицы 
        public float[] GetNMatrix()
        {
            const int n1 = 3;
            const int N1 = n1 * n1;
            var m = new float[N1];
            for (int i = 0; i < n1; i++)
                for (int j = 0; j < n1; j++)
                    m[i * n1 + j] = Content[i * n + j];

            float det = 
                m[0] * m[4] * m[8] + m[3] * m[7] * m[2] + m[1] * m[5] * m[6] -
                m[6] * m[4] * m[2] - m[1] * m[3] * m[8] - m[5] * m[7] * m[0];

            float[] nMatrixContent =
            {
                m[4] * m[8] - m[5] * m[7], m[7] * m[2] - m[1] * m[8], m[1] * m[5] - m[4] * m[2],
                m[6] * m[5] - m[3] * m[8], m[0] * m[8] - m[2] * m[6], m[3] * m[2] - m[0] * m[5],
                m[3] * m[7] - m[6] * m[4], m[1] * m[6] - m[0] * m[7], m[0] * m[4] - m[3] * m[1]
            };

            for (int i = 0; i < N1; i++)
                nMatrixContent[i] /= det;

            return nMatrixContent;
        }

        //Метод для транспонирования матрицы
        public MVPMatrix Transpose()
        {
            var transposedContent = new float[N];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    transposedContent[i * n + j] = Content[j * n + i];
            return new MVPMatrix(transposedContent);
        }

        //Метод для преобразования переноса
        public MVPMatrix Move(float x, float y, float z) => 
            new MVPMatrix(new[]{
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                x, y, z, 1.0f
            }) * this;

        //Метод для преобразования масштабирования
        public MVPMatrix Scale(float sx, float sy, float sz) =>
            new MVPMatrix(new[]{
                sx, 0.0f, 0.0f, 0.0f,
                0.0f, sy, 0.0f, 0.0f,
                0.0f, 0.0f, sz, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            }) * this;

        //Метод для преобразования вращения вокруг X
        public MVPMatrix RotateAboutX(float degree) =>
            Rotate(1.0f, 0.0f, 0.0f, degree);

        //Метод для преобразования вращения вокруг Y
        public MVPMatrix RotateAboutY(float degree) =>
            Rotate(0.0f, 1.0f, 0.0f, degree);

        //Метод для преобразования вращения вокруг Z
        public MVPMatrix RotateAboutZ(float degree) =>
            Rotate(0.0f, 0.0f, 1.0f, degree);

        //Метод для преобразования вращения (|(x, y, z)| = 1)
        public MVPMatrix Rotate(float x, float y, float z, float degree)
        {
            float c = MathF.Cos(degree);
            float s = MathF.Sin(degree);
            return new MVPMatrix(
                new[]
                {
                    x * x * (1.0f - c) + c, y * x * (1.0f - c) + z * s, x * z * (1.0f - c) - y * s, 0.0f,
                    x * y * (1.0f - c) - z * s, y * y * (1.0f - c) + c, y * z * (1.0f - c) + x * s, 0.0f,
                    x * z * (1.0f - c) + y * s, y * z * (1.0f - c) - x * s, z * z * (1.0f - c) + c, 0.0f,
                    0.0f, 0.0f, 0.0f, 1.0f
                }) * this;
        }

        public override bool Equals(object obj) =>
            obj is MVPMatrix matrix && this == matrix;

        //Метод для формирования единичной матрицы
        public static MVPMatrix GetIdentityMatrix() =>
            new MVPMatrix(new[]
            {
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            });

        //Метод для формирования матрицы параллельной проекции
        public static MVPMatrix GetParallelProjectionMatrix(
            float l, float r, float b, float t, float n, float f
        ) =>
            new MVPMatrix(new[]
            {
                2.0f / (r - l), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (t - b), 0.0f, 0.0f,
                0.0f, 0.0f, -2.0f / (f - n), 0.0f,
                -(r+l)/(r-l), -(t+b)/(t-b), -(f+n)/(f-n), 1.0f
            });

        //Метод для формирования матрицы перспективной проекции
        public static MVPMatrix GetPerspectiveProjectionMatrix(
            float l, float r, float b, float t, float n, float f
        ) =>
            new MVPMatrix(new[]
                {
                    2.0f * n / (r - l), 0.0f, 0.0f, 0.0f,
                    0.0f, 2.0f * n / (t - b), 0.0f, 0.0f,
                    (r + l) / (r - l), (t + b) / (t - b), - (f + n) / (f - n), -1.0f,
                    0.0f, 0.0f, -2.0f *f * n / (f - n), 0.0f
                });

        //Метод для формирования матрицы перспективной проекции
        public static MVPMatrix GetPerspectiveProjectionMatrix(
            float n, float f, float w, float h, float fovAngle
        )
        {
            float tg = MathF.Tan(MathF.PI / 180.0f * fovAngle / 2.0f);
            return GetPerspectiveProjectionMatrix(-n * tg, n * tg, -n * w / h *tg, n * w / h * tg, n, f);
        }

        //Оператор "равно"
        public static bool operator ==(MVPMatrix matrix1, MVPMatrix matrix2) =>
            matrix1.Content.Equals(matrix2);

        //Оператор "не равно"
        public static bool operator !=(MVPMatrix matrix1, MVPMatrix matrix2) =>
            !(matrix1 == matrix2);

        //Оператор сложения матриц
        public static MVPMatrix operator +(MVPMatrix matrix1, MVPMatrix matrix2)
        {
            var sum = new float[N];
            for (int i = 0; i < N; i++)
                sum[i] = matrix1.Content[i] + matrix2.Content[i];
            return new MVPMatrix(sum);
        }

        //Оператор вычитания матриц
        public static MVPMatrix operator -(MVPMatrix matrix1, MVPMatrix matrix2)
        {
            var dif = new float[N];
            for (int i = 0; i < N; i++)
                dif[i] = matrix1.Content[i] - matrix2.Content[i];
            return new MVPMatrix(dif);
        }

        //Оператор умножения матриц
        public static MVPMatrix operator * (MVPMatrix multipliedMatrix, MVPMatrix multiplier)
        {
            var newContent = new float[N];
            for(int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < n; k++)
                        sum += multipliedMatrix.Content[k * n + i] * multiplier.Content[j * n + k];
                    newContent[j * n + i] = sum;
                }
            return new MVPMatrix(newContent);
        }

        //Оператор умножения матрицы на число
        public static MVPMatrix operator *(MVPMatrix matrix, float num) =>
            new MVPMatrix(matrix.Content.Select(element => element * num).ToArray());

        //Оператор деления матрицы на число
        public static MVPMatrix operator /(MVPMatrix matrix, float num) =>
            new MVPMatrix(matrix.Content.Select(element => element / num).ToArray());
    }
}