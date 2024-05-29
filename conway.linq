<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Drawing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Globalization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Threading.Tasks.Parallel.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var stats = new DumpContainer().Dump();
	var dc = new DumpContainer().Dump();
	var sizeX = 25;
	var sizeY = 25;
	var scale = 3;
	var space = 1;
	var ratio = scale + space;

	var b = new Bitmap(sizeX * ratio, sizeY * ratio);
	var fb = new FastBitmap(b);
	var game = new ConwayGame();

	game.Set(ConwayGame.GenerateCells(sizeX, sizeY, true, 5));
	
	var black = Color.Black;
	var white = Color.White;
	var didBoardChange = true;
	var isLooping = false;
	var frame = 0;
	var cycleLength = 0;
	var cycleStart = 0;
	
	var hashcode = game.GetHashCode();
	var hashcodes = new List<int>();
	var hashcodesSet = new HashSet<int>();
	hashcodesSet.Add(hashcode);
	
	while (didBoardChange && !isLooping)
	{
		fb.LockImage();

		var length = game.Cells.GetLength(0);
		var height = game.Cells.GetLength(1);
		var actions = new Action[length];

		for (int x = 0; x < length; x++)
		{
			for (int y = 0; y < height; y++)
			{
				var yRatio = y * ratio;
				var xRatio = x * ratio;

				for (int m = 0; m < scale; m++)
				{
					for (int n = 0; n < scale; n++)
					{
						fb.SetPixel(xRatio + n, yRatio + m, game.Cells[x, y] ? black : white);
					}
				}
			}
		}

		fb.UnlockImage();
		dc.Content = b;
		dc.Refresh();

		(didBoardChange, hashcode) = game.MoveNext();
		
		if (hashcodesSet.Contains(hashcode)) 
		{
			isLooping = true;
			cycleStart = hashcodes.IndexOf(hashcode);
			cycleLength = hashcodes.Count - cycleStart;
		}
		
		hashcodesSet.Add(hashcode);
		hashcodes.Add(hashcode);

		stats.Content = new { frame, cycleStart, cycleLength, didBoardChange, isLooping, hashcode };
		stats.Refresh();

		await Task.Delay(50);
		
		frame++;
	}
}

public class ConwayGame
{
	public bool[,] Cells;
	private bool[,] _newCells;
	private static Random _rnd;

	static ConwayGame()
	{
		_rnd = new Random();
	}

	public override int GetHashCode()
	{
		/* Thanks to the prime, the distribution of hashcodes should be fairly well distributed
		 *   but the possibility of a collision remains (of course).
		 */
		var prime = 397;
		var length = Cells.GetLength(0);
		var height = Cells.GetLength(1);

		unchecked // Allows arithmetic overflow
		{
			int result = 0;

			for (int x = 0; x < length; x++)
			{
				for (int y = 0; y < height; y++)
				{
					if (Cells[x, y])
					{
						result = (result * prime) ^ ((x * length) + y);
					}
				}
			}
			
			return result;
		}
	}

	public void Set(bool[,] data)
	{
		Cells = data;
		_newCells = new bool[Cells.GetLength(0), Cells.GetLength(1)];
	}

	// Randomly generate game board
	public static bool[,] GenerateCells(int x, int y, bool useRandom = true, int modulus = 25)
	{
		var rows = new bool[x, y];

		if (useRandom)
		{
			for (int h = 0; h < x; h++)
			{
				for (int i = 0; i < y; i++)
				{
					rows[h, i] = _rnd.Next(100) % modulus == 0;
				}
			}
		}

		return rows;
	}

	public (bool, int) MoveNext()
	{
		var length = Cells.GetLength(0);
		var height = Cells.GetLength(1);
		var actions = new Action[length];
		var anyChanged = false;
		var prime = 397;
		var hashcode = 0;

		for (int x = 0; x < length; x++)
		{
			for (int y = 0; y < height; y++)
			{
				var count = Adjacents(x, y).Count(a => a);
				var alive = Cells[x, y];

				/* From Wikipedia: https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life
				 * 1. Any live cell with fewer than two live neighbors dies, as if by underpopulation.
				 * 2. Any live cell with two or three live neighbors lives on to the next generation.
				 * 3. Any live cell with more than three live neighbors dies, as if by overpopulation.
				 * 4. Any dead cell with exactly three live neighbors becomes a live cell, as if by reproduction.
				 */

				// Since the default state is false on the new board, we only need to check for rules 2 and 4
				if ((count == 3 && !alive) || (alive && (count == 2 || count == 3)))
				{
					/* Have to set the state on a new game board, then copy it over.
					 * If the state is updated incrementally, each update affects the 
					 *   next and Conway's GOL rules don't apply as they should.
					 */
					_newCells[x, y] = true;
					anyChanged = true;
					hashcode = (hashcode * prime) ^ ((x * length) + y);
				}
			}
		}

		Set(_newCells);

		return (anyChanged, hashcode);
	}

	private IEnumerable<bool> Adjacents(int x, int y)
	{
		var xLength = Cells.GetLength(0) - 1;
		var yLength = Cells.GetLength(1) - 1;

		if (x > 0) yield return Cells[x - 1, y];
		else yield return false;

		if (x < xLength) yield return Cells[x + 1, y];
		else yield return false;

		if (y > 0) yield return Cells[x, y - 1];
		else yield return false;

		if (y < yLength) yield return Cells[x, y + 1];
		else yield return false;

		if (x > 0 && y > 0) yield return Cells[x - 1, y - 1];
		else yield return false;

		if (x < xLength && y < yLength) yield return Cells[x + 1, y + 1];
		else yield return false;

		if (x > 0 && y < yLength) yield return Cells[x - 1, y + 1];
		else yield return false;

		if (x < xLength && y > 0) yield return Cells[x + 1, y - 1];
		else yield return false;
	}
}


// Marking this unsafe allows us direct memory access to the pixels
unsafe class FastBitmap
{
	private struct PixelData
	{
		public byte Blue;
		public byte Green;
		public byte Red;
		public byte Alpha;

		public override string ToString()
		{
			return "(" + Alpha.ToString(CultureInfo.InvariantCulture) + ", "
				   + Red.ToString(CultureInfo.InvariantCulture) + ", "
				   + Green.ToString(CultureInfo.InvariantCulture) + ", "
				   + Blue.ToString(CultureInfo.InvariantCulture) + ")";
		}
	}

	private readonly Bitmap _workingBitmap;
	private int _width;
	private BitmapData _bitmapData;
	private Byte* _pBase = null;

	public FastBitmap(Bitmap inputBitmap)
	{
		_workingBitmap = inputBitmap;
	}

	public void LockImage()
	{
		Rectangle bounds = new Rectangle(Point.Empty, _workingBitmap.Size);

		_width = bounds.Width * sizeof(PixelData);
		if (_width % 4 != 0) _width = 4 * (_width / 4 + 1);

		//Lock Image
		_bitmapData = _workingBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		_pBase = (Byte*)_bitmapData.Scan0.ToPointer();
	}

	private PixelData* _pixelData = null;

	public Color GetPixel(int x, int y)
	{
		_pixelData = (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
		return Color.FromArgb(_pixelData->Alpha, _pixelData->Red, _pixelData->Green, _pixelData->Blue);
	}

	public Color GetPixelNext()
	{
		_pixelData++;
		return Color.FromArgb(_pixelData->Alpha, _pixelData->Red, _pixelData->Green, _pixelData->Blue);
	}

	public void SetPixel(int x, int y, Color color)
	{
		PixelData* data = (PixelData*)(_pBase + y * _width + x * sizeof(PixelData));
		data->Alpha = color.A;
		data->Red = color.R;
		data->Green = color.G;
		data->Blue = color.B;
	}

	public void UnlockImage()
	{
		_workingBitmap.UnlockBits(_bitmapData);
		_bitmapData = null;
		_pBase = null;
	}
}