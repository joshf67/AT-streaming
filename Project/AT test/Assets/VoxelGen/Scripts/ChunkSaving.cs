using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class ChunkSaving
{

    public static void saveWorldData(VoxelCreation voxGen)
    {
        //setup directory
        string dir = Application.dataPath + Path.DirectorySeparatorChar + voxGen.directory + Path.DirectorySeparatorChar;

        //check if directory exists
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        //check if world data exists
        if (File.Exists(dir + "WorldData.txt"))
        {
            File.Delete(dir + "WorldData.txt");
        }

		while (voxGen.streamerOpen) {
		}
		voxGen.streamerOpen = true;

        //write all of the world data to file
        StreamWriter writer = new StreamWriter(dir + "WorldData.txt");

        writer.WriteLine(voxGen.sizeOfChunk.x);
        writer.WriteLine(voxGen.sectionHeight);
        writer.WriteLine(voxGen.sizeOfChunk.y);
        writer.WriteLine(voxGen.chunkSections);
        writer.WriteLine(voxGen.voxelSize.x);
        writer.WriteLine(voxGen.voxelSize.y);
        writer.WriteLine(voxGen.voxelSize.z);
        writer.WriteLine(voxGen.smoothing);
        writer.WriteLine(voxGen.randSeed);

		writer.Close();
		voxGen.streamerOpen = false;

    }

    public static void saveSection(VoxelCreation voxGen, section _section)
    {
        //setup directory
        string dir = Application.dataPath + Path.DirectorySeparatorChar + voxGen.directory + Path.DirectorySeparatorChar;
        char split = Path.DirectorySeparatorChar;

        //check if the world data has been saved
        if (!voxGen.worldDataSaved)
        {
            saveWorldData(voxGen);
            voxGen.worldDataSaved = true;
        }

        //check chunk file exists
        checkDirectoryExists(dir + split + _section.parent.chunkPos.ToString());

        //check section file exists
        if (File.Exists(dir + _section.parent.chunkPos.ToString() + split + "section " + _section.sectionNum.ToString() + ".txt"))
        {
            File.Delete(dir + _section.parent.chunkPos.ToString() + split + "section " + _section.sectionNum.ToString() + ".txt");
        }

		while (voxGen.streamerOpen) {
		}
		voxGen.streamerOpen = true;

        StreamWriter writer = new StreamWriter(dir + _section.parent.chunkPos.ToString() + split + "section " + _section.sectionNum.ToString() + ".txt");

        //check if section is empty
        if (_section.voxels[0, 0, 0] == null)
        {
			writer.Close();
			voxGen.streamerOpen = false;
            return;
        }

        //setup basic data
        int loop = -1;
        int prevObjID = _section.voxels[0, 0, 0].objID;
        bool updateFile = false;
        bool lastVoxel = false;

        //loop through every voxel in section
        for (int a = 0; a < voxGen.sizeOfChunk.x; a++)
        {
            for (int b = 0; b < voxGen.sizeOfChunk.y; b++)
            {
                for (int c = 0; c < voxGen.sectionHeight; c++)
                {

                    //check if current voxel is the last voxel in the section
                    if ((a == voxGen.sizeOfChunk.x - 1 && b == voxGen.sizeOfChunk.y - 1 && c == voxGen.sectionHeight - 1))
                    {
                        updateFile = true;
                        lastVoxel = true;
                    }
                    else
                    {
                        //no idea
                        if (c == 0)
                        {
                            if (_section.sectionNum == 0)
                            {
                                loop++;
                                continue;
                            }
                        }
                    }

                    //check if current voxel is same as previous voxel
                    if (_section.voxels[a, b, c].objID != prevObjID)
                    {
                        updateFile = true;
                    }
                    else
                    {
                        loop++;
                    }

                    //check if new voxel/last voxel has been found
                    if (updateFile)
                    {
                        //if loop is larger than 1 add "n" to symbolise a loop for loader
                        if (loop > 0)
                        {
                            writer.Write("n");
                            writer.Write(',');
                            writer.Write(loop);
                            writer.Write(',');
                        }
                        loop = 0;
                        //write the object ID
                        writer.Write(prevObjID);
                        //check if last voxel
                        if (lastVoxel)
                        {
                            //check if last voxel is not the same as previous obj
                            if (_section.voxels[a, b, c].objID != prevObjID)
                            {
                                //add current voxel to file
                                writer.Write(',');
                                writer.Write(_section.voxels[a, b, c].objID);
                            }
                        }
                        else
                        {
                            //else add spacer char
                            writer.Write(',');
                        }
                        //reset update file
                        updateFile = false;
                        //set prevObj to current obj
                        prevObjID = _section.voxels[a, b, c].objID;
                    }

                }
            }
        }

		writer.Close();
		voxGen.streamerOpen = false;

    }

    public static bool saveChunk(VoxelCreation voxGen, chunk _chunk)
    {

        //check if world data has been saved
        if (!voxGen.worldDataSaved)
        {
            saveWorldData(voxGen);
            voxGen.worldDataSaved = true;
        }

        //setup directory
        char split = Path.DirectorySeparatorChar;
        string dir = Application.dataPath + split + voxGen.directory + split;

        //check directory exists
        checkDirectoryExists(dir + split + _chunk.chunkPos.ToString());

        //check chunk data exists
        if (File.Exists(dir + _chunk.chunkPos.ToString() + split + "ChunkData.txt"))
        {
            File.Delete(dir + _chunk.chunkPos.ToString() + split + "ChunkData.txt");
        }

		while (voxGen.streamerOpen) {
		}
		voxGen.streamerOpen = true;

        //create new chunk data
        StreamWriter writer = new StreamWriter(dir + _chunk.chunkPos.ToString() + split + "ChunkData.txt");

        //check if starting height exists
        if (_chunk.yHeight != null)
        {

            //write starting height to file
            for (int a = 0; a < voxGen.sizeOfChunk.x; a++)
            {
                for (int b = 0; b < voxGen.sizeOfChunk.y; b++)
                {
                    writer.WriteLine(_chunk.yHeight[a, b]);
                }
            }

        }

        //write wether sections are required or not
        foreach (int i in _chunk.requiredSections)
        {
            writer.WriteLine(i);
        }

        writer.Close();
		voxGen.streamerOpen = false;

        //loop through every section and save it
        foreach (section sec in _chunk.sections)
        {
            saveSection(voxGen, sec);
        }

        return true;
    }

    static void checkDirectoryExists(string name)
    {
        if (!Directory.Exists(name))
        {
            Directory.CreateDirectory(name);
        }
    }
}
