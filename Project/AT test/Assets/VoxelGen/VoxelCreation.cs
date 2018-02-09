using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class VoxelCreation : MonoBehaviour {

	[Header("Other Variables")]
	public GameObject player;
	public int textureType = 0;

	[Space(20)]
	[Header("StreamData")]
	public string directory;
	public bool saveChunks;

	[Space(20)]
	[Header("Grid Variables")]
	public Vector2 sizeOfChunk;
	public int chunkHeight = 0;
	[SerializeField]
	public List<chunk> chunks = new List<chunk>();
	public Vector2 chunkRange = Vector2.zero;

	[Header("Voxel details")]
	public Vector3 voxelSize;
	public List<Material> voxelMaterial = new List<Material>();

	[Space(20)]
	[Header("Cave")]
	public bool firstPass;
	public bool secondPass;
	public float maxChanceOfCave;
	public float chanceOfCave;
	public float secondCavePass;
	public float improveCaveChance;

	bool firstLoad = true;

	List<chunk> loadChunk(Vector2 pos, chunk currentChunk) {
		string filename = pos.ToString();
		List<chunk> visibilityCheck = new List<chunk> ();

		if (!Directory.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory)) {
			Directory.CreateDirectory (Application.dataPath + Path.DirectorySeparatorChar + directory);
		}

		if (File.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt")) {
			StreamReader reader = new StreamReader (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt", true);

			if (reader.ReadLine () != sizeOfChunk.x.ToString() || reader.ReadLine() != sizeOfChunk.y.ToString()) {
				reader.Close ();
				File.Delete (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt");
				return visibilityCheck;
			}

			int chunkData = int.Parse(reader.ReadLine ());

			currentChunk.chunkPos = pos;

			currentChunk.voxels = new voxel[(int)sizeOfChunk.x, (int)sizeOfChunk.y, (int)chunkHeight];

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
							int texId = int.Parse (reader.ReadLine ());
							generateVoxel (currentChunk, new vec3I (a, c, b), texId);
							break;
						}
					}
				}
			}

			visibilityCheck = setupChunkNeighbours (currentChunk);
			visibilityCheck.Add (currentChunk);

			//visibilityCheck = generateChunkVoxels (currentChunk);

			/*
			bool continueToRead = true;

			do {
				continueToRead = false;
				string action = reader.ReadLine();

				if (action == null) {
					break;
				}

				int x = int.Parse(reader.ReadLine());
				int y = int.Parse(reader.ReadLine());
				int z = int.Parse(reader.ReadLine());

				voxel _voxel = currentChunk.voxels[x,z,y];


				if (action == "p") {
					
					_voxel.texId = int.Parse(reader.ReadLine());
					if (!_voxel.destroyed) {
						if (_voxel.obj != null) {
							_voxel.obj.GetComponent<MeshRenderer> ().material = voxelMaterial[_voxel.texId];
						}
					}

					continueToRead = true;
				} else if (action == "d") {

					destroyVoxel(_voxel);

					continueToRead = true;
				} else {
					continueToRead = false;
				}

			} while (continueToRead);
			*/

			reader.Close ();

			return visibilityCheck;

		}

		return visibilityCheck;
	}

	bool saveChunk(chunk _chunk) {
		string filename = _chunk.chunkPos.ToString();

		if (!Directory.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory)) {
			Directory.CreateDirectory (Application.dataPath + Path.DirectorySeparatorChar + directory);
		}

		if (File.Exists(Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt")) {
			File.Delete (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt");
		}

		StreamWriter writer = new StreamWriter (Application.dataPath + Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar + filename + ".txt");

		writer.WriteLine (sizeOfChunk.x);
		writer.WriteLine (sizeOfChunk.y);
		writer.WriteLine (1);

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {
					if (_chunk.voxels [a, b, c].destroyed) {
						/*
						writer.WriteLine ("d");
						writer.WriteLine (a);
						writer.WriteLine (c);
						writer.WriteLine (b);
						*/
						writer.WriteLine (-1);
					} else {
						/*
						writer.WriteLine ("p");
						writer.WriteLine (a);
						writer.WriteLine (c);
						writer.WriteLine (b);
						*/
						writer.WriteLine (_chunk.voxels [a, b, c].texId);
						/*
						writer.WriteLine (_chunk.voxels [a, b, c].texId);
						if (_chunk.voxels [a, b, c].placed) {
							writer.WriteLine ("p");
							writer.WriteLine (a);
							writer.WriteLine (c);
							writer.WriteLine (b);
							writer.WriteLine (_chunk.voxels [a, b, c].texId);
						}
						*/
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

	void generateVoxel(chunk parent, vec3I pos, int texId) {
		parent.voxels [pos.x, pos.z, pos.y] = new voxel();

		parent.voxels [pos.x, pos.z, pos.y].parent = parent;

		parent.voxels [pos.x, pos.z, pos.y].pos = new Vector3 (pos.x * voxelSize.x, pos.y * voxelSize.y, pos.z * voxelSize.z) + new Vector3(parent.chunkPos.x * sizeOfChunk.x * voxelSize.x, 0, parent.chunkPos.y * sizeOfChunk.y * voxelSize.z);

		if (texId == -2) {
			parent.voxels [pos.x, pos.z, pos.y].destroyed = true;
		} else if (texId == -1) {
			parent.voxels [pos.x, pos.z, pos.y].texId = 0;
		} else {
			parent.voxels [pos.x, pos.z, pos.y].texId = texId;
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

	List<chunk> generateChunkVoxels(chunk _chunk) {

		List<chunk> visibilityCheck = new List<chunk> ();

		_chunk.voxels = new voxel[(int)sizeOfChunk.x, (int)sizeOfChunk.y, (int)chunkHeight];

		for (int a = 0; a < sizeOfChunk.x; a++) {
			for (int b = 0; b < sizeOfChunk.y; b++) {
				for (int c = 0; c < chunkHeight; c++) {

					generateVoxel (_chunk, new vec3I (a, c, b), 0);

					/*

					_chunk.voxels [a, b, c] = new voxel();

					_chunk.voxels [a, b, c].parent = _chunk;

					_chunk.voxels [a, b, c].pos = new Vector3 (a * voxelSize.x, c * voxelSize.y, b * voxelSize.z) + new Vector3(_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x, 0, _chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z);

					_chunk.voxels [a, b, c].texId = 0;

*/

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

	int posToChunk(Vector3 pos) {
		return checkChunkExists (new Vector2 (Mathf.FloorToInt(pos.x / (sizeOfChunk.x * voxelSize.x)), Mathf.FloorToInt(pos.z / (sizeOfChunk.y * voxelSize.z))));
	}

	vec3I posToVoxel(vec3I pos, chunk _chunk) {
		return posToVoxel(new Vector3(pos.x, pos.y, pos.z), _chunk);
	}

	vec3I posToVoxel(Vector3 pos, chunk _chunk) {
		Vector3 _pos = pos;

		_pos.x -= _chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x;
		_pos.y /=  voxelSize.y;
		_pos.z -= _chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z;

		if (voxelSize.x > 1) {
			_pos.x /= voxelSize.x;
		}
		if (voxelSize.z > 1) {
			_pos.z /= voxelSize.z;
		}

		//turn pos into int
		//return voxel position within chunk
		return new vec3I (_pos);
	}

	void Start() {
		player.GetComponent<Rigidbody> ().isKinematic = true;
	}

	void Update() {

		Vector2 playerPos = new Vector2(Mathf.Floor((player.transform.position.x + (voxelSize.x / 2)) / (sizeOfChunk.x * voxelSize.x)),
			Mathf.Floor((player.transform.position.z + (voxelSize.z / 2)) / (sizeOfChunk.y * voxelSize.z)));

		List<chunk> toDelete = new List<chunk>();
		List<chunk> toCheckVisibility = new List<chunk> ();

		//check if chunks are still needed
		foreach (chunk _chunk in chunks) {
			if (!checkWithinDistance (_chunk, playerPos)) {
				//if (_chunk.chunkEdited) {
					saveChunk (_chunk);
				//}
				deleteChunkVoxels (_chunk);
				List<chunk> tempChunks = resetChunkNeighbours (_chunk);
				foreach (chunk __chunk in tempChunks) {
					if (!toCheckVisibility.Contains(__chunk)) {
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
				if (!toCheckVisibility.Contains(_chunk)) {
					toCheckVisibility.Add (_chunk);
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
			if (textureType > 0) {
				textureType--;
			}
		}

		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			if (textureType < voxelMaterial.Count) {
				textureType++;
			}
		}

		//destroy/place
		if (Input.GetMouseButtonDown (0)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position;
				int chunkPos = posToChunk (hitPos);
				if (chunkPos != -1) {
					vec3I voxelPos = posToVoxel (hitPos, chunks[chunkPos]);
					destroyVoxel(chunks[chunkPos].voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
				}
			}
		}

		if (Input.GetMouseButtonDown (1)) {
			RaycastHit hit;
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit)) {
				Vector3 hitPos = hit.collider.transform.position;
				int chunkPos = posToChunk (hitPos);
				if (chunkPos != -1) {
					vec3I voxelPos = posToVoxel (hitPos, chunks[chunkPos]) + returnSideHitPos(hit);
					placeObject (voxelPos, chunks [chunkPos]);
				}
			}
		}

		if (firstLoad) {
			firstLoad = false;
			player.GetComponent<Rigidbody> ().isKinematic = false;
			player.transform.position = new Vector3 (0, (chunkHeight * voxelSize.y) + 2, 0);
		}

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

		if (!testVoxelPosInBounds(voxelToChange)) {
			return;
		}

		voxel vox = _chunk.voxels[voxelToChange.x, voxelToChange.z, voxelToChange.y];

		if (vox.destroyed) {
			vox.destroyed = false;
			vox.placed = true;
			vox.texId = textureType;
			voxelVisible (vox, testVoxel (vox));
		}
	}

	vec3I returnSideHitPos(RaycastHit hit) {
		Vector3 rayNormal = hit.normal;
		Transform trans = hit.transform;

		rayNormal = trans.TransformDirection (rayNormal);

		vec3I returnVal = vec3I.zero();

		if (rayNormal == trans.up) {
			returnVal.y = 1;
		} else if (rayNormal == -trans.up) {
			returnVal.y = -1;
		} else if (rayNormal == trans.right) {
			returnVal.x = 1;
		} else if (rayNormal == -trans.right) {
			returnVal.x = -1;
		} else if (rayNormal == trans.forward) {
			returnVal.z = 1;
		} else if (rayNormal == -trans.forward) {
			returnVal.z = -1;
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
				_voxel.obj = GameObject.CreatePrimitive (PrimitiveType.Cube);
				_voxel.obj.transform.position = _voxel.pos;
				_voxel.obj.transform.localScale = voxelSize;
				_voxel.obj.GetComponent<MeshRenderer> ().material = voxelMaterial[_voxel.texId];
			}
		}
	}

	List<voxel> findNeighbours(Vector3 pos, chunk _chunk = null) {
		List<voxel> returnVoxels = new List<voxel>();
		chunk voxelChunk = _chunk;
		if (voxelChunk == null) {
			voxelChunk = chunks [posToChunk (pos)];
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
		GameObject.Destroy(_voxel.obj);
		_voxel.destroyed = true;
		_voxel.parent.chunkEdited = true;
		_voxel.placed = false;

		foreach (voxel vox in findNeighbours(_voxel.pos, _voxel.parent)) {
			if (!vox.destroyed) {
				voxelVisible (vox, true);
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
	public int texId;

	public voxel () {
		parent = null;
		obj = null;
		pos = Vector3.zero;
		placed = false;
		destroyed = false;
		texId = 0;
	}
}

[System.Serializable]
public class chunk {
	public voxel[,,] voxels;
	public Vector2 chunkPos;
	public chunk left, right, forward, back;
	public bool chunkEdited;
	public int chunkState;
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