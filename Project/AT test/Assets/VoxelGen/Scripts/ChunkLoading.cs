using System.IO;
using UnityEngine;

public class ChunkLoading{

	/*

	public List<chunk> loadChunk(Vector2 pos, chunk currentChunk, Vector3 sizeOfChunk) {
		string filename = pos.ToString();
		List<chunk> visibilityCheck = new List<chunk> ();

		if (!Directory.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory)) {
			Directory.CreateDirectory (Application.dataPath + Path.DirectorySeparatorChar + directory);
		}

		if (File.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt")) {
			StreamReader reader = new StreamReader (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt", true);

			if (reader.ReadLine () != sizeOfChunk.x.ToString() || reader.ReadLine() != sizeOfChunk.y.ToString()
				|| reader.ReadLine() != voxelSize.x.ToString() || reader.ReadLine() != voxelSize.y.ToString() || reader.ReadLine() != voxelSize.z.ToString()
				|| reader.ReadLine() != chunkHeight.ToString()) {
				reader.Close ();
				File.Delete (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt");
				return visibilityCheck;
			}

			int chunkData = int.Parse(reader.ReadLine ());

			currentChunk.chunkPos = pos;

			currentChunk.voxels = new voxel[(int)sizeOfChunk.x, (int)sizeOfChunk.y, (int)chunkHeight];

			int loop = 0;
			int currentTexId = -2;

			for (int a = 0; a < sizeOfChunk.x; a++) {
				for (int b = 0; b < sizeOfChunk.y; b++) {
					for (int c = 0; c < chunkHeight; c++) {
						switch (chunkData) {
						case -1:
							generateVoxel (currentChunk, new vec3I (a, c, b), -2);
							break;
						case 0:
							generateVoxel (currentChunk, new vec3I (a, c, b), 0);
							break;
						case 1:
							if (loop == 0) {
								string line = reader.ReadLine ();
								if (line == "n") {
									loop = int.Parse (reader.ReadLine ());
									currentTexId = int.Parse (reader.ReadLine ());
								} else {
									generateVoxel (currentChunk, new vec3I (a, c, b), int.Parse (line));
								}
							} 
							if (loop != 0) {
								generateVoxel (currentChunk, new vec3I (a, c, b), currentTexId);
								loop--;
							}
							break;
						}
					}
				}
			}

			visibilityCheck = setupChunkNeighbours (currentChunk);
			visibilityCheck.Add (currentChunk);

			reader.Close ();

			return visibilityCheck;

		}

		return visibilityCheck;
	}

*/

}
