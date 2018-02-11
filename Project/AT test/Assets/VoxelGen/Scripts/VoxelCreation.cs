using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

public class VoxelCreation : MonoBehaviour {

	[Header("Other Variables")]
	public GameObject player;
	public int objectType = 0;
	public Vector2 playerPos;
	public float explosionDistance = 1;

	[Space(20)]
	[Header("StreamData")]
	public string directory;
	public bool saveChunks;

	[Space(20)]
	[Header("Grid Variables")]
	public Vector2 sizeOfChunk;
	public int chunkHeight = 0;
	public int chunkSections = 0;
	[SerializeField]
	public List<chunk> chunks = new List<chunk>();
	public Vector2 chunkRange = Vector2.zero;
	public bool enableDynamicChunkLoading = true;
	public bool debugMode = false;
	public int smoothing = 0;
	public float perlinDist;
	public float randSeed;
	public float noise1Effect = 0;
	public float noise2Effect = 0;

	[Header("Voxel details")]
	public Vector3 voxelSize;
	public List<Material> voxelMaterial = new List<Material>();
	public List<GameObject> voxelObjects = new List<GameObject>();

	[Space(20)]
	[Header("Cave")]
	public bool firstPass;
	public bool secondPass;
	public float maxChanceOfCave;
	public float chanceOfCave;
	public float secondCavePass;
	public float improveCaveChance;

	bool firstLoad = true;

	bool testEmpty(chunk _chunk) {
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {
					if (c != 0) {
						if (!_chunk.voxels [a, b, c].destroyed) {
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	void checkDirectoryExists(Vector2 pos) {
		if (!Directory.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory)) {
			Directory.CreateDirectory (Application.dataPath + Path.DirectorySeparatorChar + directory);
		}

		if (!Directory.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + pos.ToString())) {
			Directory.CreateDirectory (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + pos.ToString());
		}
	}

	List<chunk> loadChunk (Vector2 pos, chunk currentChunk)
	{
		string filename = pos.ToString ();
		List<chunk> visibilityCheck = new List<chunk> ();

		if (File.Exists (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + pos.ToString () + Path.DirectorySeparatorChar + filename + ".txt")) {
			StreamReader reader = new StreamReader (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + pos.ToString () + Path.DirectorySeparatorChar + filename + ".txt", true);

			if (reader.ReadLine () != sizeOfChunk.x.ToString () || reader.ReadLine () != chunkHeight.ToString () || reader.ReadLine () != sizeOfChunk.y.ToString ()
			    || reader.ReadLine () != voxelSize.x.ToString () || reader.ReadLine () != voxelSize.y.ToString () || reader.ReadLine () != voxelSize.z.ToString ()) {
				reader.Close ();
				File.Delete (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + pos.ToString () + Path.DirectorySeparatorChar + filename + ".txt");
				return visibilityCheck;
			}

			currentChunk.chunkState = int.Parse (reader.ReadLine ());

			currentChunk.chunkPos = pos;

			currentChunk.voxels = new voxel[(int)sizeOfChunk.x, (int)sizeOfChunk.y, (int)chunkHeight];

			visibilityCheck = setupChunkNeighbours (currentChunk);
			visibilityCheck.Add (currentChunk);

			string savedData = reader.ReadToEnd ();
			string[] lines = savedData.Split (',');

			int loop = -1;
			int currentObjId = -2;
			int currentLine = 0;

			if (currentChunk.chunkState == 1) {
				for (int a = 0; a < sizeOfChunk.x; a++) {
					for (int b = 0; b < sizeOfChunk.y; b++) {
						for (int c = 0; c < chunkHeight; c++) {
							if (loop == -1) {
								string action = lines [currentLine++];
								if (action == "n") {
									loop = int.Parse (lines [currentLine++]);
									currentObjId = int.Parse (lines [currentLine++]);
								} else {
									generateVoxel (currentChunk, new vec3I (a, c, b), int.Parse (action));
								}
							} 
							if (loop != -1) {
								generateVoxel (currentChunk, new vec3I (a, c, b), currentObjId);
								loop--;
							}
						}
					}
				}
			} else {
				switch (currentChunk.chunkState) {
				case -1:
					for (int a = 0; a < sizeOfChunk.x; a++) {
						for (int b = 0; b < sizeOfChunk.y; b++) {
							for (int c = 0; c < chunkHeight; c++) {
								generateVoxel (currentChunk, new vec3I (a, c, b), 0);
							}
						}
					}
					break;
				case 1:
					generateChunkVoxels (currentChunk);
					break;
				}
			}

			reader.Close ();

			return visibilityCheck;

		}

		return visibilityCheck;
	}

	bool saveChunk(chunk _chunk) {
		string filename = _chunk.chunkPos.ToString();

		checkDirectoryExists (_chunk.chunkPos);

		if (File.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + _chunk.chunkPos.ToString() + Path.DirectorySeparatorChar + filename + ".txt")) {
			File.Delete (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + _chunk.chunkPos.ToString() + Path.DirectorySeparatorChar + filename + ".txt");
		}

		//test chunkState
		if (testEmpty (_chunk)) {
			_chunk.chunkState = -1;
		}

		StreamWriter writer = new StreamWriter (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + _chunk.chunkPos.ToString() + Path.DirectorySeparatorChar + filename + ".txt");

		writer.WriteLine (sizeOfChunk.x);
		writer.WriteLine (chunkHeight);
		writer.WriteLine (sizeOfChunk.y);
		writer.WriteLine (voxelSize.x);
		writer.WriteLine (voxelSize.y);
		writer.WriteLine (voxelSize.z);

		if (_chunk.chunkState == 0) {
			_chunk.chunkState = 1;
		}

		writer.WriteLine (_chunk.chunkState);

		if (_chunk.chunkState != -1) {

			int loop = -1;
			int prevObjID = _chunk.voxels [0, 0, 0].objID;
			bool updateFile = false;
			bool lastVoxel = false;

			for (int a = 0; a < sizeOfChunk.x; a++) {
				for (int b = 0; b < sizeOfChunk.y; b++) {
					for (int c = 0; c < chunkHeight; c++) {

						//writer.WriteLine (_chunk.voxels [a, b, c].texId);

						if (_chunk.voxels [a, b, c].objID != prevObjID) {
							updateFile = true;
						} else {
							loop++;
						}

						if ((a == sizeOfChunk.x - 1 && b == sizeOfChunk.y - 1 && c == chunkHeight - 1)) {
							updateFile = true;
							lastVoxel = true;
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
								if (_chunk.voxels [a, b, c].objID != prevObjID) {
									writer.Write (',');
									writer.Write (_chunk.voxels [a, b, c].objID);
								}
							} else {
								writer.Write (',');
							}
							updateFile = false;
							prevObjID = _chunk.voxels [a, b, c].objID;
						}

					}
				}
			}

		}

		writer.Close ();

		return true;
	}

	//change to only affect chunks everytime an update is needed
	List<chunk> resetChunkNeighbours(chunk _chunk) {
		List<chunk> visibilityReset = new List<chunk> ();
		if (_chunk.left != null) {
			_chunk.left.right = null;
			visibilityReset.Add (_chunk.left);
		}

		if (_chunk.right != null) {
			_chunk.right.left = null;
			visibilityReset.Add (_chunk.right);
		}

		if (_chunk.forward != null) {
			_chunk.forward.back = null;
			visibilityReset.Add (_chunk.forward);
		}

		if (_chunk.back != null) {
			_chunk.back.forward = null;
			visibilityReset.Add (_chunk.back);
		}
		return visibilityReset;
	}

	void deleteChunkVoxels(chunk _chunk) {
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {
					if (_chunk.voxels [a, b, c].obj != null) {
						Destroy (_chunk.voxels [a, b, c].obj);
					}
				}
			}
		}
	}

	bool checkWithinDistance(chunk _chunk, Vector2 otherChunkPos) {
		
		if (otherChunkPos.x + chunkRange.x < _chunk.chunkPos.x || otherChunkPos.x - chunkRange.x > _chunk.chunkPos.x ||
			otherChunkPos.y + chunkRange.y < _chunk.chunkPos.y || otherChunkPos.y - chunkRange.y > _chunk.chunkPos.y) {
			return false;
		}
		return true;
	}

	void generateVoxel(chunk parent, vec3I pos, int objID) {
		parent.voxels [pos.x, pos.z, pos.y] = new voxel();

		parent.voxels [pos.x, pos.z, pos.y].parent = parent;

		parent.voxels [pos.x, pos.z, pos.y].pos = new Vector3 (pos.x * voxelSize.x, pos.y * voxelSize.y, pos.z * voxelSize.z) + new Vector3(parent.chunkPos.x * sizeOfChunk.x * voxelSize.x, 0, parent.chunkPos.y * sizeOfChunk.y * voxelSize.z);

		if (objID == 0) {
			parent.voxels [pos.x, pos.z, pos.y].destroyed = true;
			parent.voxels [pos.x, pos.z, pos.y].objID = 0;
		} else {
			parent.voxels [pos.x, pos.z, pos.y].objID = objID;
		}

		if (pos.y == 0) {
			parent.voxels [pos.x, pos.z, pos.y].destroyed = false;
			parent.voxels [pos.x, pos.z, pos.y].destroyable = false;
			parent.voxels [pos.x, pos.z, pos.y].objID = 1;
		}
	}

	List<chunk> setupChunkNeighbours(chunk _chunk) {

		List<chunk> visibilityCheck = new List<chunk> ();
		
		//setup neighbours
		//left
		int left = checkChunkExists(_chunk.chunkPos + new Vector2(-1,0));
		if (left != -1) {
			_chunk.left = chunks [left];
			chunks [left].right = _chunk;
			visibilityCheck.Add (chunks [left]);
		}

		//right
		int right = checkChunkExists(_chunk.chunkPos + new Vector2(1,0));
		if (right != -1) {
			_chunk.right = chunks [right];
			chunks [right].left = _chunk;
			visibilityCheck.Add (chunks [right]);
		}

		//forward
		int forward = checkChunkExists(_chunk.chunkPos + new Vector2(0,1));
		if (forward != -1) {
			_chunk.forward = chunks [forward];
			chunks [forward].back = _chunk;
			visibilityCheck.Add (chunks [forward]);
		}

		//back
		int back = checkChunkExists(_chunk.chunkPos + new Vector2(0,-1));
		if (back != -1) {
			_chunk.back = chunks [back];
			chunks [back].forward = _chunk;
			visibilityCheck.Add (chunks [back]);
		}

		return visibilityCheck;

	}

	float findHighestHeight(chunk _chunk, Vector2 xz) {
		float highest = 0;
		voxel curr = _chunk.voxels [(int)xz.x, (int)xz.y, 0];
		while (curr != null) {
			if (highest + 1 < chunkHeight) {
				curr = _chunk.voxels [(int)xz.x, (int)xz.y, (int)highest + 1];
				if (curr != null) {
					highest++;
				}
			} else {
				return _chunk.voxels [(int)xz.x, (int)xz.y, (int)highest + 1].pos.y;
			}
		}
		return _chunk.voxels [(int)xz.x, (int)xz.y, (int)highest + 1].pos.y;
	}

	List<chunk> generateChunkVoxels(chunk _chunk) {

		List<chunk> visibilityCheck = new List<chunk> ();

		_chunk.voxels = new voxel[(int)sizeOfChunk.x, (int)sizeOfChunk.y, (int)chunkHeight];

		vec3I sizeOfChunkI = new vec3I (sizeOfChunk.x, sizeOfChunk.y, 0);

		double[] yHeight = new double[sizeOfChunkI.x * sizeOfChunkI.y];

		//generate random height
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				//yHeight [(a * sizeOfChunkI.y) + b] = UnityEngine.Random.Range (0, chunkHeight) * randSeed;
				//yHeight [(a * sizeOfChunkI.y) + b] = UnityEngine.Random.Range (0, chunkHeight - 2);
				//yHeight [(a * sizeOfChunkI.y) + b] = Mathf.PerlinNoise(((_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x) + a) * perlinDist * randSeed, ((_chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z) + b) * perlinDist * randSeed) * chunkHeight;
				float noise1 = Mathf.PerlinNoise(((_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x) + a) * perlinDist * randSeed, ((_chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z) + b) * perlinDist * randSeed);
				float noise2 = Mathf.PerlinNoise (UnityEngine.Random.Range (1, int.MaxValue) / UnityEngine.Random.Range (1, int.MaxValue), UnityEngine.Random.Range (1, int.MaxValue) / UnityEngine.Random.Range (1, int.MaxValue));
				yHeight [(a * sizeOfChunkI.y) + b] = ((noise1 * noise1Effect) - (noise2 * noise2Effect)) * chunkHeight;
			}
		}

		//smooth times
		for (int sm = 0; sm < smoothing; sm++) {

			//smooth random height
			for (int a = 0; a < sizeOfChunk.x; a++) {
				for (int b = 0; b < sizeOfChunk.y; b++) {
					double height = yHeight [(a * sizeOfChunkI.y) + b];
					int sides = 0;

					if (a > 0) {
						height += yHeight [((a - 1) * sizeOfChunkI.y) + b];
						sides++;

						if (b < sizeOfChunkI.y - 1) {
							height += yHeight [((a - 1) * sizeOfChunkI.y) + b + 1];
							sides++;
						}

						if (b > 0) {
							height += yHeight [((a - 1) * sizeOfChunkI.y) + b - 1];
							sides++;
						}

					} else {
						if (_chunk.left != null) {
							height += findHighestHeight (_chunk.left, new Vector2 (sizeOfChunk.x - 1, b));
							sides++;
						}
					}

					if (a < sizeOfChunkI.x - 1) {
						height += yHeight [((a + 1) * sizeOfChunkI.y) + b];
						sides++;

						if (b < sizeOfChunkI.y - 1) {
							height += yHeight [((a + 1) * sizeOfChunkI.y) + b + 1];
							sides++;
						}

						if (b > 0) {
							height += yHeight [((a + 1) * sizeOfChunkI.y) + b - 1];
							sides++;
						}
					} else {
						if (_chunk.right != null) {
							height += findHighestHeight (_chunk.right, new Vector2 (0, b));
							sides++;
						}
					}

					if (b > 0) {
						height += yHeight [(a * sizeOfChunkI.y) + b - 1];
						sides++;
					} else {
						if (_chunk.forward != null) {
							height += findHighestHeight (_chunk.forward, new Vector2 (a, 0));
							sides++;
						}
					}

					if (b < sizeOfChunkI.y - 1) {
						height += yHeight [(a * sizeOfChunkI.y) + b + 1];
						sides++;
					} else {
						if (_chunk.back != null) {
							height += findHighestHeight (_chunk.back, new Vector2 (a, sizeOfChunk.y - 1));
							sides++;
						}
					}

					yHeight [(a * sizeOfChunkI.y) + b] = height / sides;
				}
			}
		}

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {

				for (int c = 0; c < chunkHeight; c++) {

					if (c > yHeight [(a * sizeOfChunkI.y) + b]) {
						generateVoxel (_chunk, new vec3I (a, c, b), 0);
					} else {
						if (c > yHeight [(a * sizeOfChunkI.y) + b] - 2) {
							generateVoxel (_chunk, new vec3I (a, c, b), 2);
						} else {
							generateVoxel (_chunk, new vec3I (a, c, b), 1);
						}
					}

				}
			}
		}

		visibilityCheck = setupChunkNeighbours (_chunk);

		visibilityCheck.Add (_chunk);

		return visibilityCheck;
	}

	void chunkVisibleCheck(chunk _chunk) {

		if (_chunk == null) {
			return;
		}

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {
					if (!_chunk.voxels [a, b, c].destroyed) {
						if (testVoxel (_chunk.voxels [a, b, c])) {
							if (_chunk.voxels [a, b, c].obj == null) {
								voxelVisible (_chunk.voxels [a, b, c], true);
							}
						} else if (_chunk.voxels [a, b, c].obj != null) {
							voxelVisible (_chunk.voxels [a, b, c], false);
						}
					} else if (_chunk.voxels [a, b, c].obj != null) {
						voxelVisible (_chunk.voxels [a, b, c], false);
					}
				}
			}
		}

	}
		
	void cavePass(chunk _chunk) {
		voxel gridVoxel;
		float chance;
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 1; c < chunkHeight; c++) {

					gridVoxel = _chunk.voxels [a, b, c];

					if (!gridVoxel.destroyed) {

						chance = 0;

						foreach(voxel vox in findNeighbours(gridVoxel.pos, _chunk)) {
							if (vox.destroyed) {
								chance += improveCaveChance;
							}
						}

						if (chance != 0) {
							chance += secondCavePass;
							if (UnityEngine.Random.Range (0, maxChanceOfCave) < chance) {
								destroyVoxel (gridVoxel);
							}
						} else {

						}
					}
				}
			}
		}
	}

	void firstCavePass(chunk _chunk) {
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {
					if (c != 0) {
						if (UnityEngine.Random.Range (0, maxChanceOfCave) < chanceOfCave) {
							destroyVoxel (_chunk.voxels [a, b, c]);
						}
					}
				}
			}
		}
	}

	int checkChunkExists(Vector2 input) {
		for(int a = 0; a < chunks.Count; a++) {
			if (input == chunks[a].chunkPos) {
				return a;
			}
		}
		return -1;
	}

	bool compareChunks(chunk input, chunk compare) {
		return input.chunkPos == compare.chunkPos;
	}

	chunk posToChunk(Vector3 pos) {
		int chunkIndex = checkChunkExists (new Vector2 (Mathf.FloorToInt (pos.x / (sizeOfChunk.x * voxelSize.x)), Mathf.FloorToInt (pos.z / (sizeOfChunk.y * voxelSize.z))));
		if (chunkIndex != -1) {
			return chunks[chunkIndex];
		}
		return null;
	}

	vec3I posToVoxel(Vector3 pos, Vector2 _chunkPos) {
		chunk tempChunk = posToChunk (_chunkPos);
		if (tempChunk != null) {
			return posToVoxel (new Vector3 (pos.x, pos.y, pos.z), tempChunk);
		}
		return vec3I.zero ();
	}

	vec3I posToVoxel(vec3I pos, chunk _chunk) {
		return posToVoxel(new Vector3(pos.x, pos.y, pos.z), _chunk);
	}

	vec3I posToVoxel(Vector3 pos, chunk _chunk) {
		Vector3 _pos = pos;

		_pos.x -= _chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x;
		_pos.z -= _chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z;
		_pos = Functions.vec3Div (_pos, voxelSize);

		//turn pos into int
		//return voxel position within chunk
		return new vec3I (_pos);
	}

	int posToSection(int yPos, chunk _chunk) {
		return Mathf.FloorToInt(yPos / chunkSections);
	}

	vec3I posToVoxel(vec3I pos, section _section) {
		return new vec3I ();
	}



	void Start() {
		player.GetComponent<Rigidbody> ().isKinematic = true;

		//chunk tempChunk = new chunk ();

		//generateChunkVoxels (tempChunk);
		//chunkVisibleCheck (tempChunk);

		randSeed = UnityEngine.Random.Range (1, int.MaxValue);
		randSeed /= int.MaxValue;
	}


	void Update() {

		playerPos = new Vector2(Mathf.Floor((player.transform.position.x + (voxelSize.x / 2)) / (sizeOfChunk.x * voxelSize.x)),
			Mathf.Floor((player.transform.position.z + (voxelSize.z / 2)) / (sizeOfChunk.y * voxelSize.z)));

		List<chunk> toDelete = new List<chunk>();
		List<chunk> toCheckVisibility = new List<chunk> ();

		if (enableDynamicChunkLoading) {

			//check if chunks are still needed
			foreach (chunk _chunk in chunks) {
				if (!checkWithinDistance (_chunk, playerPos)) {
					//if (_chunk.chunkEdited) {
					saveChunk (_chunk);
					//}
					deleteChunkVoxels (_chunk);
					List<chunk> tempChunks = resetChunkNeighbours (_chunk);
					foreach (chunk __chunk in tempChunks) {
						if (!toCheckVisibility.Contains (__chunk)) {
							toCheckVisibility.Add (__chunk);
						}
					}
					toDelete.Add (_chunk);
				}
			}

			//remove all chunks not needed
			foreach (chunk _chunk in toDelete) {
				toCheckVisibility.Remove (_chunk);
				chunks.Remove (_chunk);
			}

			List<Vector2> chunksToGenerate = new List<Vector2> ();

			//generate new chunks based on player pos
			for (float a = -chunkRange.x; a <= chunkRange.x; a++) {
				for (float b = -chunkRange.y; b <= chunkRange.y; b++) {
					if (checkChunkExists (new Vector2 (a, b) + playerPos) == -1) {
						chunksToGenerate.Add (new Vector2 (a, b));
					}
				}
			}

			//sort chunks into closest first
			bool work = true;
			while (work) {
				work = false;

				float dist1;
				float dist2;

				for (int a = 0; a < chunksToGenerate.Count - 1; a++) {
					dist1 = Vector2.Distance (chunksToGenerate [a], playerPos);
					dist2 = Vector2.Distance (chunksToGenerate [a + 1], playerPos);
					if (dist1 > dist2) {
						work = true;
						Vector2 temp = chunksToGenerate [a];
						chunksToGenerate [a] = chunksToGenerate [a + 1];
						chunksToGenerate [a + 1] = temp;
					}
				}
			}

			foreach (Vector2 chunkPos in chunksToGenerate) {
				chunks.Add (new chunk ());

				List<chunk> newVisibilityCheck = loadChunk (chunkPos + playerPos, chunks [chunks.Count - 1]);

				if (newVisibilityCheck.Count == 0) {

					chunks [chunks.Count - 1].chunkPos = chunkPos + playerPos;

					newVisibilityCheck = generateChunkVoxels (chunks [chunks.Count - 1]);

				}

				foreach (chunk _chunk in newVisibilityCheck) {
					if (!toCheckVisibility.Contains (_chunk)) {
						toCheckVisibility.Add (_chunk);
					}
				}
			}

		}

		//check visibility on chunks needed
		foreach (chunk _chunk in toCheckVisibility) {
			chunkVisibleCheck (_chunk);
		}


		if (firstPass) {
			foreach (chunk _chunk in chunks) {
				firstCavePass (_chunk);
			}
			firstPass = false;
		}
		if (secondPass) {
			foreach (chunk _chunk in chunks) {
				cavePass (_chunk);
			}
			secondPass = false;
		}
		if (saveChunks) {
			foreach (chunk _chunk in chunks) {
				saveChunk (_chunk);
			}
			saveChunks = false;
			Debug.Log ("Saved");
		}

		//change texture
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			if (objectType > 0) {
				objectType--;
			}
		}

		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			if (objectType < voxelObjects.Count - 1) {
				objectType++;
			}
		}

		if (debugMode) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position;
				chunk tempChunk = posToChunk (hitPos);
				if (tempChunk != null) {
					vec3I voxelPos = posToVoxel (hitPos, tempChunk);
					debugVoxel(tempChunk.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
				}
			}
		}

		//destroy/place
		if (Input.GetMouseButtonDown (2)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				explode (hit.point, explosionDistance);
			}
		}

		//destroy/place
		if (Input.GetMouseButtonDown (0)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position;
				chunk tempChunk = posToChunk (hitPos);
				if (tempChunk != null) {
					vec3I voxelPos = posToVoxel (hitPos, tempChunk);
					if (!testVoxelPosInBounds(voxelPos)) {
						return;
					}
					destroyVoxel(tempChunk.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
				}
			}
		}

		if (Input.GetMouseButtonDown (1)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position + returnSideHitPos(hit);
				chunk tempChunk = posToChunk (hitPos);
				if (tempChunk != null) {
					vec3I voxelPos = posToVoxel (hitPos, tempChunk);
					if (!testVoxelPosInBounds(voxelPos)) {
						return;
					}
					placeObject (voxelPos, tempChunk);
				}
			}
		}

		if (firstLoad) {
			firstLoad = false;
			player.GetComponent<Rigidbody> ().isKinematic = false;
			player.transform.position = new Vector3 (0, (chunkHeight * voxelSize.y) + 2, 0);
		}

	}

	public void explode(Vector3 position, float explosionDist) {
		for (float a = -explosionDist; a < explosionDist; a++) {
			for (float b = -explosionDist; b < explosionDist; b++) {
				for (float c = -explosionDist; c < explosionDist; c++) {
					if (Vector3.Distance (position + Functions.vec3Times(new Vector3 (a, b, c), voxelSize), position) < explosionDist) {
						Vector3 hitPos = position + Functions.vec3Times(new Vector3 (a, b, c), voxelSize);
						chunk tempChunk = posToChunk (hitPos);
						if (tempChunk != null) {
							vec3I voxelPos = posToVoxel (hitPos, tempChunk);
							if (!testVoxelPosInBounds(voxelPos)) {
								continue;
							}
							if (position + new Vector3 (a, b, c) != position) {
								if (tempChunk.voxels[voxelPos.x, voxelPos.z, voxelPos.y].obj != null) {
									if (tempChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y].obj.name == "TNT") {
										tempChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y].obj.GetComponent<TNT> ().trigger ();
									}
								}
							}
							destroyVoxel(tempChunk.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
						}
					}
				}
			}
		}
	}

	void debugVoxel(voxel _voxel) {
		string debugOutput = null;

		debugOutput += "Voxel Data:\n";
		debugOutput += "Voxel Name: " + voxelObjects [_voxel.objID].name;
		debugOutput += "\nPosition:  X=" + _voxel.pos.x;
		debugOutput += "\n               Y=" + _voxel.pos.y;
		debugOutput += "\n               Z=" + _voxel.pos.z;
		debugOutput += "\n             ";
		debugOutput += "\nObject ID:   " + _voxel.objID;
		debugOutput += "\nDestroyable: " + _voxel.destroyable;

		debugOutput += "\nSection Data:\n";
		debugOutput += "(reserved):\n";

		debugOutput += "\nChunk Data:\n";
		debugOutput += "\nChunk Position: " + _voxel.parent.chunkPos;
		debugOutput += "\nChunk Edited: " + _voxel.parent.chunkEdited;
		debugOutput += "\nChunk State: " + _voxel.parent.chunkState;

		GetComponent<LineRenderer> ().SetPosition (0, player.transform.position);
		GetComponent<LineRenderer> ().SetPosition (1, _voxel.pos);
		GameObject.FindObjectOfType<Text> ().text = debugOutput;
	}

	bool testVoxelPosInBounds(vec3I input) {
		if (input.x < 0 || input.x > sizeOfChunk.x - 1 ||
			input.y < 0 || input.y > chunkHeight - 1 ||
			input.z < 0 || input.z > sizeOfChunk.y - 1) {
			return false;
		}
		return true;
	}

	void placeObject(vec3I voxelToChange, chunk _chunk) {
		voxel vox = _chunk.voxels[voxelToChange.x, voxelToChange.z, voxelToChange.y];

		if (objectType != 0) {

			_chunk.chunkState = 1;

			if (vox.destroyed) {
				vox.destroyed = false;
				vox.placed = true;
				vox.objID = objectType;
				voxelVisible (vox, testVoxel (vox));
			}

		}
	}

	Vector3 returnSideHitPos(RaycastHit hit) {
		Vector3 rayNormal = hit.normal;
		Transform trans = hit.transform;

		rayNormal = trans.TransformDirection (rayNormal);

		Vector3 returnVal = Vector3.zero;

		if (rayNormal == trans.up) {
			returnVal.y = voxelSize.y;
		} else if (rayNormal == -trans.up) {
			returnVal.y = -voxelSize.y;
		} else if (rayNormal == trans.right) {
			returnVal.x = voxelSize.x;
		} else if (rayNormal == -trans.right) {
			returnVal.x = -voxelSize.x;
		} else if (rayNormal == trans.forward) {
			returnVal.z = voxelSize.z;
		} else if (rayNormal == -trans.forward) {
			returnVal.z = -voxelSize.z;
		}

		return returnVal;
	}

	bool testVoxel(voxel gridVoxel) {
		List<voxel> neighbours = findNeighbours(gridVoxel.pos, gridVoxel.parent);

		if (neighbours.Count != 6) {
			return true;
		}

		foreach (voxel vox in neighbours) {
			if (vox.destroyed) {
				return true;
			}
		}

		return false;
	}

	//turn into mesh option
	void voxelVisible(voxel _voxel, bool visible = false) {
		if (_voxel.obj != null) {
			if (visible == false) {
				Destroy (_voxel.obj);
			} else {
				_voxel.obj.SetActive (visible);
			}
		} else {
			if (visible) {
				//_voxel.obj = GameObject.CreatePrimitive (PrimitiveType.Cube);
				_voxel.obj = GameObject.Instantiate(voxelObjects[_voxel.objID], _voxel.pos, voxelObjects[_voxel.objID].transform.rotation);
				_voxel.obj.transform.localScale = voxelSize;
				//_voxel.obj.transform.position = _voxel.pos;
			}
		}
	}

	List<voxel> findNeighbours(Vector3 pos, chunk _chunk = null) {
		List<voxel> returnVoxels = new List<voxel>();
		chunk voxelChunk = _chunk;
		if (voxelChunk == null) {
			voxelChunk = posToChunk (pos);
		}
		vec3I voxelPos = new vec3I(posToVoxel (pos, voxelChunk));

		//test left
		if (voxelPos.x > 0) {
			if (voxelChunk.voxels [voxelPos.x - 1, voxelPos.z, voxelPos.y] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x - 1, voxelPos.z, voxelPos.y]);
			}
		} else {
			if (voxelChunk.left != null) {
				vec3I tempVoxelPos = new vec3I(posToVoxel (pos - new Vector3 (voxelSize.x, 0, 0), voxelChunk.left));
				voxel tempVox = voxelChunk.left.voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
				if (tempVox != null) {
					returnVoxels.Add (tempVox);
				}
			}
		}

		//test right
		if (voxelPos.x < sizeOfChunk.x - 1) {
			if (voxelChunk.voxels [voxelPos.x + 1, voxelPos.z, voxelPos.y] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x + 1, voxelPos.z, voxelPos.y]);
			}
		} else {
			if (voxelChunk.right != null) {
				vec3I tempVoxelPos = new vec3I(posToVoxel (pos + new Vector3 (voxelSize.x, 0, 0), voxelChunk.right));
				voxel tempVox = voxelChunk.right.voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
				if (tempVox != null) {
					returnVoxels.Add (tempVox);
				}
			}
		}

		//test back
		if (voxelPos.z > 0) {
			if (voxelChunk.voxels [voxelPos.x, voxelPos.z - 1, voxelPos.y] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x, voxelPos.z - 1, voxelPos.y]);
			}
		} else {
			if (voxelChunk.back != null) {
				vec3I tempVoxelPos = new vec3I(posToVoxel (pos - new Vector3 (0, 0, voxelSize.z), voxelChunk.back));
				voxel tempVox = voxelChunk.back.voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
				if (tempVox != null) {
					returnVoxels.Add (tempVox);
				}
			}
		}

		//test forward
		if (voxelPos.z < sizeOfChunk.y - 1) {
			if (voxelChunk.voxels [voxelPos.x, voxelPos.z + 1, voxelPos.y] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x, voxelPos.z + 1, voxelPos.y]);
			}
		} else {
			if (voxelChunk.forward != null) {
				vec3I tempVoxelPos = new vec3I(posToVoxel (pos + new Vector3 (0, 0, voxelSize.z), voxelChunk.forward));
				voxel tempVox = voxelChunk.forward.voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
				if (tempVox != null) {
					returnVoxels.Add (tempVox);
				}
			}
		}

		//test down
		if (voxelPos.y > 0) {
			if (voxelChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y - 1] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y - 1]);
			}
		}

		//test up
		if (voxelPos.y < chunkHeight - 1) {
			if (voxelChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y + 1] != null) {
				returnVoxels.Add (voxelChunk.voxels [voxelPos.x, voxelPos.z, voxelPos.y + 1]);
			}
		}

		return returnVoxels;
	}

	void destroyVoxel(voxel _voxel) {
		if (_voxel.destroyable) {
			GameObject.Destroy (_voxel.obj);
			_voxel.destroyed = true;
			_voxel.parent.chunkEdited = true;
			_voxel.placed = false;
			_voxel.parent.chunkState = 1;
			_voxel.objID = 0;

			foreach (voxel vox in findNeighbours(_voxel.pos, _voxel.parent)) {
				if (!vox.destroyed) {
					voxelVisible (vox, true);
				}
			}
		}
	}


}
	
[System.Serializable]
public class voxel {
	public chunk parent;
	public GameObject obj;
	public Vector3 pos;
	public bool placed;
	public bool destroyed;
	public bool destroyable;
	public int objID;

	public voxel () {
		parent = null;
		obj = null;
		pos = Vector3.zero;
		placed = false;
		destroyed = false;
		objID = 0;
		destroyable = true;
	}
}

[System.Serializable]
public class chunk {
	public voxel[,,] voxels;
	public section[] sections;
	public bool[] sectionsEnabled;
	public Vector2 chunkPos;
	public chunk left, right, forward, back;
	public bool chunkEdited;
	public int chunkState;
}

public class section {
	public chunk parent;
	public int sectionNum;
	public voxel[,,] voxels;
}

[System.Serializable]
public struct vec3I {
	public int x, y, z;

	//vec3I defaults
	public vec3I(vec3I input) {
		x = input.x;
		y = input.y;
		z = input.z;
	}

	public vec3I(int _x, int _y, int _z) {
		x = _x;
		y = _y;
		z = _z;
	}

	//float defaults
	public vec3I(Vector3 input) {
		x = (int)input.x;
		y = (int)input.y;
		z = (int)input.z;
	}

	public vec3I(float _x, float _y, float _z) {
		x = (int)_x;
		y = (int)_y;
		z = (int)_z;
	}

	//functions

	public static vec3I zero() {
		return new vec3I(0,0,0);
	}

	public static vec3I one() {
		return new vec3I(1,1,1);
	}

	public Vector3 vec3() {
		return new Vector3 (x, y, z);
	}

	//basic overrides

	public static vec3I operator +(vec3I in1, vec3I in2) {
		return new vec3I (in1.x + in2.x, in1.y + in2.y, in1.z + in2.z);
	}

	public static vec3I operator +(vec3I in1, Vector3 in2) {
		return new vec3I (in1.x + (int)in2.x, in1.y + (int)in2.y, in1.z + (int)in2.z);
	}

	public static Vector3 operator +(Vector3 in1, vec3I in2) {
		return new Vector3 (in1.x + in2.x, in1.y + in2.y, in1.z + in2.z);
	}

	public static vec3I operator -(vec3I in1, vec3I in2) {
		return new vec3I (in1.x - in2.x, in1.y - in2.y, in1.z - in2.z);
	}

	public static vec3I operator -(vec3I in1, Vector3 in2) {
		return new vec3I (in1.x - (int)in2.x, in1.y - (int)in2.y, in1.z - (int)in2.z);
	}

	public static Vector3 operator -(Vector3 in1, vec3I in2) {
		return new Vector3 (in1.x - in2.x, in1.y - in2.y, in1.z - in2.z);
	}

}