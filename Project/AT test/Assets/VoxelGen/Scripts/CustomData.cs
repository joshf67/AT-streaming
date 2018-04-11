using System.Collections.Generic;
using UnityEngine;
using CusHolder;

//script that holds all custom data used in voxel creation

//class to store voxel data
[System.Serializable]
public class voxel
{
    public section parent;
    public GameObject obj;
    public Vector3 pos;
    public vec3I voxelPos;
    public bool visible;
    public bool destroyed;
    public bool destroyable;
    public int objID;

    public voxel()
    {
        parent = null;
        obj = null;
        pos = Vector3.zero;
        visible = false;
        destroyed = false;
        objID = 0;
        destroyable = true;
    }
}

//class to store chunk data
[System.Serializable]
public class chunk
{
    public double[,] yHeight;
    public List<int> requiredSections = new List<int>();
    public ThreadSafeList<section> sections = new ThreadSafeList<section>();
    public Vector2 chunkPos;
    public chunk left, right, forward, back;
    public int immune = 0;
    public int voxelDestroyTasksRemaining = 0;
    public bool beingDestroyed = false;

    public int checkSectionExists(int sectionNumber)
    {
        for (int a = 0; a < sections.Count; a++)
        {
            if (sections[a].sectionNum == sectionNumber)
            {
                return a;
            }
        }
        return -1;
    }

}

//class to store section data
[System.Serializable]
public class section
{
    public chunk parent;
    public int sectionNum;
    public voxel[,,] voxels;

    public section(int width, int length, int height, chunk _parent, int _sectionNum)
    {
        voxels = new voxel[width, length, height];
        sectionNum = _sectionNum;
        parent = _parent;
    }
}

//class to store voxel task data
[System.Serializable]
public class voxelTask
{
    public voxel vox = null;
    public bool visible = false;

    public voxelTask(voxel _vox, bool _show)
    {
        vox = _vox;
        visible = _show;
    }
}

//class to store chunk task data
[System.Serializable]
public class chunkTask
{
    public chunk _chunk = null;
    public bool visible = false;

    public chunkTask(chunk __chunk, bool _show)
    {
        _chunk = __chunk;
        visible = _show;
    }
}