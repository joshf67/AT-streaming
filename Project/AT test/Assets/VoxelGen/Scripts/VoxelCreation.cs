using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using CusHolder;

public class VoxelCreation : MonoBehaviour
{

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

    public ThreadSafeList<chunk> chunks = new ThreadSafeList<chunk>();
    public bool enableDynamicChunkLoading = true;
    public Vector2 chunkRange = Vector2.zero;
    public bool allowForceLoad = false;

    public int opsPerVisibleUpdate = 0;
    public bool ignoreDestroyOps = false;
    public bool checkNeighboursVisibleOnCreation = false;

    public bool debugMode = false;

    public float perlinDist;
    public float randSeed;
    public float noise1Effect = 0;
    public float noise2Effect = 0;
    public int smoothing = 0;

    [Space(20)]
    [Header("Thread Variables")]
    //stores thread task data
    public ThreadSafeList<chunk> toCheckVisibility = new ThreadSafeList<chunk>();
    Thread currentChunkThread = null;
    public bool stopChunkThread = false;

    public ThreadSafeList<voxelTask> tasks = new ThreadSafeList<voxelTask>();

    public ThreadSafeList<chunk> destroyChunkTasks = new ThreadSafeList<chunk>();
    Thread currentChunkDestroyThread = null;
    public bool stopChunkDestroyThread = false;

    public ThreadSafeList<chunkTask> createChunkTasks = new ThreadSafeList<chunkTask>();
    Thread currentChunkCreateThread = null;
    public bool stopChunkCreateThread = false;

    public ThreadSafeList<Vector2> chunkSetupTasks = new ThreadSafeList<Vector2>();
    Thread currentSetupChunkThread = null;
    public bool stopSetupChunkThread = false;

    public ThreadSafeList<chunk> createChunkYHeightTasks = new ThreadSafeList<chunk>();
    public bool checkingChunkYHeight = false;

    public bool streamerOpen = false;

    [Space(20)]
    [Header("Voxel details")]
    public Vector3 voxelSize;
    public List<Material> voxelMaterial = new List<Material>();
    public List<GameObject> voxelObjects = new List<GameObject>();

    bool firstLoad = true;
    float resetSaveVar = 0;



    void Start()
    {
        //setup a random seed
        randSeed = UnityEngine.Random.Range(1, int.MaxValue);
        randSeed /= int.MaxValue;

        //load world data from file
        ChunkLoading.loadWorldData(this);

        //setup basic threads to allow initiall testing
        currentChunkThread = new Thread(chunkVisibleCheck);
        currentChunkThread.IsBackground = true;
        currentChunkThread.Abort();

        currentChunkDestroyThread = new Thread(deleteChunkVoxels);
        currentChunkDestroyThread.IsBackground = true;
        currentChunkThread.Abort();

        currentChunkCreateThread = new Thread(createChunk);
        currentChunkCreateThread.IsBackground = true;
        currentChunkThread.Abort();

        currentSetupChunkThread = new Thread(setupChunk);
        currentSetupChunkThread.IsBackground = true;
        currentSetupChunkThread.Abort();

    }

    void Update()
    {

        //save chunks on keydown enter
        if (Input.GetKeyDown(KeyCode.Return) && resetSaveVar <= 0)
        {
            saveChunks = true;
            resetSaveVar = 3;
        }

        //reset world
        if (Input.GetKeyDown(KeyCode.R))
        {
            char split = Path.DirectorySeparatorChar;
            string dir = Application.dataPath + split + directory + split;

            //delete world data
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            while (currentChunkThread.IsAlive || currentChunkDestroyThread.IsAlive || currentChunkCreateThread.IsAlive || currentSetupChunkThread.IsAlive)
            {
                OnApplicationQuit();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        //load world
        if (Input.GetKeyDown(KeyCode.L))
        {
            firstLoad = true;
        }

        if (resetSaveVar >= 0)
        {
            resetSaveVar -= Time.deltaTime;
        }

        //calculate the players position in chunk coords
        playerPos = new Vector2(Mathf.Floor((player.transform.position.x + (voxelSize.x / 2)) / (sizeOfChunk.x * voxelSize.x)),
            Mathf.Floor((player.transform.position.z + (voxelSize.z / 2)) / (sizeOfChunk.y * voxelSize.z)));

        //setup to delete list
        List<chunk> toDelete = new List<chunk>();

        //check if visibility check exists
        if (toCheckVisibility.Count != 0)
        {
            //checek if current visibility thread is active
            if (!currentChunkThread.IsAlive)
            {
                //make a new thread for visibility check
                currentChunkThread = new Thread(chunkVisibleCheck);
                currentChunkThread.IsBackground = true;
                currentChunkThread.Start();
            }
        }

        //check if setup chunk exists
        if (chunkSetupTasks.Count != 0)
        {
            //checek if setup chunk thread is active
            if (!currentSetupChunkThread.IsAlive)
            {
                //make a new thread for visibility check
                currentSetupChunkThread = new Thread(setupChunk);
                currentSetupChunkThread.IsBackground = true;
                currentSetupChunkThread.Start();
            }
        }

        //check if destroy chunk exists
        if (destroyChunkTasks.Count != 0)
        {
            //check if destroy thread is active
            if (!currentChunkDestroyThread.IsAlive)
            {
                //make a new thread for chunk destruction
                currentChunkDestroyThread = new Thread(deleteChunkVoxels);
                currentChunkDestroyThread.IsBackground = true;
                currentChunkDestroyThread.Start();
            }
        }

        //check if create chunk exists
        if (createChunkTasks.Count != 0)
        {
            //check if create thread is active
            if (!currentChunkCreateThread.IsAlive)
            {
                //make a new thread for chunk creation
                currentChunkCreateThread = new Thread(createChunk);
                currentChunkCreateThread.IsBackground = true;
                currentChunkCreateThread.Start();
            }
        }

        //set checking chunk height to true for thread saftey
        checkingChunkYHeight = true;
        //loop through chunks within tasks and generate y height
        foreach (chunk _chunk in createChunkYHeightTasks)
        {
            generateYHeight(_chunk);
        }
        //set checking chunk height to false for thread saftey
        checkingChunkYHeight = false;

        //check if dynamic loading is enabled
        if (enableDynamicChunkLoading)
        {

            //check if players position is no equal to previous
            if (player.transform.position != prevPos)
            {
                //check if chunks are still needed
                foreach (chunk _chunk in chunks)
                {
                    //check if chunk isn't being destroyed
                    if (!destroyChunkTasks.Contains(_chunk))
                    {
                        //check if chunk isn't within range of player
                        if (!checkWithinDistance(_chunk, playerPos))
                        {
                            //check if chunk is current immune to destruction
                            if (_chunk.immune <= 0)
                            {
                                //save the chunk data
                                ChunkSaving.saveChunk(this, _chunk);
                                //add the chunk to destroy task
                                destroyChunkTasks.Add(_chunk);
                                _chunk.beingDestroyed = true;
                                //reset the chunk neighbours
                                List<chunk> tempChunks = resetChunkNeighbours(_chunk);
                                toDelete.Add(_chunk);
                            }
                            else
                            {
                                //if immune minus immue by a tick
                                _chunk.immune--;
                            }
                        }
                    }
                }

                //remove all chunks not needed
                foreach (chunk _chunk in toDelete)
                {
                    //remove chunk from visiblity check and chunk list
                    toCheckVisibility.Remove(_chunk);
                    chunks.Remove(_chunk);
                }

                //generate new chunks based on player pos
                for (float a = -chunkRange.x; a <= chunkRange.x; a++)
                {
                    for (float b = -chunkRange.y; b <= chunkRange.y; b++)
                    {
                        //check if chunk within range doesn't exist
                        if (checkChunkExists(new Vector2(a, b) + playerPos) == -1)
                        {
                            if (!chunkSetupTasks.Contains(new Vector2(a, b)))
                            {
                                chunkSetupTasks.Add(new Vector2(a, b));
                            }
                        }
                    }
                }

            }

        }

        //update previous players position
        prevPos = player.transform.position;

        //loop for a force amount and check complete GameObject tasks
        int ops = 0;
        while (ops < opsPerVisibleUpdate && tasks.Count != 0)
        {
            ops++;
            //check if task isn't null
            if (tasks[0] != null)
            {
                //check if ignoring destroy ops
                if (ignoreDestroyOps)
                {
                    if (tasks[0].visible == false)
                    {
                        ops--;
                    }
                }
                //complete voxel visbility task
                voxelVisible(tasks[0].vox, tasks[0].visible);
            }
            else
            {
                //ignore count if task is null
                ops--;
            }
            //remove task
            tasks.RemoveAt(0);
        }

        //check if saving chunks is true
        if (saveChunks)
        {
            //loop through each chunk and save it
            foreach (chunk _chunk in chunks)
            {
                ChunkSaving.saveChunk(this, _chunk);
            }
            //disable chunk save
            saveChunks = false;
        }

        //change object to place
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (objectType > 0)
            {
                objectType--;
            }
        }

        //change object to place
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (objectType < voxelObjects.Count - 1)
            {
                objectType++;
            }
        }

        if (debugMode)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                Vector3 hitPos = hit.collider.transform.position;
                chunk tempChunk = PositionCalculations.posToChunk(this, hitPos);
                if (tempChunk != null)
                {
                    section tempSection = PositionCalculations.posToSection(this, hitPos, tempChunk);
                    if (tempSection != null)
                    {
                        vec3I voxelPos = PositionCalculations.posToVoxel(this, hitPos, tempSection);
                        debugVoxel(tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
                    }
                }
            }
        }

        //cause explosion
        if (Input.GetMouseButtonDown(2))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                explode(hit.point, explosionDistance);
            }
        }

        //destroy object
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            //find point of hit
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                Vector3 hitPos = hit.collider.transform.position;
                chunk tempChunk = PositionCalculations.posToChunk(this, hitPos);
                //force load chunk if hit unloaded chunk
                if (tempChunk == null)
                {
                    if (allowForceLoad)
                    {
                        tempChunk = ChunkLoading.forceChunkLoad(this, PositionCalculations.posToChunkPos(this, hitPos));
                    }
                }
                if (tempChunk != null)
                {
                    section tempSection = PositionCalculations.posToSection(this, hitPos, tempChunk);
                    //force load section if hit unloaded section
                    if (tempSection == null)
                    {
                        if (allowForceLoad)
                        {
                            tempSection = ChunkLoading.forceSectionLoad(this, PositionCalculations.posToSectionPos(this, hitPos, tempChunk), tempChunk);
                        }
                    }
                    if (tempSection != null)
                    {
                        //destroy voxel at hit position
                        vec3I voxelPos = PositionCalculations.posToVoxel(this, hitPos, tempSection);
                        destroyVoxel(tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
                    }
                }
            }
        }

        //place object
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            //find point of hit
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                //calculate position of side hit
                Vector3 hitPos = hit.collider.transform.position + returnSideHitPos(hit);
                chunk tempChunk = PositionCalculations.posToChunk(this, hitPos);
                //force load chunk if hit unloaded chunk
                if (tempChunk == null)
                {
                    if (allowForceLoad)
                    {
                        tempChunk = ChunkLoading.forceChunkLoad(this, PositionCalculations.posToChunkPos(this, hitPos));
                    }
                }
                if (tempChunk != null)
                {
                    section tempSection = PositionCalculations.posToSection(this, hitPos, tempChunk);
                    //force load section if hit unloaded section
                    if (tempSection == null)
                    {
                        if (allowForceLoad)
                        {
                            tempSection = ChunkLoading.forceSectionLoad(this, PositionCalculations.posToSectionPos(this, hitPos, tempChunk), tempChunk);
                        }
                    }
                    if (tempSection != null)
                    {
                        //place voxel object at hit position
                        vec3I voxelPos = PositionCalculations.posToVoxel(this, hitPos, tempSection);
                        placeObject(voxelPos, tempSection);
                    }
                }
            }
        }

        //setup players character on first load
        if (firstLoad)
        {
            firstLoad = false;
            player.GetComponent<Rigidbody>().isKinematic = false;
            player.transform.position = new Vector3(0, (sectionHeight * chunkSections * voxelSize.y) + 2, 0);
        }

    }

    void OnApplicationQuit()
    {
        //cause every thread to exit
        stopChunkThread = true;
        stopChunkDestroyThread = true;
        stopChunkCreateThread = true;
        stopSetupChunkThread = true;
    }

    //================ Creation functions =============//

    public void generateVoxel(section parent, vec3I pos, int objID)
    {
        //setup new voxel
        parent.voxels[pos.x, pos.z, pos.y] = new voxel();

        parent.voxels[pos.x, pos.z, pos.y].parent = parent;

        parent.voxels[pos.x, pos.z, pos.y].voxelPos = pos;

        //calculate voxel position
        parent.voxels[pos.x, pos.z, pos.y].pos = new Vector3(pos.x * voxelSize.x, pos.y * voxelSize.y, pos.z * voxelSize.z)
            + new Vector3(parent.parent.chunkPos.x * sizeOfChunk.x * voxelSize.x, sectionHeight * parent.sectionNum * voxelSize.y, parent.parent.chunkPos.y * sizeOfChunk.y * voxelSize.z);

        //check if voxel is air
        if (objID == 0)
        {
            //set destroyed to true
            parent.voxels[pos.x, pos.z, pos.y].destroyed = true;
            parent.voxels[pos.x, pos.z, pos.y].objID = 0;
        }
        else
        {
            //set voxel to objectID
            parent.voxels[pos.x, pos.z, pos.y].objID = objID;
        }

        //check if voxel is lowest point
        if (parent.voxels[pos.x, pos.z, pos.y].pos.y == 0)
        {
            //setup indestructable object
            parent.voxels[pos.x, pos.z, pos.y].destroyed = false;
            parent.voxels[pos.x, pos.z, pos.y].destroyable = false;
            parent.voxels[pos.x, pos.z, pos.y].objID = 1;
        }
    }

    //add check for neighbours
    void generateSectionVoxels(section _section)
    {

        //setup section
        _section.voxels = new voxel[sizeOfChunk.x, sizeOfChunk.y, sectionHeight];

        //wait until chunk has starting heights
        while (_section.parent.yHeight == null)
        {
            if (!checkingChunkYHeight)
            {
                if (!createChunkYHeightTasks.Contains(_section.parent))
                {
                    createChunkYHeightTasks.Add(_section.parent);
                }
            }
        }

        int yHeight = 0;

        //loop through every voxel in section
        for (int a = 0; a < sizeOfChunk.x; a++)
        {
            for (int b = 0; b < sizeOfChunk.y; b++)
            {
                for (int c = 0; c < sectionHeight; c++)
                {
                    yHeight = _section.sectionNum * sectionHeight;

                    //test if voxel is above starting height
                    if (yHeight + c > _section.parent.yHeight[a, b])
                    {
                        //create voxel with air
                        generateVoxel(_section, new vec3I(a, c, b), 0);
                    }
                    else
                    {
                        //test if voxel is 2 block below starting height
                        if (yHeight + c > _section.parent.yHeight[a, b] - 2)
                        {
                            //create voxel with sand
                            generateVoxel(_section, new vec3I(a, c, b), 2);
                        }
                        else
                        {
                            //create voxel with block
                            generateVoxel(_section, new vec3I(a, c, b), 1);
                        }
                    }

                }
            }
        }

    }

    public double[,] generateYHeight(chunk _chunk)
    {

        //store new yHeight of chunk
        double[,] yHeight = new double[sizeOfChunk.x, sizeOfChunk.y];

        //generate random height for the chunk
        for (int a = 0; a < sizeOfChunk.x; a++)
        {
            for (int b = 0; b < sizeOfChunk.y; b++)
            {
                float noise1 = Mathf.PerlinNoise(((_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x) + a) * perlinDist * randSeed, ((_chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z) + b) * perlinDist * randSeed);
                float noise2 = Mathf.PerlinNoise(UnityEngine.Random.Range(1, int.MaxValue) / UnityEngine.Random.Range(1, int.MaxValue), UnityEngine.Random.Range(1, int.MaxValue) / UnityEngine.Random.Range(1, int.MaxValue));
                yHeight[a, b] = ((noise1 * noise1Effect) - (noise2 * noise2Effect)) * (sectionHeight * chunkSections);
            }
        }

        //set chunk creation heights to yHeight
        _chunk.yHeight = yHeight;

        //smooth the chunks height
        smoothChunk(_chunk);

        return yHeight;

    }

    //function to check section voxels visibility
    public void sectionVisibileCheck(section _section)
    {

        //loop through all voxels
        for (int a = 0; a < sizeOfChunk.x; a++)
        {
            for (int b = 0; b < sizeOfChunk.y; b++)
            {
                for (int c = sectionHeight - 1; c >= 0; c--)
                {

                    //check if the voxel exists and isn't destroyed
                    if (_section.voxels[a, b, c] != null)
                    {
                        if (!_section.voxels[a, b, c].destroyed)
                        {
                            if (_section.voxels[a, b, c].objID != 0)
                            {

                                //check if the voxel is visible
                                if (testVoxel(_section.voxels[a, b, c], true))
                                {
                                    //add new visible task
                                    voxelTask temp = new voxelTask(_section.voxels[a, b, c], true);
                                    if (!tasks.Contains(temp))
                                    {
                                        tasks.Add(temp);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //function to place object
    void placeObject(vec3I voxelToChange, section _section)
    {
        voxel vox = _section.voxels[voxelToChange.x, voxelToChange.z, voxelToChange.y];

        //check if object selected isn't air
        if (objectType != 0)
        {

            //check if voxel is empty
            if (vox.destroyed)
            {

                //set voxel to current object
                vox.destroyed = false;
                vox.objID = objectType;
                voxelVisible(vox, testVoxel(vox));
            }

        }
    }

    //function to generate voxel object or delete it
    void voxelVisible(voxel _voxel, bool visible = false)
    {
        //check if voxel exists
        if (_voxel != null)
        {
            //check if voxel obj exists
            if (_voxel.obj != null)
            {
                if (visible == false)
                {
                    //check if voxel isn't visible and destroy it if it is.
                    _voxel.visible = false;
                    Destroy(_voxel.obj);
                }
                else
                {
                    //check if voxel is visible and enable it's object if it is hidden
                    _voxel.visible = true;
                    _voxel.obj.SetActive(visible);
                }
            }
            else
            {
                //create a new GameObject of type of object selected
                if (visible)
                {
                    if (!_voxel.parent.parent.beingDestroyed)
                    {
                        _voxel.visible = true;
                        _voxel.obj = GameObject.Instantiate(voxelObjects[_voxel.objID], _voxel.pos, voxelObjects[_voxel.objID].transform.rotation);
                        _voxel.obj.transform.localScale = voxelSize;
                    }
                }
            }
        }
    }

    //============== Destruction functions ===========//

    void deleteSectionVoxels(section _section)
    {
        for (int a = 0; a < sizeOfChunk.x; a++)
        {
            for (int b = 0; b < sizeOfChunk.y; b++)
            {
                for (int c = 0; c < sectionHeight; c++)
                {
                    if (_section.voxels[a, b, c] != null)
                    {
                        if (_section.voxels[a, b, c].visible)
                        {
                            tasks.Add(new voxelTask(_section.voxels[a, b, c], false));
                            _section.parent.voxelDestroyTasksRemaining++;
                        }
                    }
                }
            }
        }
    }

    //function that sets all voxels data to be destroyed
    void destroyVoxel(voxel _voxel)
    {
        if (_voxel != null)
        {
            if (_voxel.destroyable)
            {
                GameObject.Destroy(_voxel.obj);
                _voxel.destroyed = true;
                _voxel.visible = false;
                _voxel.objID = 0;

                foreach (voxel vox in findNeighbours(_voxel.pos, _voxel.parent))
                {
                    if (!vox.destroyed)
                    {
                        voxelVisible(vox, true);
                    }
                }
            }
        }
    }


    //================ Thread functions ===============//


    void deleteChunkVoxels()
    {

        while (destroyChunkTasks.Count != 0)
        {
            if (!stopChunkDestroyThread)
            {
                foreach (section sect in destroyChunkTasks[0].sections)
                {
                    deleteSectionVoxels(sect);
                }
                destroyChunkTasks[0].sections.Clear();
                destroyChunkTasks.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        Thread.CurrentThread.Abort();
        Thread.CurrentThread.Join();
    }

    //function to check all voxel visiblity
    void chunkVisibleCheck()
    {

        //loop untill visibility cound is 0
        while (toCheckVisibility.Count != 0)
        {
            //check stop chunk thread for thread saftey
            if (!stopChunkThread)
            {
                //check if chunk is being destroyed
                if (!toCheckVisibility[0].beingDestroyed)
                {
                    //store chunk section in temporary variable
                    ThreadSafeList<section> temp = toCheckVisibility[0].sections;
                    //loop through all sections and check their voxel visibility
                    for (int a = 0; a < temp.Count; a++)
                    {
                        sectionVisibileCheck(temp[a]);
                    }
                }
                //remove visibility task
                toCheckVisibility.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        //end current thread
        Thread.CurrentThread.Abort();
        Thread.CurrentThread.Join();

    }

    //function to generate chunks
    public void createChunk()
    {

        //sort chunks into closest first
        bool work = true;
        while (work)
        {
            work = false;

            float dist1;
            float dist2;

            for (int a = 0; a < createChunkTasks.Count - 1; a++)
            {
                dist1 = Vector2.Distance(createChunkTasks[a]._chunk.chunkPos, playerPos);
                dist2 = Vector2.Distance(createChunkTasks[a + 1]._chunk.chunkPos, playerPos);
                if (dist1 > dist2)
                {
                    work = true;
                    chunkTask temp = createChunkTasks[a];
                    createChunkTasks[a] = createChunkTasks[a + 1];
                    createChunkTasks[a + 1] = temp;
                }
            }
        }

        //loop for every chunk task
        while (createChunkTasks.Count != 0)
        {

            //check if stopChunk is false for thread saftey
            if (!stopChunkCreateThread)
            {

                //create all new sections in chunk task
                for (int a = 0; a < chunkSections; a++)
                {
                    createChunkTasks[0]._chunk.sections.Add(new section(sizeOfChunk.x, sizeOfChunk.y, sectionHeight, createChunkTasks[0]._chunk, a));
                }

                //generate all voxels in chunk sections
                for (int a = 0; a < chunkSections; a++)
                {
                    generateSectionVoxels(createChunkTasks[0]._chunk.sections[a]);
                }

                //check if chunk is visibile
                if (createChunkTasks[0].visible)
                {
                    //check if visiblity tasks contains current chunk
                    if (!toCheckVisibility.Contains(createChunkTasks[0]._chunk))
                    {
                        toCheckVisibility.Add(createChunkTasks[0]._chunk);
                    }
                }

                //loop through each neighbour and add them to check visbility
                foreach (chunk _chunk in setupChunkNeighbours(createChunkTasks[0]._chunk))
                {
                    if (createChunkTasks[0].visible)
                    {
                        if (checkNeighboursVisibleOnCreation)
                        {
                            if (!toCheckVisibility.Contains(_chunk))
                            {
                                toCheckVisibility.Add(_chunk);
                            }
                        }
                    }
                }

                //remove chunk task
                createChunkTasks.RemoveAt(0);
            }
            else
            {
                break;
            }

        }

        //exit thread
        Thread.CurrentThread.Abort();
        Thread.CurrentThread.Join();

    }

    void setupChunk()
    {

        //loop through each chunk above and generate new chunk
        while (chunkSetupTasks.Count != 0)
        {

            if (!stopSetupChunkThread)
            {

                //sort chunks into closest first
                bool work = true;
                while (work)
                {
                    work = false;

                    float dist1;
                    float dist2;

                    for (int a = 0; a < chunkSetupTasks.Count - 1; a++)
                    {
                        dist1 = Vector2.Distance(chunkSetupTasks[a], playerPos);
                        dist2 = Vector2.Distance(chunkSetupTasks[a + 1], playerPos);
                        if (dist1 > dist2)
                        {
                            work = true;
                            Vector2 temp = chunkSetupTasks[a];
                            chunkSetupTasks[a] = chunkSetupTasks[a + 1];
                            chunkSetupTasks[a + 1] = temp;
                        }
                    }
                }

                //setup new chunk
                chunks.Add(new chunk());

                //try to load chunk from file
                List<chunk> newVisibilityCheck = ChunkLoading.loadChunk(this, chunkSetupTasks[0] + playerPos, chunks[chunks.Count - 1]);

                //check if chunk didn't load
                if ((newVisibilityCheck.Count == 0 && chunkSetupTasks[0] != Vector2.zero) || chunks[chunks.Count - 1].sections.Count == 0)
                {

                    //setup chunk position
                    chunks[chunks.Count - 1].chunkPos = chunkSetupTasks[0] + playerPos;

                    //add chunk to create task
                    createChunkTasks.Add(new chunkTask(chunks[chunks.Count - 1], true));

                }
                else
                {

                    //if chunk loaded add neighbours to visbility task
                    foreach (chunk _chunk in newVisibilityCheck)
                    {
                        if (!toCheckVisibility.Contains(_chunk))
                        {
                            toCheckVisibility.Add(_chunk);
                        }
                    }

                }

                chunkSetupTasks.RemoveAt(0);

            }
            else
            {
                break;
            }

        }

        //exit thread
        Thread.CurrentThread.Abort();
        Thread.CurrentThread.Join();
    }

    //=================== Misc functions =============//

    //function to reset chunk neighbours data
    List<chunk> resetChunkNeighbours(chunk _chunk)
    {
        List<chunk> visibilityReset = new List<chunk>();
        if (_chunk.left != null)
        {
            _chunk.left.right = null;
            visibilityReset.Add(_chunk.left);
        }

        if (_chunk.right != null)
        {
            _chunk.right.left = null;
            visibilityReset.Add(_chunk.right);
        }

        if (_chunk.forward != null)
        {
            _chunk.forward.back = null;
            visibilityReset.Add(_chunk.forward);
        }

        if (_chunk.back != null)
        {
            _chunk.back.forward = null;
            visibilityReset.Add(_chunk.back);
        }
        return visibilityReset;
    }

    //function to check distance to chunk position
    bool checkWithinDistance(chunk _chunk, Vector2 otherChunkPos)
    {

        if (otherChunkPos.x + chunkRange.x < _chunk.chunkPos.x || otherChunkPos.x - chunkRange.x > _chunk.chunkPos.x ||
            otherChunkPos.y + chunkRange.y < _chunk.chunkPos.y || otherChunkPos.y - chunkRange.y > _chunk.chunkPos.y)
        {
            return false;
        }
        return true;
    }

    public List<chunk> setupChunkNeighbours(chunk _chunk)
    {

        List<chunk> visibilityCheck = new List<chunk>();

        //setup neighbours, check if chunk exists then set neighbour data
        //left
        int left = checkChunkExists(_chunk.chunkPos + new Vector2(-1, 0));
        if (left != -1)
        {
            _chunk.left = chunks[left];
            chunks[left].right = _chunk;
            visibilityCheck.Add(chunks[left]);
        }

        //right
        int right = checkChunkExists(_chunk.chunkPos + new Vector2(1, 0));
        if (right != -1)
        {
            _chunk.right = chunks[right];
            chunks[right].left = _chunk;
            visibilityCheck.Add(chunks[right]);
        }

        //forward
        int forward = checkChunkExists(_chunk.chunkPos + new Vector2(0, 1));
        if (forward != -1)
        {
            _chunk.forward = chunks[forward];
            chunks[forward].back = _chunk;
            visibilityCheck.Add(chunks[forward]);
        }

        //back
        int back = checkChunkExists(_chunk.chunkPos + new Vector2(0, -1));
        if (back != -1)
        {
            _chunk.back = chunks[back];
            chunks[back].forward = _chunk;
            visibilityCheck.Add(chunks[back]);
        }

        return visibilityCheck;

    }

    void smoothChunk(chunk _chunk)
    {

        //smooth times
        for (int sm = 0; sm < smoothing; sm++)
        {

            //smooth random height
            for (int a = 0; a < sizeOfChunk.x; a++)
            {
                for (int b = 0; b < sizeOfChunk.y; b++)
                {
                    double height = _chunk.yHeight[a, b];
                    int sides = 1;

                    //check if within chunk bounds
                    if (a > 0)
                    {
                        height += _chunk.yHeight[a - 1, b];
                        sides++;

                        if (b < sizeOfChunk.y - 1)
                        {
                            height += _chunk.yHeight[a - 1, b + 1];
                            sides++;
                        }

                        if (b > 0)
                        {
                            height += _chunk.yHeight[a - 1, b - 1];
                            sides++;
                        }

                    }
                    else
                    {
                        //else check if neighbour chunk exists
                        if (_chunk.left != null)
                        {
                            //check if neighbour chunk height exists
                            if (_chunk.left.yHeight != null)
                            {
                                //add average height
                                height += _chunk.left.yHeight[sizeOfChunk.x - 1, b];
                                sides++;
                            }
                        }
                    }

                    //check if within chunk bounds
                    if (a < sizeOfChunk.x - 1)
                    {
                        height += _chunk.yHeight[a + 1, b];
                        sides++;

                        if (b < sizeOfChunk.y - 1)
                        {
                            height += _chunk.yHeight[a + 1, b + 1];
                            sides++;
                        }

                        if (b > 0)
                        {
                            height += _chunk.yHeight[a + 1, b - 1];
                            sides++;
                        }
                    }
                    else
                    {
                        //else check if neighbour chunk exists
                        if (_chunk.right != null)
                        {
                            //check if neighbour chunk height exists
                            if (_chunk.right.yHeight != null)
                            {
                                //add average height
                                height += _chunk.right.yHeight[0, b];
                                sides++;
                            }
                        }
                    }

                    //check if within chunk bounds
                    if (b > 0)
                    {
                        height += _chunk.yHeight[a, b - 1]; ;
                        sides++;
                    }
                    else
                    {
                        //else check if neighbour chunk exists
                        if (_chunk.forward != null)
                        {
                            //check if neighbour chunk height exists
                            if (_chunk.forward.yHeight != null)
                            {
                                //add average height
                                height += _chunk.forward.yHeight[a, 0];
                                sides++;
                            }
                        }
                    }

                    //check if within chunk bounds
                    if (b < sizeOfChunk.y - 1)
                    {
                        height += _chunk.yHeight[a, b + 1]; ;
                        sides++;
                    }
                    else
                    {
                        //else check if neighbour chunk exists
                        if (_chunk.back != null)
                        {
                            //check if neighbour chunk height exists
                            if (_chunk.back.yHeight != null)
                            {
                                //add average height
                                height += _chunk.back.yHeight[a, sizeOfChunk.y - 1];
                                sides++;
                            }
                        }
                    }

                    //setup height average
                    _chunk.yHeight[a, b] = height / sides;
                }
            }
        }
    }

    //check if chunk exists and return index in array
    public int checkChunkExists(Vector2 input)
    {
        //loop through all chunks and return chunk index
        for (int a = 0; a < chunks.Count; a++)
        {
            if (input == chunks[a].chunkPos)
            {
                return a;
            }
        }
        return -1;
    }

    //function to check if position is within chunk bounds
    bool testVoxelPosInBounds(vec3I input)
    {
        if (input.x < 0 || input.x > sizeOfChunk.x - 1 ||
            input.y < 0 || input.y > (sectionHeight * chunkSections) + sectionHeight - 1 ||
            input.z < 0 || input.z > sizeOfChunk.y - 1)
        {
            return false;
        }
        return true;
    }

    //function to cause explosion (destroy of multiple blocks)
    public void explode(Vector3 position, float explosionDist)
    {
        //loop through every possible place of explosion
        for (float a = -explosionDist; a < explosionDist; a++)
        {
            for (float b = -explosionDist; b < explosionDist; b++)
            {
                for (float c = -explosionDist; c < explosionDist; c++)
                {
                    //check if distance is within range
                    if (Vector3.Distance(position + CusMaths.Functions.vec3Times(new Vector3(a, b, c), voxelSize), position) < explosionDist)
                    {
                        Vector3 hitPos = position + CusMaths.Functions.vec3Times(new Vector3(a, b, c), voxelSize);
                        //calculate chunk position of hit 
                        chunk tempChunk = PositionCalculations.posToChunk(this, hitPos);
                        //force load chunk if it doesn't exist
                        if (tempChunk == null)
                        {
                            if (allowForceLoad)
                            {
                                tempChunk = ChunkLoading.forceChunkLoad(this, PositionCalculations.posToChunkPos(this, hitPos));
                            }
                        }
                        if (tempChunk != null)
                        {
                            section tempSection = PositionCalculations.posToSection(this, hitPos, tempChunk);
                            //force load section if it doesn't exist
                            if (tempSection == null)
                            {
                                if (allowForceLoad)
                                {
                                    tempSection = ChunkLoading.forceSectionLoad(this, PositionCalculations.posToSectionPos(this, hitPos, tempChunk), tempChunk);
                                }
                            }
                            if (tempSection != null)
                            {
                                vec3I voxelPos = PositionCalculations.posToVoxel(this, hitPos, tempSection);
                                //test hit is within bounds
                                if (testVoxelPosInBounds(voxelPos))
                                {
                                    if (tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y] != null)
                                    {
                                        //check if current explosion isn't at current position
                                        if (position + CusMaths.Functions.vec3Times(new Vector3(a, b, c), voxelSize) != position)
                                        {
                                            //check if voxel at position is tnt and if so blow it up
                                            if (tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y].obj != null)
                                            {
                                                if (tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y].obj.name == "TNT")
                                                {
                                                    tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y].obj.GetComponent<TNT>().trigger();
                                                }
                                            }
                                        }
                                        //destroy the current voxel
                                        destroyVoxel(tempSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void debugVoxel(voxel _voxel)
    {
        //display basic debugging data
        string debugOutput = null;

        if (_voxel != null)
        {

            GetComponent<LineRenderer>().SetPosition(0, player.transform.position);
            GetComponent<LineRenderer>().SetPosition(1, _voxel.pos);

            debugOutput += "Voxel Data:\n";
            debugOutput += "Voxel Name: " + voxelObjects[_voxel.objID].name;
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

        }

        debugOutput += "\n\nPlayer Data:";
        debugOutput += "\nObject Equipped: " + voxelObjects[objectType].name;
        debugOutput += "\nPress Left to Change Block";
        debugOutput += "\nPress Right to Change Block";

        debugOutput += "\nPress Enter to save: ";
        if (resetSaveVar >= 0)
        {
            debugOutput += "Saved";
        }

		debugOutput += "\nPress R to reset level";
		debugOutput += "\nPress L to reset position";

        GameObject.FindObjectOfType<Text>().text = debugOutput;
    }

    void OnDrawGizmos()
    {
        //draw basic debugging data for player
        foreach (chunk _chunk in chunks)
        {
            if (Vector2.Distance(_chunk.chunkPos, playerPos) < 2)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                if (Vector2.Distance(_chunk.chunkPos, playerPos) < 3)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.red;
                }
            }
            //draw chunks
            Gizmos.DrawWireCube(new Vector3(((_chunk.chunkPos.x * sizeOfChunk.x * voxelSize.x) + (sizeOfChunk.x / 2 * voxelSize.x)) - (voxelSize.x / 2),
                ((sectionHeight * chunkSections) * voxelSize.y) / 2 - (voxelSize.y / 2),
                ((_chunk.chunkPos.y * sizeOfChunk.y * voxelSize.z) + (sizeOfChunk.y / 2 * voxelSize.z)) - (voxelSize.z / 2)),
                new Vector3(sizeOfChunk.x * voxelSize.x, sectionHeight * chunkSections * voxelSize.y, sizeOfChunk.y * voxelSize.z));
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(player.transform.position, player.transform.position + new Vector3(0, chunkSections * sectionHeight, 0));
    }

    //calculate based on normal of object hit which side has been hit
    Vector3 returnSideHitPos(RaycastHit hit)
    {
        Vector3 rayNormal = hit.normal;
        Transform trans = hit.transform;

        rayNormal = trans.TransformDirection(rayNormal);

        Vector3 returnVal = Vector3.zero;

        if (rayNormal == trans.up)
        {
            returnVal.y = voxelSize.y;
        }
        else if (rayNormal == -trans.up)
        {
            returnVal.y = -voxelSize.y;
        }
        else if (rayNormal == trans.right)
        {
            returnVal.x = voxelSize.x;
        }
        else if (rayNormal == -trans.right)
        {
            returnVal.x = -voxelSize.x;
        }
        else if (rayNormal == trans.forward)
        {
            returnVal.z = voxelSize.z;
        }
        else if (rayNormal == -trans.forward)
        {
            returnVal.z = -voxelSize.z;
        }

        return returnVal;
    }

    //function to test neighbour voxel to see if this voxel is visible
    bool testVoxel(voxel gridVoxel, bool ignoreSections = false)
    {
        //find voxel neighbours
        List<voxel> neighbours = findNeighbours(gridVoxel.pos, gridVoxel.parent);


        if (ignoreSections)
        {
            //calculate how many ignore blocks should be used
            int ignoreCount = 6;

            if (gridVoxel.voxelPos.x == 0)
            {
                ignoreCount--;
            }

            if (gridVoxel.voxelPos.x == sizeOfChunk.x - 1)
            {
                ignoreCount--;
            }

            if (gridVoxel.voxelPos.z == 0)
            {
                ignoreCount--;
            }

            if (gridVoxel.voxelPos.z == sizeOfChunk.y - 1)
            {
                ignoreCount--;
            }

            if (gridVoxel.voxelPos.y == 0)
            {
                ignoreCount--;
            }

            if (gridVoxel.voxelPos.y == sectionHeight - 1)
            {
                ignoreCount--;
            }

            //check if the number of neighbours is less than ignore count 
            if (neighbours.Count < ignoreCount)
            {
                return true;
            }
        }
        else
        {
            //check if any neighbour is empty
            if (neighbours.Count != 6)
            {
                return true;
            }
        }

        //loop through each voxel and check if it is air or empty
        foreach (voxel vox in neighbours)
        {
            if (vox.destroyed || vox.objID == 0)
            {
                return true;
            }
        }

        return false;
    }

    //find Neighbours for sections
    public List<voxel> findNeighbours(Vector3 pos, section _section = null)
    {
        List<voxel> returnVoxels = new List<voxel>();
        section voxelSection = _section;
        //check voxel section exists
        if (voxelSection == null)
        {
            voxelSection = PositionCalculations.posToSection(this, pos, _section.parent);
            if (voxelSection == null)
            {
                return null;
            }
        }
        vec3I voxelPos = PositionCalculations.posToVoxel(this, pos, voxelSection);

        //test left
        if (voxelPos.x > 0)
        {
            //check if neighbour isn't empty
            if (_section.voxels[voxelPos.x - 1, voxelPos.z, voxelPos.y] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x - 1, voxelPos.z, voxelPos.y]);
            }
        }
        else
        {
            //check if neighbour chunk exists
            if (voxelSection.parent.left != null)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.left.checkSectionExists(_section.sectionNum);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(sizeOfChunk.x - 1, voxelPos.y, voxelPos.z);

                    voxel tempVox = voxelSection.parent.left.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        //test right
        if (voxelPos.x < sizeOfChunk.x - 1)
        {
            //check if neighbour isn't empty
            if (voxelSection.voxels[voxelPos.x + 1, voxelPos.z, voxelPos.y] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x + 1, voxelPos.z, voxelPos.y]);
            }
        }
        else
        {
            //check if neighbour chunk exists
            if (voxelSection.parent.right != null)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.right.checkSectionExists(_section.sectionNum);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(0, voxelPos.y, voxelPos.z);

                    voxel tempVox = voxelSection.parent.right.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        //test back
        if (voxelPos.z > 0)
        {
            //check if neighbour isn't empty
            if (voxelSection.voxels[voxelPos.x, voxelPos.z - 1, voxelPos.y] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x, voxelPos.z - 1, voxelPos.y]);
            }
        }
        else
        {
            //check if neighbour chunk exists
            if (voxelSection.parent.back != null)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.back.checkSectionExists(_section.sectionNum);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(voxelPos.x, voxelPos.y, sizeOfChunk.y - 1);

                    voxel tempVox = voxelSection.parent.back.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        //test forward
        if (voxelPos.z < sizeOfChunk.y - 1)
        {
            //check if neighbour isn't empty
            if (voxelSection.voxels[voxelPos.x, voxelPos.z + 1, voxelPos.y] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x, voxelPos.z + 1, voxelPos.y]);
            }
        }
        else
        {
            //check if neighbour chunk exists
            if (voxelSection.parent.forward != null)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.forward.checkSectionExists(_section.sectionNum);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(voxelPos.x, voxelPos.y, 0);

                    voxel tempVox = voxelSection.parent.forward.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        //test down
        if (voxelPos.y > 0)
        {
            //check if neighbour isn't empty
            if (voxelSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y - 1] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y - 1]);
            }
        }
        else
        {
            //check if neighbour section exists
            if (voxelSection.sectionNum != 0)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.checkSectionExists(_section.sectionNum - 1);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(voxelPos.x, sectionHeight - 1, voxelPos.z);

                    voxel tempVox = voxelSection.parent.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        //test up
        if (voxelPos.y < sectionHeight - 1)
        {
            //check if neighbour isn't empty
            if (voxelSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y + 1] != null)
            {
                returnVoxels.Add(voxelSection.voxels[voxelPos.x, voxelPos.z, voxelPos.y + 1]);
            }
        }
        else
        {
            //check if neighbour section exists
            if (voxelSection.sectionNum != chunkSections - 1)
            {
                //check if neighbour isn't empty
                int otherVoxelSection = _section.parent.checkSectionExists(_section.sectionNum + 1);
                if (otherVoxelSection != -1)
                {
                    vec3I tempVoxelPos = new vec3I(voxelPos.x, 0, voxelPos.z);

                    voxel tempVox = voxelSection.parent.sections[otherVoxelSection].voxels[tempVoxelPos.x, tempVoxelPos.z, tempVoxelPos.y];
                    if (tempVox != null)
                    {
                        returnVoxels.Add(tempVox);
                    }
                }
            }
        }

        return returnVoxels;
    }

}