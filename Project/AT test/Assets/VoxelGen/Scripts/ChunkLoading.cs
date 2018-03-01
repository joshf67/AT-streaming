using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class ChunkLoading {

	public static void loadWorldData(VoxelCreation voxGen) {

		char split = Path.DirectorySeparatorChar;
		string dir = Application.dataPath + split + voxGen.directory + split;

		if (File.Exists (dir + "WorldData.txt")) {
			StreamReader reader = new StreamReader (dir + "WorldData.txt", true);

			if (reader.ReadLine () != voxGen.sizeOfChunk.x.ToString () || reader.ReadLine () != voxGen.sectionHeight.ToString () || reader.ReadLine () != voxGen.sizeOfChunk.y.ToString ()
				|| reader.ReadLine () != voxGen.chunkSections.ToString () || reader.ReadLine () != voxGen.voxelSize.x.ToString () || reader.ReadLine () != voxGen.voxelSize.y.ToString ()
				|| reader.ReadLine () != voxGen.voxelSize.z.ToString () || reader.ReadLine() != voxGen.smoothing.ToString()) {
				reader.Close ();
				Directory.Delete (dir, true);
				return;
			}

			voxGen.randSeed = float.Parse (reader.ReadLine ());

			reader.Close ();
		}
	}


	public static section loadSection (VoxelCreation voxGen, int sectionNumber, chunk currentChunk) {
		section returnVal = null;
		char split = Path.DirectorySeparatorChar;
		string dir = Application.dataPath + split + voxGen.directory + split;

		if (File.Exists (dir + currentChunk.chunkPos.ToString () + split + "section " + sectionNumber.ToString() + ".txt")) {
			StreamReader reader = new StreamReader (dir + currentChunk.chunkPos.ToString () + split + "section " + sectionNumber.ToString() + ".txt", true);

			returnVal = new section (voxGen.sizeOfChunk.x, voxGen.sizeOfChunk.y, voxGen.sectionHeight, currentChunk, sectionNumber);

			string savedData = reader.ReadToEnd ();
			string[] lines = savedData.Split (',');

			if (lines.Length == 1) {
				reader.Close ();
				return returnVal;
			}

			int loop = -1;
			int currentObjId = -2;
			int currentLine = 0;

			for (int a = 0; a < voxGen.sizeOfChunk.x; a++) {
				for (int b = 0; b < voxGen.sizeOfChunk.y; b++) {
					for (int c = 0; c < voxGen.sectionHeight; c++) {
						if (loop == -1) {
							string action = lines [currentLine++];
							if (action == "n") {
								loop = int.Parse (lines [currentLine++]);
								currentObjId = int.Parse (lines [currentLine++]);
							} else {
								if (action != "") {
									voxGen.generateVoxel (returnVal, new vec3I (a, c, b), int.Parse (action));
								}
							}
						} 
						if (loop != -1) {
							voxGen.generateVoxel (returnVal, new vec3I (a, c, b), currentObjId);
							loop--;
						}
					}
				}
			}

			reader.Close ();

		}

		return returnVal;
	}



	public static List<chunk> loadChunk (VoxelCreation voxGen, Vector2 pos, chunk currentChunk)
	{
		char split = Path.DirectorySeparatorChar;
		string dir = Application.dataPath + split + voxGen.directory + split;
		List<chunk> visibilityCheck = new List<chunk> ();

		if (File.Exists (dir + pos.ToString () + split + "ChunkData.txt")) {

			StreamReader reader = new StreamReader (dir + pos.ToString () + split + "ChunkData.txt", true);

			string inputTest = null;

			currentChunk.yHeight = new double[voxGen.sizeOfChunk.x, voxGen.sizeOfChunk.y];

			for (int a = 0; a < voxGen.sizeOfChunk.x; a++) {
				for (int b = 0; b < voxGen.sizeOfChunk.y; b++) {
					inputTest = reader.ReadLine ();
					if (inputTest == "") {
						continue;
					}
					if (inputTest == null) {
						currentChunk.yHeight = null;
						continue;
					}
					currentChunk.yHeight [a, b] = double.Parse (inputTest);
				}
			}

			reader.Close ();

			if (currentChunk.yHeight == null) {
				voxGen.generateYHeight (currentChunk);
			}

		}

		currentChunk.chunkPos = pos;

		//change to loading individual chunks?
		for (int a = 0; a < voxGen.chunkSections; a++) {
			section tempSection = loadSection (voxGen, a, currentChunk);
			if (tempSection != null) {
				currentChunk.sections.Add (tempSection);
			}
		}

		visibilityCheck = voxGen.setupChunkNeighbours (currentChunk);
		visibilityCheck.Add (currentChunk);
		return visibilityCheck;

	}



	public static section forceSectionLoad(VoxelCreation voxGen, int sectionNum, Vector2 chunkPos, bool hidden = true) {

		int chunkIndex = voxGen.checkChunkExists (chunkPos);
		chunk tempChunk = null;

		if (chunkIndex != -1) {
			tempChunk = voxGen.chunks [chunkIndex];
		} else {
			tempChunk = ChunkLoading.forceChunkLoad (voxGen, chunkPos);
		}

		return forceSectionLoad (voxGen, sectionNum, tempChunk, hidden);
	}





	public static section forceSectionLoad(VoxelCreation voxGen, int sectionNum, chunk _chunk, bool hidden = true) {

		_chunk.immune = 3;

		section temp = ChunkLoading.loadSection (voxGen, sectionNum, _chunk);

		if (temp == null) {
			foreach (chunk __chunk in voxGen.createChunk (_chunk)) {
				if (!hidden) {
					voxGen.toCheckVisibility.Add(__chunk);
				}
			}
		}

		return temp;
	}




	public static chunk forceChunkLoad(VoxelCreation voxGen, Vector2 chunkPos, bool hidden = true) {
		if (voxGen.checkChunkExists (chunkPos) == -1) {

			voxGen.chunks.Add (new chunk ());

			List<chunk> newVisibilityCheck = ChunkLoading.loadChunk (voxGen, chunkPos, voxGen.chunks [voxGen.chunks.Count - 1]);

			if (newVisibilityCheck.Count == 0) {

				voxGen.chunks [voxGen.chunks.Count - 1].chunkPos = chunkPos + voxGen.playerPos;

				newVisibilityCheck = voxGen.createChunk (voxGen.chunks [voxGen.chunks.Count - 1]);

			}

			if (hidden) {
				newVisibilityCheck.Clear ();
			}

			foreach (chunk _chunk in newVisibilityCheck) {
				voxGen.toCheckVisibility.Add(_chunk);
			}

			voxGen.chunks [voxGen.chunks.Count - 1].immune = 3;

			return voxGen.chunks [voxGen.chunks.Count - 1];
		}

		return null;
	}



}
