namespace Textures
{
    struct Texture
	{
		public uint ID { get;set; }
		public int MapLocation { get;set; }
		public string MapName { get; set; }
		public string File { get;set; }
		public int Width { get; set;}
		public int Height {get;set;}

		public Texture(string mapName, string file)
		{
			ID = 0;
			MapLocation = 0;
			MapName = mapName;
			File = file;
			Width = 0;
			Height = 0;
		}
	};
}