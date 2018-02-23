using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class ChunkSaving {

	public static void saveWorldData(VoxelCreation voxGen) {
		string dir = Application.dataPath + Path.DirectorySeparatorChar + voxGen.directory + Path.DirectorySeparatorChar;

		if (!Directory.Exists(dir)) {
			Directory.CreateDirectory (dir);
		}

		if (File.Exists (dir + "WorldData.txt")) {
			File.Delete (dir + "WorldData.txt");
		}

		StreamWriter writer = new StreamWriter (dir + "WorldData.txt");

		writer.WriteLine (voxGen.sizeOfChunk.x);
		writer.WriteLine (voxGen.sectionHeight);
		writer.WriteLine (voxGen.sizeOfChunk.y);
		writer.WriteLine (voxGen.chunkSections);
		writer.WriteLine (voxGen.voxelSize.x);
		writer.WriteLine (voxGen.voxelSize.y);
		writer.WriteLine (voxGen.voxelSize.z);
		writer.WriteLine (voxGen.smoothing);
		writer.WriteLine (voxGen.randSeed);

		writer.Close ();

	}

	public static void saveSection(VoxelCreation voxGen, section _section) {
		string dir = Application.dataPath + Path.DirectorySeparatorChar + voxGen.directory + Path.DirectorySeparatorChar;
		char split = Path.DirectorySeparatorChar;

		if (!voxGen.worldDataSaved) {
			saveWorldData (voxGen);
			voxGen.worldDataSaved = true;
		}

		checkDirectoryExists (dir + split + _section.parent.chunkPos.ToString());

		if (File.Exists (dir + _section.parent.chunkPos.ToString () + split + "section " + _section.sectionNum.ToString() + ".txt")) {
			File.Delete (dir + _section.parent.chunkPos.ToString () + split + "section " + _section.sectionNum.ToString() + ".txt");
		}

		StreamWriter writer = new StreamWriter (dir + _section.parent.chunkPos.ToString () + split + "section " + _section.sectionNum.ToString() + ".txt");

		if (_section.voxels [0, 0, 0] == null) {
			writer.Close ();
			return;
		}

		int loop = -1;
		int prevObjID = _section.voxels [0, 0, 0].objID;
		bool updateFile = false;
		bool lastVoxel = false;

		for (int a = 0; a < voxGen.sizeOfChunk.x; a++) {
			for (int b = 0; b < voxGen.sizeOfChunk.y; b++) {
				for (int c = 0; c < voxGen.sectionHeight; c++) {

					if ((a == voxGen.sizeOfChunk.x - 1 && b == voxGen.sizeOfChunk.y - 1 && c == voxGen.sectionHeight - 1)) {
						updateFile = true;
						lastVoxel = true;
					} else {
						if (c == 0) {
							if (_section.sectionNum == 0) {
								loop++;
								continue;
							}
						}
					}

					if (_section.voxels [a, b, c].objID != prevObjID) {
						updateFile = true;
					} else {
						loop++;
					}

					if (updateFile) {
						if (loop > 0) {
							writer.Write ("n");
							writer.Write (',');
							writer.Write (loop);
							writer.Write (',');
						}
						loop = 0;
						writer.Write (prevObjID);
						if (lastVoxel) {
							if (_section.voxels [a, b, c].objID != prevObjID) {
								writer.Write (',');
								writer.Write (_section.voxels [a, b, c].objID);
							}
						} else {
							writer.Write (',');
						}
						updateFile = false;
						prevObjID = _section.voxels [a, b, c].objID;
					}

				}
			}
		}

		writer.Close ();

	}

	public static bool saveChunk(VoxelCreation voxGen, chunk _chunk) {

		if (!voxGen.worldDataSaved) {
			saveWorldData (voxGen);
			voxGen.worldDataSaved = true;
		}

		char split = Path.DirectorySeparatorChar;
		string dir = Application.dataPath + split + voxGen.directory + split;

		checkDirectoryExists (dir + split + _chunk.chunkPos.ToString());

		if (File.Exists (dir + _chunk.chunkPos.ToString () + split + "ChunkData.txt")) {
			File.Delete (dir + _chunk.chunkPos.ToString () + split + "ChunkData.txt");
		}

		StreamWriter writer = new StreamWriter (dir + _chunk.chunkPos.ToString () + split + "ChunkData.txt");

		if (_chunk.yHeight != null) {

			for (int a = 0; a < voxGen.sizeOfChunk.x; a++) {
				for (int b = 0; b < voxGen.sizeOfChunk.y; b++) {
					writer.WriteLine (_chunk.yHeight [a, b]);
				}
			}

		}

		foreach (int i in _chunk.requiredSections) {
			writer.WriteLine (i);
		}

		writer.Close ();

		foreach (section sec in _chunk.sections) {
			saveSection(voxGen, sec);
		}

		return true;
	}

	static void checkDirectoryExists(string name) {
		if (!Directory.Exists(name)) {
			Directory.CreateDirectory (name);
		}
	}
}
