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
	Vector3 prevPos = Vector3.zero;

	[Space(20)]
	[Header("StreamData")]
	public string directory;
	public bool saveChunks;
	public bool worldDataSaved = false;

	[Space(20)]
	[Header("Grid Variables")]
	public vec2I sizeOfChunk;
	public int chunkSections = 0;
	public int sectionHeight = 0;
	[SerializeField]
	public List<chunk> chunks = new List<chunk>();
	public Vector2 chunkRange = Vector2.zero;
	public Vector2 sectionRange = Vector2.zero;
	public bool enableDynamicChunkLoading = true;
	public bool debugMode = false;
	public int smoothing = 0;
	public float perlinDist;
	public float randSeed;
	public float noise1Effect = 0;
	public float noise2Effect = 0;
	public int heightMapRes = 0;

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
		foreach (section sec in _chunk.sections) {
			deleteSectionVoxels (sec);
		}
	}

	void deleteSectionVoxels(section _section) {
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < sectionHeight; c++) {
					if (_section.voxels [a, b, c] != null) {
						if (_section.voxels [a, b, c].obj != null) {
							Destroy (_section.voxels [a, b, c].obj);
						}
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

	public void generateVoxel(section parent, vec3I pos, int objID) {
		parent.voxels [pos.x, pos.z, pos.y] = new voxel();

		parent.voxels [pos.x, pos.z, pos.y].parent = parent;

		//need to change y calculation to section instead of global
		parent.voxels [pos.x, pos.z, pos.y].pos = new Vector3 (pos.x * voxelSize.x, pos.y * voxelSize.y, pos.z * voxelSize.z)
			+ new Vector3(parent.parent.chunkPos.x * sizeOfChunk.x * voxelSize.x, sectionHeight * parent.sectionNum * voxelSize.y, parent.parent.chunkPos.y * sizeOfChunk.y * voxelSize.z);

		if (objID == 0) {
			parent.voxels [pos.x, pos.z, pos.y].destroyed = true;
			parent.voxels [pos.x, pos.z, pos.y].objID = 0;
		} else {
			parent.voxels [pos.x, pos.z, pos.y].objID = objID;
		}

		if (parent.voxels [pos.x, pos.z, pos.y].pos.y == 0) {
			parent.voxels [pos.x, pos.z, pos.y].destroyed = false;
			parent.voxels [pos.x, pos.z, pos.y].destroyable = false;
			parent.voxels [pos.x, pos.z, pos.y].objID = 1;
		}
	}

	public List<chunk> setupChunkNeighbours(chunk _chunk) {

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

	//add check for neighbours
	void generateSectionVoxels(section _section) {

		_section.voxels = new voxel[sizeOfChunk.x, sizeOfChunk.y, sectionHeight];

		if (_section.parent.yHeight == null) {
			generateYHeight (_section.parent);
		}

		int yHeight = 0;
		
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < sectionHeight; c++) {
					yHeight = _section.sectionNum * sectionHeight;

					if (yHeight + c > _section.parent.yHeight [a, b]) {
						generateVoxel (_section, new vec3I (a, c, b), 0);
					} else {
						if (yHeight + c > _section.parent.yHeight [a, b] - 2) {
							generateVoxel (_section, new vec3I (a, c, b), 2);
						} else {
							generateVoxel (_section, new vec3I (a, c, b), 1);
						}
					}

				}
			}
		}

	}

	public double[,] generateYHeight(chunk _chunk) {

		double[,] yHeight = new double[sizeOfChunk.x, sizeOfChunk.y];

		//generate random height
		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				float noise1 = Mathf.PerlinNoise(((_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x) + a) * perlinDist * randSeed, ((_chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z) + b) * perlinDist * randSeed);
				float noise2 = Mathf.PerlinNoise (UnityEngine.Random.Range (1, int.MaxValue) / UnityEngine.Random.Range (1, int.MaxValue), UnityEngine.Random.Range (1, int.MaxValue) / UnityEngine.Random.Range (1, int.MaxValue));
				yHeight [a, b] = ((noise1 * noise1Effect) - (noise2 * noise2Effect)) * (sectionHeight * chunkSections);
			}
		}

		_chunk.yHeight = yHeight;

		smoothChunk (_chunk);

		return yHeight;

	}

	void smoothChunk(chunk _chunk) {

		//smooth times
		for (int sm = 0; sm < smoothing; sm++) {

			//smooth random height
			for (int a = 0; a < sizeOfChunk.x; a++) {
				for (int b = 0; b < sizeOfChunk.y; b++) {
					double height = _chunk.yHeight [a, b];
					int sides = 1;

					if (a > 0) {
						height += _chunk.yHeight [a - 1, b];
						sides++;

						if (b < sizeOfChunk.y - 1) {
							height += _chunk.yHeight [a - 1, b + 1];
							sides++;
						}

						if (b > 0) {
							height += _chunk.yHeight [a - 1, b - 1];
							sides++;
						}

					} else {
						if (_chunk.left != null) {
							height += _chunk.left.yHeight[sizeOfChunk.x - 1, b];
							sides++;
						}
					}

					if (a < sizeOfChunk.x - 1) {
						height += _chunk.yHeight [a + 1, b];
						sides++;

						if (b < sizeOfChunk.y - 1) {
							height += _chunk.yHeight [a + 1, b + 1];
							sides++;
						}

						if (b > 0) {
							height += _chunk.yHeight [a + 1, b - 1];
							sides++;
						}
					} else {
						if (_chunk.left != null) {
							height += _chunk.left.yHeight[0, b];
							sides++;
						}
					}

					if (b > 0) {
						height += _chunk.yHeight [a, b - 1];;
						sides++;
					} else {
						if (_chunk.forward != null) {
							height += _chunk.forward.yHeight[a, 0];
							sides++;
						}
					}

					if (b < sizeOfChunk.y - 1) {
						height += _chunk.yHeight [a, b + 1];;
						sides++;
					} else {
						if (_chunk.back != null) {
							height += _chunk.back.yHeight[a, sizeOfChunk.y - 1];
							sides++;
						}
					}

					_chunk.yHeight [a, b] = height / sides;
				}
			}
		}
	}

	public void chunkVisibleCheck(chunk _chunk) {

		if (_chunk == null) {
			return;
		}

		foreach (section sec in _chunk.sections) {
			sectionVisibileCheck (sec);
		}

	}

	void sectionVisibileCheck(section _section) {

		double heightDiff = 0;

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < sectionHeight; c++) {
					if (_section.voxels [a, b, c] != null) {
						if (!_section.voxels [a, b, c].destroyed) {
							if (_section.voxels [a, b, c].objID != 0) {

								/*

								if (_section.voxels [a, b, c].pos.y >= _section.parent.yHeight [a, b] - (voxelSize.y * 2)) {
									voxelVisible (_section.voxels [a, b, c], true);
									continue;
								} else {

									if (a > 0) {
										if (_section.voxels [a, b, c].pos.y >= _section.parent.yHeight [a - 1, b] - (voxelSize.y * 2)) {
											voxelVisible (_section.voxels [a, b, c], true);
											continue;
										}

									} else {
										if (_section.parent.left != null) {
											if (_section.voxels [a, b, c].pos.y >= _section.parent.left.yHeight [sizeOfChunk.x - 1, b] - (voxelSize.y * 2)) {
												voxelVisible (_section.voxels [a, b, c], true);
												continue;
											}
										}
									}

									if (a < sizeOfChunk.x - 1) {
										if (_section.voxels [a, b, c].pos.y >= _section.parent.yHeight [a + 1, b] - (voxelSize.y * 2)) {
											voxelVisible (_section.voxels [a, b, c], true);
											continue;
										}

									} else {
										if (_section.parent.right != null) {
											if (_section.voxels [a, b, c].pos.y >= _section.parent.right.yHeight [0, b] - (voxelSize.y * 2)) {
												voxelVisible (_section.voxels [a, b, c], true);
												continue;
											}
										}
									}


									if (b > 0) {
										if (_section.voxels [a, b, c].pos.y >= _section.parent.yHeight [a, b - 1] - (voxelSize.y * 2)) {
											voxelVisible (_section.voxels [a, b, c], true);
											continue;
										}
									}  else {
										if (_section.parent.forward != null) {
											if (_section.voxels [a, b, c].pos.y >= _section.parent.forward.yHeight [a, 0] - (voxelSize.y * 2)) {
												voxelVisible (_section.voxels [a, b, c], true);
												continue;
											}
										}
									}

									if (b < sizeOfChunk.y - 1) {
										if (_section.voxels [a, b, c].pos.y >= _section.parent.yHeight [a, b + 1] - (voxelSize.y * 2)) {
											voxelVisible (_section.voxels [a, b, c], true);
											continue;
										}
									} else {
										if (_section.parent.back != null) {
											if (_section.voxels [a, b, c].pos.y >= _section.parent.back.yHeight [a, sizeOfChunk.y - 1] - (voxelSize.y * 2)) {
												voxelVisible (_section.voxels [a, b, c], true);
												continue;
											}
										}
									}
								}
								*/

								/*
								 
								if (Mathf.Abs((float)(_section.voxels [a, b, c].pos.y - _section.parent.yHeight [a, b])) <= voxelSize.y) {
									voxelVisible (_section.voxels [a, b, c], true);
								}

								*/



								if (testVoxel (_section.voxels [a, b, c])) {
									if (_section.voxels [a, b, c].obj == null) {
										voxelVisible (_section.voxels [a, b, c], true);
									}
								} else if (_section.voxels [a, b, c].obj != null) {
									voxelVisible (_section.voxels [a, b, c], false);
								}


							}
						} else if (_section.voxels [a, b, c].obj != null) {
							voxelVisible (_section.voxels [a, b, c], false);
						}
					}
				}
			}
		}
	}

	/*
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
	*/

	public int checkChunkExists(Vector2 input) {
		for(int a = 0; a < chunks.Count; a++) {
			if (input == chunks[a].chunkPos) {
				return a;
			}
		}
		return -1;
	}

	void Start() {
		player.GetComponent<Rigidbody> ().isKinematic = true;

		randSeed = UnityEngine.Random.Range (1, int.MaxValue);
		randSeed /= int.MaxValue;

		ChunkLoading.loadWorldData (this);

		float size = sizeOfChunk.y;

		if (sizeOfChunk.x < sizeOfChunk.y) {
			size = sizeOfChunk.x;
		}

		//size = Mathf.CeilToInt (size / 2) + 1;
		int power = 0;
		float powerSize = 0;

		while (powerSize > 2) {
			power += 1;
			powerSize /= 2;
		}

		int heightMapRes = (int)Mathf.Pow(2, power) + 1;

		while (heightMapRes < size) {
			power += 1;
			heightMapRes = (int)Mathf.Pow(2, power) + 1;
		}

		char split = Path.DirectorySeparatorChar;
		string dir = Application.dataPath + split + directory + split;

		if (!Directory.Exists(dir)) {
			Directory.CreateDirectory (dir);
		}

		if (!File.Exists (dir + "TestData.txt")) {
			
			StreamWriter tempWriter = new StreamWriter (dir + "TestData.txt");
			for (int a = 0; a < 100000; a++) {
				tempWriter.WriteLine (UnityEngine.Random.Range (0, 10));
			}
			tempWriter.Close ();
			//File.Delete (dir + "TestData.txt");
		}

		StreamReader tempRead = new StreamReader (dir + "TestData.txt");

		StreamWriter tempWrite = new StreamWriter (dir + "TestDataResults.txt");

		string line = null;
		int lineNum = 0;
		int aimLine = 40000;

		while ((line = tempRead.ReadLine ()) != null) {
			if (lineNum == aimLine) {
				tempWrite.WriteLine (UnityEngine.Random.Range (0, 10));
			} else {
				tempWrite.WriteLine (line);
			}
			lineNum++;
		}

		tempRead.Close ();
		tempWrite.Close ();

		File.Delete (dir + "TestData.txt");
		File.Copy ((dir + "TestDataResults.txt"), (dir + "TestData.txt"));
		File.Delete (dir + "TestDataResults.txt");


	}

	public List<chunk> createChunk (chunk _chunk) {

		List<chunk> returnVal = new List<chunk> ();

		for (int a = 0; a < chunkSections; a++) {
			_chunk.sections.Add (new section (sizeOfChunk.x, sizeOfChunk.y, sectionHeight, _chunk, a));
			generateSectionVoxels (_chunk.sections [a]);
		}

		double minimumHeight = sectionHeight * chunkSections;

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				if (_chunk.yHeight [a, b] < minimumHeight) {
					minimumHeight = _chunk.yHeight [a, b];
				}
			}
		}

		List<int> toRemove = new List<int> ();

		/*
		for (int a = 0; a < chunkSections; a++) {
			if ((a * (sectionHeight * voxelSize.y)) + sectionHeight < minimumHeight) {
				saveSection (_chunk.sections [a]);
				toRemove.Add (a);
			}
		}

		foreach (int i in toRemove) {
			deleteSectionVoxels (_chunk.sections[_chunk.checkSectionExists (i)]);
			_chunk.sections.RemoveAt (_chunk.checkSectionExists (i));
		}
		*/

		returnVal = setupChunkNeighbours (_chunk);
		returnVal.Add (_chunk);
		return returnVal;
	}

	void Update() {

		playerPos = new Vector2(Mathf.Floor((player.transform.position.x + (voxelSize.x / 2)) / (sizeOfChunk.x * voxelSize.x)),
			Mathf.Floor((player.transform.position.z + (voxelSize.z / 2)) / (sizeOfChunk.y * voxelSize.z)));

		List<chunk> toDelete = new List<chunk>();
		List<chunk> toCheckVisibility = new List<chunk> ();

		if (enableDynamicChunkLoading) {

			if (player.transform.position != prevPos) {
				//check if chunks are still needed
				foreach (chunk _chunk in chunks) {
					if (!checkWithinDistance (_chunk, playerPos)) {
						if (_chunk.immune <= 0) {
							ChunkSaving.saveChunk (this, _chunk);
							deleteChunkVoxels (_chunk);
							List<chunk> tempChunks = resetChunkNeighbours (_chunk);
							foreach (chunk __chunk in tempChunks) {
								if (!toCheckVisibility.Contains (__chunk)) {
									toCheckVisibility.Add (__chunk);
								}
							}
							toDelete.Add (_chunk);
						} else {
							_chunk.immune--;
						}
					}
				}

				//remove all chunks not needed
				foreach (chunk _chunk in toDelete) {
					toCheckVisibility.Remove (_chunk);
					ChunkSaving.saveChunk (this, _chunk);
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

					List<chunk> newVisibilityCheck = ChunkLoading.loadChunk (this, chunkPos + playerPos, chunks [chunks.Count - 1]);

					if ((newVisibilityCheck.Count == 0 && chunkPos != Vector2.zero) || chunks [chunks.Count - 1].sections.Count == 0) {

						chunks [chunks.Count - 1].chunkPos = chunkPos + playerPos;

						newVisibilityCheck = createChunk (chunks [chunks.Count - 1]);

					}

					foreach (chunk _chunk in newVisibilityCheck) {
						if (!toCheckVisibility.Contains (_chunk)) {
							toCheckVisibility.Add (_chunk);
						}
					}
				}

			}

		}

		prevPos = player.transform.position;

		//check visibility on chunks needed
		foreach (chunk _chunk in toCheckVisibility) {
			chunkVisibleCheck (_chunk);
		}


		if (firstPass) {
			foreach (chunk _chunk in chunks) {
				//firstCavePass (_chunk);
			}
			firstPass = false;
		}
		if (secondPass) {
			foreach (chunk _chunk in chunks) {
				//cavePass (_chunk);
			}
			secondPass = false;
		}
		if (saveChunks) {
			foreach (chunk _chunk in chunks) {
				ChunkSaving.saveChunk (this, _chunk);
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
				chunk tempChunk = PositionCalculations.posToChunk (this, hitPos);
				if (tempChunk != null) {
					section tempSection = PositionCalculations.posToSection (this, hitPos, tempChunk);
					if (tempSection != null) {
						vec3I voxelPos = PositionCalculations.posToVoxel (this, hitPos, tempSection);
						debugVoxel(tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
					}
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
				chunk tempChunk = PositionCalculations.posToChunk (this, hitPos);
				if (tempChunk == null) {
					tempChunk = ChunkLoading.forceChunkLoad (this, PositionCalculations.posToChunkPos (this, hitPos));
				}
				if (tempChunk != null) {
					section tempSection = PositionCalculations.posToSection (this, hitPos, tempChunk);
					if (tempSection == null) {
						tempSection = ChunkLoading.forceSectionLoad (this,PositionCalculations.posToSectionPos (this, hitPos, tempChunk), tempChunk);
					}
					if (tempSection != null) {
						vec3I voxelPos = PositionCalculations.posToVoxel (this, hitPos, tempSection);
						destroyVoxel (tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y]);
					}
				}
			}
		}

		if (Input.GetMouseButtonDown (1)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position + returnSideHitPos(hit);
				chunk tempChunk = PositionCalculations.posToChunk (this, hitPos);
				if (tempChunk == null) {
					tempChunk = ChunkLoading.forceChunkLoad (this, PositionCalculations.posToChunkPos (this, hitPos));
				}
				if (tempChunk != null) {
					section tempSection = PositionCalculations.posToSection (this, hitPos, tempChunk);
					if (tempSection == null) {
						tempSection = ChunkLoading.forceSectionLoad (this, PositionCalculations.posToSectionPos (this, hitPos, tempChunk), tempChunk);
					}
					if (tempSection != null) {
						vec3I voxelPos = PositionCalculations.posToVoxel (this, hitPos, tempSection);
						placeObject (voxelPos, tempSection);
					}
				}
			}
		}

		if (firstLoad) {
			firstLoad = false;
			player.GetComponent<Rigidbody> ().isKinematic = false;
			player.transform.position = new Vector3 (0, (sectionHeight * chunkSections * voxelSize.y) + 2, 0);
		}

	}

	public void explode(Vector3 position, float explosionDist) {
		for (float a = -explosionDist; a < explosionDist; a++) {
			for (float b = -explosionDist; b < explosionDist; b++) {
				for (float c = -explosionDist; c < explosionDist; c++) {
					if (Vector3.Distance (position + Functions.vec3Times (new Vector3 (a, b, c), voxelSize), position) < explosionDist) {
						Vector3 hitPos = position + Functions.vec3Times (new Vector3 (a, b, c), voxelSize);
						chunk tempChunk = PositionCalculations.posToChunk (this, hitPos);
						if (tempChunk == null) {
							tempChunk = ChunkLoading.forceChunkLoad (this, PositionCalculations.posToChunkPos (this, hitPos));
						}
						if (tempChunk != null) {
							section tempSection = PositionCalculations.posToSection (this, hitPos, tempChunk);
							if (tempSection == null) {
								tempSection = ChunkLoading.forceSectionLoad (this, PositionCalculations.posToSectionPos (this, hitPos, tempChunk), tempChunk);
							}
							if (tempSection != null) {
								vec3I voxelPos = PositionCalculations.posToVoxel (this, hitPos, tempSection);
								if (testVoxelPosInBounds (voxelPos)) {
									if (position + Functions.vec3Times (new Vector3 (a, b, c), voxelSize) != position) {
										if (tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y] != null) {
											if (tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y].obj != null) {
												if (tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y].obj.name == "TNT") {
													tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y].obj.GetComponent<TNT> ().trigger ();
												}
											}
										}
									}
									destroyVoxel (tempSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y]);
								}
							}
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

		debugOutput += "\n\nSection Data:\n";
		debugOutput += "Section :" + _voxel.parent.sectionNum;

		debugOutput += "\n\nChunk Data:";
		debugOutput += "\nChunk Position: " + _voxel.parent.parent.chunkPos;

		GetComponent<LineRenderer> ().SetPosition (0, player.transform.position);
		GetComponent<LineRenderer> ().SetPosition (1, _voxel.pos);
		GameObject.FindObjectOfType<Text> ().text = debugOutput;
	}

	bool testVoxelPosInBounds(vec3I input) {
		if (input.x < 0 || input.x > sizeOfChunk.x - 1 ||
			input.y < 0 || input.y > (sectionHeight * chunkSections) + sectionHeight - 1 ||
			input.z < 0 || input.z > sizeOfChunk.y - 1) {
			return false;
		}
		return true;
	}

	void placeObject(vec3I voxelToChange, section _section) {
		voxel vox = _section.voxels[voxelToChange.x, voxelToChange.z, voxelToChange.y];

		if (objectType != 0) {

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

	//find Neighbours for sections
	public List<voxel> findNeighbours(Vector3 pos, section _section = null) {
		List<voxel> returnVoxels = new List<voxel>();
		section voxelSection = _section;
		if (voxelSection == null) {
			voxelSection = PositionCalculations.posToSection (this, pos, _section.parent);
			if (voxelSection == null) {
				return null;
			}
		}
		vec3I voxelPos = PositionCalculations.posToVoxel (this, pos, voxelSection);

		//test left
		if (voxelPos.x > 0) {
			if (_section.voxels [voxelPos.x - 1, voxelPos.z, voxelPos.y] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x - 1, voxelPos.z, voxelPos.y]);
			}
		} else {
			if (voxelSection.parent.left != null) {
				int otherVoxelSection = _section.parent.left.checkSectionExists (_section.sectionNum);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (sizeOfChunk.x - 1, voxelPos.y, voxelPos.z);

					voxel tempVox = voxelSection.parent.left.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		//test right
		if (voxelPos.x < sizeOfChunk.x - 1) {
			if (voxelSection.voxels [voxelPos.x + 1, voxelPos.z, voxelPos.y] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x + 1, voxelPos.z, voxelPos.y]);
			}
		} else {
			if (voxelSection.parent.right != null) {
				int otherVoxelSection = _section.parent.right.checkSectionExists (_section.sectionNum);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (0, voxelPos.y, voxelPos.z);

					voxel tempVox = voxelSection.parent.right.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		//test back
		if (voxelPos.z > 0) {
			if (voxelSection.voxels [voxelPos.x, voxelPos.z - 1, voxelPos.y] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x, voxelPos.z - 1, voxelPos.y]);
			}
		} else {
			if (voxelSection.parent.back != null) {
				int otherVoxelSection = _section.parent.back.checkSectionExists (_section.sectionNum);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (voxelPos.x, voxelPos.y, sizeOfChunk.y - 1);

					voxel tempVox = voxelSection.parent.back.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		//test forward
		if (voxelPos.z < sizeOfChunk.y - 1) {
			if (voxelSection.voxels [voxelPos.x, voxelPos.z + 1, voxelPos.y] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x, voxelPos.z + 1, voxelPos.y]);
			}
		} else {
			if (voxelSection.parent.forward != null) {
				int otherVoxelSection = _section.parent.forward.checkSectionExists (_section.sectionNum);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (voxelPos.x, voxelPos.y, 0);

					voxel tempVox = voxelSection.parent.forward.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		//test down
		if (voxelPos.y > 0) {
			if (voxelSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y - 1] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y - 1]);
			}
		} else {
			if (voxelSection.sectionNum != 0) {
				int otherVoxelSection = _section.parent.checkSectionExists (_section.sectionNum - 1);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (voxelPos.x, sectionHeight - 1, voxelPos.z);

					voxel tempVox = voxelSection.parent.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		//test up
		if (voxelPos.y < sectionHeight - 1) {
			if (voxelSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y + 1] != null) {
				returnVoxels.Add (voxelSection.voxels [voxelPos.x, voxelPos.z, voxelPos.y + 1]);
			}
		} else {
			if (voxelSection.sectionNum != chunkSections - 1) {
				int otherVoxelSection = _section.parent.checkSectionExists (_section.sectionNum + 1);
				if (otherVoxelSection != -1) {
					vec3I tempVoxelPos = new vec3I (voxelPos.x, 0, voxelPos.z);

					voxel tempVox = voxelSection.parent.sections[otherVoxelSection].voxels [tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
					if (tempVox != null) {
						returnVoxels.Add (tempVox);
					}
				}
			}
		}

		return returnVoxels;
	}

	void destroyVoxel(voxel _voxel) {
		if (_voxel.destroyable) {
			GameObject.Destroy (_voxel.obj);
			_voxel.destroyed = true;
			_voxel.placed = false;
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
	public section parent;
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
	public double[,] yHeight;
	public List<int> requiredSections = new List<int>();
	public List<section> sections = new List<section>();
	public Vector2 chunkPos;
	public chunk left, right, forward, back;
	public int immune = 0;

	public int checkSectionExists(int sectionNumber) {
		for (int a = 0; a < sections.Count; a++) {
			if (sections [a].sectionNum == sectionNumber) {
				return a;
			}
		}
		return -1;
	}
}

public class section {
	public chunk parent;
	public int sectionNum;
	public voxel[,,] voxels;

	public section(int width, int length, int height, chunk _parent, int _sectionNum) {
		voxels = new voxel[width, length, height];
		sectionNum = _sectionNum;
		parent = _parent;
	}
}

[Serializable]
public class vec2I {
	public int x, y;

	//vec3I defaults
	public vec2I() {
		x = 0;
		y = 0;
	}

	public vec2I(vec2I input) {
		x = input.x;
		y = input.y;
	}

	public vec2I(int _x, int _y) {
		x = _x;
		y = _y;
	}

	//float defaults
	public vec2I(Vector2 input) {
		x = (int)input.x;
		y = (int)input.y;
	}

	public vec2I(float _x, float _y) {
		x = (int)_x;
		y = (int)_y;
	}

	public vec2I(float _x) {
		y = 0;
	}

	public vec2I(int _x) {
		y = 0;
	}

	//functions

	public static vec2I zero() {
		return new vec2I(0,0);
	}

	public static vec2I one() {
		return new vec2I(1,1);
	}

	public Vector3 vec2() {
		return new Vector2 (x, y);
	}

	//basic overrides

	public static vec2I operator +(vec2I in1, vec2I in2) {
		return new vec2I (in1.x + in2.x, in1.y + in2.y);
	}

	public static vec2I operator +(vec2I in1, Vector2 in2) {
		return new vec2I (in1.x + (int)in2.x, in1.y + (int)in2.y);
	}

	public static Vector2 operator +(Vector2 in1, vec2I in2) {
		return new Vector2 (in1.x + in2.x, in1.y + in2.y);
	}

	public static vec2I operator -(vec2I in1, vec2I in2) {
		return new vec2I (in1.x - in2.x, in1.y - in2.y);
	}

	public static vec2I operator -(vec2I in1, Vector2 in2) {
		return new vec2I (in1.x - (int)in2.x, in1.y - (int)in2.y);
	}

	public static Vector2 operator -(Vector2 in1, vec2I in2) {
		return new Vector2 (in1.x - in2.x, in1.y - in2.y);
	}

}

[System.Serializable]
public class vec3I : vec2I {
	public int z;

	//vec3I defaults
	public vec3I() : base () {
		z = 0;
	}

	public vec3I(vec3I input) : base (input.x, input.y) {
		z = input.z;
	}

	public vec3I(int _x, int _y, int _z) : base (_x, _y) {
		z = _z;
	}

	//float defaults
	public vec3I(Vector3 input) : base (input.x, input.y) {
		z = (int)input.z;
	}

	public vec3I(float _x, float _y, float _z) : base (_x, _y) {
		z = (int)_z;
	}

	public vec3I(float _x, float _y) : base (_x, _y) {
		z = 0;
	}

	public vec3I(int _x, int _y) : base (_x, _y) {
		z = 0;
	}

	public vec3I(float _x) : base (_x) {
		y = 0;
		z = 0;
	}

	public vec3I(int _x) : base (_x) {
		y = 0;
		z = 0;
	}

	//functions

	new public static vec3I zero() {
		return new vec3I(0,0,0);
	}

	new public static vec3I one() {
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