using System.IO;
using UnityEngine;
using System.Collections.Generic;
using CusHolder;

public class ChunkLoading
{

	static string directory = Application.dataPath;

    public static void loadWorldData(VoxelCreation voxGen)
    {

        //setup directory
        char split = Path.DirectorySeparatorChar;
		string dir = directory + split + voxGen.directory + split;

        //check if world data exists
        if (File.Exists(dir + "WorldData.txt"))
        {
			while (voxGen.streamerOpen) {
			}
			voxGen.streamerOpen = true;

            StreamReader reader = new StreamReader(dir + "WorldData.txt", true);

            //delete save if world data is different

            if (reader.ReadLine() != voxGen.sizeOfChunk.x.ToString() || reader.ReadLine() != voxGen.sectionHeight.ToString() || reader.ReadLine() != voxGen.sizeOfChunk.y.ToString()
                || reader.ReadLine() != voxGen.chunkSections.ToString() || reader.ReadLine() != voxGen.voxelSize.x.ToString() || reader.ReadLine() != voxGen.voxelSize.y.ToString()
                || reader.ReadLine() != voxGen.voxelSize.z.ToString() || reader.ReadLine() != voxGen.smoothing.ToString())
            {
				reader.Close();
				voxGen.streamerOpen = false;
                Directory.Delete(dir, true);
                return;
            }

            //store random seed for world
            voxGen.randSeed = float.Parse(reader.ReadLine());

			reader.Close();
			voxGen.streamerOpen = false;
        }
    }


    public static section loadSection(VoxelCreation voxGen, int sectionNumber, chunk currentChunk)
    {
        section returnVal = null;
        //setup directory
        char split = Path.DirectorySeparatorChar;
		string dir = directory + split + voxGen.directory + split;

        //check if section file exists
        if (File.Exists(dir + currentChunk.chunkPos.ToString() + split + "section " + sectionNumber.ToString() + ".txt"))
        {
			while (voxGen.streamerOpen) {
			}
			voxGen.streamerOpen = true;

            StreamReader reader = new StreamReader(dir + currentChunk.chunkPos.ToString() + split + "section " + sectionNumber.ToString() + ".txt", true);

            //setup new section
            returnVal = new section(voxGen.sizeOfChunk.x, voxGen.sizeOfChunk.y, voxGen.sectionHeight, currentChunk, sectionNumber);

            //read section file
            string savedData = reader.ReadToEnd();
            string[] lines = savedData.Split(',');

            if (lines.Length == 1)
            {
				reader.Close();
				voxGen.streamerOpen = false;
                return returnVal;
            }

            int loop = -1;
            int currentObjId = -2;
            int currentLine = 0;

            //loop through all section voxels
            for (int a = 0; a < voxGen.sizeOfChunk.x; a++)
            {
                for (int b = 0; b < voxGen.sizeOfChunk.y; b++)
                {
                    for (int c = 0; c < voxGen.sectionHeight; c++)
                    {
                        //check if loop is active
                        if (loop == -1)
                        {
                            //generate voxel based on file input
                            string action = lines[currentLine++];
                            //test if action is loop
                            if (action == "n")
                            {
                                //setup loop data
                                loop = int.Parse(lines[currentLine++]);
                                currentObjId = int.Parse(lines[currentLine++]);
                            }
                            else
                            {
                                //check if action isn't a false input
                                if (action != "")
                                {
                                    //generate voxel based on input
                                    voxGen.generateVoxel(returnVal, new vec3I(a, c, b), int.Parse(action));
                                }
                            }
                        }
                        if (loop != -1)
                        {
                            //generate voxel based on previous input
                            voxGen.generateVoxel(returnVal, new vec3I(a, c, b), currentObjId);
                            loop--;
                        }
                    }
                }
            }

			reader.Close();
			voxGen.streamerOpen = false;

        }

        return returnVal;
    }



    public static List<chunk> loadChunk(VoxelCreation voxGen, Vector2 pos, chunk currentChunk)
    {
        //setup directory
        char split = Path.DirectorySeparatorChar;
		string dir = directory + split + voxGen.directory + split;
        List<chunk> visibilityCheck = new List<chunk>();

        //check if chunk data exists
        if (File.Exists(dir + pos.ToString() + split + "ChunkData.txt"))
        {

			while (voxGen.streamerOpen) {
			}
			voxGen.streamerOpen = true;

            StreamReader reader = new StreamReader(dir + pos.ToString() + split + "ChunkData.txt", true);

            string inputTest = null;

            //setup starting height
            currentChunk.yHeight = new double[voxGen.sizeOfChunk.x, voxGen.sizeOfChunk.y];

            //read starting heights from file
            for (int a = 0; a < voxGen.sizeOfChunk.x; a++)
            {
                for (int b = 0; b < voxGen.sizeOfChunk.y; b++)
                {
                    inputTest = reader.ReadLine();
                    if (inputTest == "")
                    {
                        continue;
                    }
                    if (inputTest == null)
                    {
                        currentChunk.yHeight = null;
                        continue;
                    }
                    currentChunk.yHeight[a, b] = double.Parse(inputTest);
                }
            }

			reader.Close();
			voxGen.streamerOpen = false;

			//create new voxel height if starting height is null
			//wait until chunk has starting heights
			while (currentChunk.yHeight == null) {
				if (!voxGen.checkingChunkYHeight) {
					if (!voxGen.createChunkYHeightTasks.Contains (currentChunk)) {
						voxGen.createChunkYHeightTasks.Add (currentChunk);
					}
				}
			}

        }

        currentChunk.chunkPos = pos;

        //loop through each section and load it from file
        for (int a = 0; a < voxGen.chunkSections; a++)
        {
            section tempSection = loadSection(voxGen, a, currentChunk);
            if (tempSection != null)
            {
                currentChunk.sections.Add(tempSection);
            }
        }

        //setup neighbours and return visbility check
        if (voxGen.checkNeighboursVisibleOnCreation)
        {
            visibilityCheck = voxGen.setupChunkNeighbours(currentChunk);
        }
        else
        {
            voxGen.setupChunkNeighbours(currentChunk);
        }
        visibilityCheck.Add(currentChunk);
        return visibilityCheck;

    }



    public static section forceSectionLoad(VoxelCreation voxGen, int sectionNum, Vector2 chunkPos, bool hidden = true)
    {

        //check chunk exists
        int chunkIndex = voxGen.checkChunkExists(chunkPos);
        chunk tempChunk = null;

        //force load chunk
        if (chunkIndex != -1)
        {
            tempChunk = voxGen.chunks[chunkIndex];
        }
        else
        {
            tempChunk = ChunkLoading.forceChunkLoad(voxGen, chunkPos);
        }

        //return section loaded
        return forceSectionLoad(voxGen, sectionNum, tempChunk, hidden);
    }





    public static section forceSectionLoad(VoxelCreation voxGen, int sectionNum, chunk _chunk, bool hidden = true)
    {

        //force chunk to remain for 3 ticks
        _chunk.immune = 3;

        //load section
        section temp = ChunkLoading.loadSection(voxGen, sectionNum, _chunk);

        //generate new chunk if section fails to load
        if (temp == null)
        {
            voxGen.createChunkTasks.Add(new chunkTask(_chunk, true));
        }

        return temp;
    }




    public static chunk forceChunkLoad(VoxelCreation voxGen, Vector2 chunkPos, bool hidden = true)
    {
        //check if chunk exists
        if (voxGen.checkChunkExists(chunkPos) == -1)
        {

            //setup new chunk
            voxGen.chunks.Add(new chunk());

            //try to load chunk
            List<chunk> newVisibilityCheck = ChunkLoading.loadChunk(voxGen, chunkPos, voxGen.chunks[voxGen.chunks.Count - 1]);

            //check if chunk loaded
            if (newVisibilityCheck.Count == 0)
            {

                //setup new chunk creation task
                voxGen.chunks[voxGen.chunks.Count - 1].chunkPos = chunkPos + voxGen.playerPos;

                voxGen.createChunkTasks.Add(new chunkTask(voxGen.chunks[voxGen.chunks.Count - 1], !hidden));

            }

            //if hidden clear visbility check
            if (hidden)
            {
                newVisibilityCheck.Clear();
            }

            //loop through visiblity check and add chunk to visbility check
            foreach (chunk _chunk in newVisibilityCheck)
            {
                voxGen.toCheckVisibility.Add(_chunk);
            }

            //set chunk immunity to 3 ticks
            voxGen.chunks[voxGen.chunks.Count - 1].immune = 3;

            //return above chunk
            return voxGen.chunks[voxGen.chunks.Count - 1];
        }

        return null;
    }



}
