namespace SolidOfRevolution
{
    internal class LED
    {
        public int PositionHandler {get;set;}
        public int ColorHandler {get;set;}
        public int StateHandler {get;set;}

        public float[] Position { get; private set;} = { 1, 1, 1 }; 
        public float[] Color {get;set;} = { 1.0f, 1.0f, 1.0f } ;
        public bool Enabled {get;set;} = true;

        public void Move(float dx, float dy, float dz)
        {
            MoveAlongX(dx);
            MoveAlongY(dy);
            MoveAlongZ(dz);
        }

        public void MoveAlongX(float dx) =>
            Position[0] += dx;

        public void MoveAlongY(float dy) =>
            Position[1] += dy;

        public void MoveAlongZ(float dz) =>
            Position[2] += dz;
    }
}