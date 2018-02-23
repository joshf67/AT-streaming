using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionCalculations {

	public static chunk posToChunk(VoxelCreation voxGen, Vector3 pos) {
		int chunkIndex = voxGen.checkChunkExists (new Vector2 (Mathf.FloorToInt (pos.x / (voxGen.sizeOfChunk.x * voxGen.voxelSize.x)), Mathf.FloorToInt (pos.z / (voxGen.sizeOfChunk.y * voxGen.voxelSize.z))));
		if (chunkIndex != -1) {
			return voxGen.chunks[chunkIndex];
		}
		return null;
	}

	public static Vector2 posToChunkPos(VoxelCreation voxGen, Vector3 pos) {
		return new Vector2 (Mathf.FloorToInt (pos.x / (voxGen.sizeOfChunk.x * voxGen.voxelSize.x)), Mathf.FloorToInt (pos.z / (voxGen.sizeOfChunk.y * voxGen.voxelSize.z)));
	}

	public static section posToSection(VoxelCreation voxGen, vec3I pos) {
		return posToSection (voxGen, pos.vec3 ());
	}

	public static section posToSection(VoxelCreation voxGen, Vector3 pos) {
		chunk tempChunk = posToChunk (voxGen, pos);
		if (tempChunk != null) {
			return posToSection (voxGen, pos, tempChunk);
		}
		return null;
	}

	public static section posToSection(VoxelCreation voxGen, vec3I pos, chunk _chunk) {
		return posToSection (voxGen, pos.vec3 (), _chunk);
	}

	public static section posToSection(VoxelCreation voxGen, Vector3 pos, chunk _chunk) {
		int sectionIndex = _chunk.checkSectionExists ((int)(pos.y / (voxGen.sectionHeight * voxGen.voxelSize.y)));

		if (sectionIndex != -1) {
			return _chunk.sections[sectionIndex];
		}

		return null;
	}

	public static int posToSectionPos(VoxelCreation voxGen, Vector3 pos, chunk _chunk) {
		return (int)(pos.y / (voxGen.sectionHeight * voxGen.voxelSize.y));
	}

	public static vec3I posToVoxel(VoxelCreation voxGen, vec3I pos, section _section) {
		return posToVoxel(voxGen, new Vector3(pos.x, pos.y, pos.z), _section);
	}

	public static vec3I posToVoxel(VoxelCreation voxGen, Vector3 pos, section _section) {
		Vector3 _pos = pos;

		_pos.x -= _section.parent.chunkPos.x * voxGen.sizeOfChunk.x * voxGen.voxelSize.x;
		_pos.y -= _section.sectionNum * voxGen.sectionHeight * voxGen.voxelSize.y;
		_pos.z -= _section.parent.chunkPos.y * voxGen.sizeOfChunk.y * voxGen.voxelSize.z;

		_pos = Functions.vec3Div (_pos, voxGen.voxelSize);

		//turn pos into int
		//return voxel position within chunk
		return new vec3I (_pos);
	}

}
