﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeChunk : MonoBehaviour
{
     byte [] cubes;


    public CubeCoordinate ChunkCoordinate
    {
        get
        {
            return new CubeCoordinate(chunkX, chunkY, chunkZ);
        }
        set
        {
            chunkX = value.x;
            chunkY = value.y;
            chunkZ = value.z;
            transform.position = new Vector3(chunkX * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CUBE_SIDE_LENGTH,
                                             chunkY * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CUBE_SIDE_LENGTH,
                                             chunkZ * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CUBE_SIDE_LENGTH);

            transform.name = "("+chunkX +"," + chunkY + "," + chunkZ+")";

                                           
        }
    }


    public CubeMesh terrain, plan;
    [SerializeField]
    CubeChunk[] NeighborChunks;


    int chunkX, chunkY, chunkZ;

    public bool NeedRefresh,isActiveNow;



    public CubeGrid grid;


    Vector3[] tempVerticesArray;

    private void Awake()
    {
        cubes = new byte[CubeMetrics.CHUNK_WIDTH * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CHUNK_WIDTH];

        NeighborChunks = new CubeChunk[6];
        tempVerticesArray = new Vector3[8];
        enabled = false;

    }

    private void Start()
    {
       
    }

    public void Init()
    {
        NeedRefresh = false;
        isActiveNow = false;
        DisconnectNeighbors();
        transform.name = "New";
        enabled = false;
    }



   

    #region Data

    public void SetAllCubeData(byte[] datas)
    {
        cubes = datas;
    }

    void SetCubeData(int index,byte data ,bool refresh=false)
    {
        cubes[index] = data;
        if(refresh)
        {
            Refresh();
        }
    }

    public void SetCubeData(Vector3 cubePosition,byte data,bool refresh=false)
    {
        Vector3 p = CubeMetrics.WorldPosition2CubePosition(cubePosition);
        SetCubeData(new CubeCoordinate(p, CubeCoordinate.CoordinateType.cubeWorld), data,refresh);
    }

    /// <summary>
    /// Set from WorldCubePositon
    /// </summary>
    /// <param name="coordinate"></param>
    /// <param name="data"></param>
    public void SetCubeData(CubeCoordinate coordinate, byte data,bool refersh=false)
    {

        int index = CubeMetrics.GetCubeIndex_FromWorldToLocal(coordinate.x, coordinate.y, coordinate.z,
                                                           chunkX, chunkY, chunkZ);
        SetCubeData(index, data,refersh);
    }

    /// <summary>
    /// Set from localCubePosition
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="data"></param>
    public void SetCubeData(int x, int y, int z, byte data,bool refresh=false)
    {
        SetCubeData(CubeMetrics.GetCubeIndexToChunk(x, y, z), data,refresh);
    }

    //-----------------------------------------------------------------------------------

    public  byte[] GetAllCubeData()
    {
        return cubes;
    }

    byte GetCubeData(int index)
    {
        return cubes[index];
    }

    public byte GetCubeData(Vector3 cubePosition)
    {
        Vector3 p = CubeMetrics.WorldPosition2CubePosition(cubePosition);

        return GetCubeData(new CubeCoordinate(p, CubeCoordinate.CoordinateType.cubeWorld));

    }

    public byte GetCubeData(CubeCoordinate coordinate)
    {
        int index = CubeMetrics.GetCubeIndex_FromWorldToLocal(coordinate.x, coordinate.y, coordinate.z,
                                                           chunkX, chunkY, chunkZ);
        return GetCubeData(index);
    }

    public int GetCubeData(int x, int y, int z)
    {
        return cubes[CubeMetrics.GetCubeIndexToChunk(x, y, z)];
    }
    #endregion






    #region Mesh
    void TriangulateByData()
    {
        Clear();
        for (int y = 0; y < CubeMetrics.CHUNK_WIDTH; y++)
        {
            for (int z = 0; z < CubeMetrics.CHUNK_WIDTH; z++)
            {
                for (int x = 0; x < CubeMetrics.CHUNK_WIDTH; x++)
                {
                    byte d = cubes[x + y * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CHUNK_WIDTH + z * CubeMetrics.CHUNK_WIDTH];
                    CubeData data = CubeData.ToCubeData(d);
                    if (data.active)
                    {
                        CubeCoordinate coordinate = new CubeCoordinate(x, y, z);
                        TriangulateCube(x, y, z, data);
                    }
                }
            }
        }
        Apply();
    }

    void TriangulateCube(int x, int y, int z, CubeData data)
    {
        float cx = transform.position.x + x * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f;
        float cy = transform.position.y + y * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f;
        float cz = transform.position.z + z * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f;

        Vector3 cubePosition = new Vector3(cx, cy, cz) -(Vector3.one * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CUBE_SIDE_LENGTH / 2f);

        Vector3[] cubeVertices = GetCubeVertices(cubePosition, data.orientate);

  
        for (int i = 0; i < 6; i++)
        {
            int temp = i;
            if (temp < 4)
            {
                temp = (temp + (int)data.orientate) > 3 ? temp + (int)data.orientate - 4 : temp + (int)data.orientate;
            }

            if (!CheckAdjacent(x, y, z, (CubeSurface)temp,data.isTransparent))
            {
                TriangualteCubeSurface(cubeVertices, (CubeSurface)i, data);
            }
        }
    }

    void TriangualteCubeSurface(Vector3[] vertices, CubeSurface surfaceTo, CubeData data)
    {
        float uCoordinate = ((int)surfaceTo * 1.0f) / 6.0f;
        float vCoordinate = ((int)data.type * 1.0f) / 16f * 1.0f;
        Vector2 uvBasePoint = new Vector2(uCoordinate, vCoordinate);

        CubeMesh mesh = data.isTransparent ? plan : terrain;

     

        switch (surfaceTo)
        {
            case CubeSurface.up:
                mesh.AddQuad(vertices[0], vertices[1], vertices[2], vertices[3]);
                break;
            case CubeSurface.down:
                mesh.AddQuad(vertices[5], vertices[4], vertices[7], vertices[6]);
                break;
            case CubeSurface.left:
                mesh.AddQuad(vertices[0], vertices[3], vertices[7], vertices[4]);
                break;
            case CubeSurface.right:
                mesh.AddQuad(vertices[2], vertices[1], vertices[5], vertices[6]);
                break;
            case CubeSurface.front:
                mesh.AddQuad(vertices[1], vertices[0], vertices[4], vertices[5]);
                break;
            case CubeSurface.back:
                mesh.AddQuad(vertices[3], vertices[2], vertices[6], vertices[7]);
                break;
        }
        mesh.AddQuadUV(uvBasePoint, 16);
    }
    
    //
    //bool CheckAdjacent2(int x, int y, int z, CubeSurface surfaceTo)
    //{
    //    int minValue = 0;
    //    int maxValue = CubeMetrics.CHUNK_WIDTH - 1;
    //    CubeCoordinate c;
    //    switch (surfaceTo)
    //    {
    //        case CubeSurface.front:
    //            if (z + 2 > CubeMetrics.CHUNK_WIDTH)
    //            {
    //                c = ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.front);
    //                //return grid.chunkDatas.ContainsKey(c.ToString()) &&
    //                //    CubeData.ToCubeData(grid.chunkDatas[c.ToString()][CubeMetrics.GetCubeIndexToChunk(x, y, minValue)]).HasCube;
    //                //Debug.Log(grid.chunkDatas.ContainsKey(c.ToString()));
    //                return NeighborChunks[(int)AdjacentDirection.front] &&
    //               CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.front].
    //               cubes[CubeMetrics.GetCubeIndexToChunk(x, y, minValue)]).HasCube;
    //            }
                
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x, y, z + 1)]).HasCube;
    //        case CubeSurface.back:
    //            if (z - 1 < 0)
    //            {
    //                return NeighborChunks[(int)AdjacentDirection.back] &&
    //                 CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.back].
    //                 cubes[CubeMetrics.GetCubeIndexToChunk(x, y, maxValue)]).HasCube;
    //            }
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x, y, z - 1)]).HasCube;
    //        case CubeSurface.left:
    //            if (x - 1 < 0)
    //            {
    //                return NeighborChunks[(int)AdjacentDirection.left] &&
    //                CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.left].cubes[CubeMetrics.GetCubeIndexToChunk(maxValue, y, z)]).HasCube;
    //            }
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x - 1, y, z)]).HasCube;
    //        case CubeSurface.right:
    //            if (x + 2 > CubeMetrics.CHUNK_WIDTH)
    //            {
    //                return NeighborChunks[(int)AdjacentDirection.right] &&
    //                CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.right].cubes[CubeMetrics.GetCubeIndexToChunk(minValue, y, z)]).HasCube;
    //            }
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x + 1, y, z)]).HasCube;
    //        case CubeSurface.up:
    //            if (y + 2 > CubeMetrics.CHUNK_WIDTH)
    //            {
    //                return NeighborChunks[(int)AdjacentDirection.up] &&
    //                CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.up].cubes[CubeMetrics.GetCubeIndexToChunk(x, minValue, z)]).HasCube;
    //            }
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x, y + 1, z)]).HasCube;
    //        case CubeSurface.down:
    //            if (y - 1 < 0)
    //            {
    //                return NeighborChunks[(int)AdjacentDirection.down] &&
    //                CubeData.ToCubeData(NeighborChunks[(int)AdjacentDirection.down].cubes[CubeMetrics.GetCubeIndexToChunk(x, maxValue, z)]).HasCube;
    //            }
    //            return CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(x, y - 1, z)]).HasCube;
    //        default:
    //            return false;
    //    }
    //}

    //考虑未实例化到场景中的chunk, 直接在数据表里核对是否需要隐藏
    bool CheckAdjacent(int x, int y, int z, CubeSurface surfaceTo,bool isTransparent)
    {
        int tempX = x;
        int tempY = y;
        int tempZ = z;
        bool outOfRange = false;
        CubeCoordinate coordinate;

        switch(surfaceTo)
        {
            case CubeSurface.front:
                outOfRange = z + 2 > CubeMetrics.CHUNK_WIDTH;
                coordinate = ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.front);
                tempZ = outOfRange ? 0 : z + 1;
                break;
            case CubeSurface.back:
                outOfRange = z - 1 < 0;
                coordinate= ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.back);
                tempZ = outOfRange ? CubeMetrics.CHUNK_WIDTH - 1 : z - 1;
                break;
            case CubeSurface.left:
                outOfRange = x - 1 < 0;
                coordinate= ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.left);
                tempX = outOfRange ? CubeMetrics.CHUNK_WIDTH - 1 : x - 1;
                break;
            case CubeSurface.right:
                outOfRange = x + 2 > CubeMetrics.CHUNK_WIDTH;
                coordinate = ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.right);
                tempX = outOfRange ? 0 : x + 1;
                break;
            case CubeSurface.up:
                outOfRange = y + 2 > CubeMetrics.CHUNK_WIDTH;
                coordinate = ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.up);
                tempY = outOfRange ? 0 : y + 1;
                break;
            case CubeSurface.down:
                outOfRange = y - 1 < 0;
                coordinate = ChunkCoordinate.GetAdjacentCoordinate(AdjacentDirection.down);
                tempY= outOfRange ? CubeMetrics.CHUNK_WIDTH - 1 : y - 1;
                break;
            default:
                coordinate = ChunkCoordinate;
                break;
        }
        try
        {
            return outOfRange ?
            grid.chunkDatas.ContainsKey(coordinate.ToString()) &&
            CubeData.ToCubeData(grid.chunkDatas[coordinate.ToString()][CubeMetrics.GetCubeIndexToChunk(tempX, tempY, tempZ)]).HasCube(isTransparent) :
            CubeData.ToCubeData(cubes[CubeMetrics.GetCubeIndexToChunk(tempX, tempY, tempZ)]).HasCube(isTransparent);
        }
        catch
        {
            Debug.Log("chunk"+ChunkCoordinate.ToString()+":Surface"+surfaceTo+"index is error");
            return false;

        }
    }


    Vector3[] GetCubeVertices(Vector3 position, CubeOrientate orientate)
    {
        for (int i = 0; i < tempVerticesArray.Length; i++)
        {
            tempVerticesArray[i] = transform.InverseTransformPoint( position + CubeMetrics.GetCubeVertexByOrientate(i, orientate));
        }
        return tempVerticesArray;
    }

    void Clear()
    {
        terrain.Clear();
        plan.Clear();
    }

    void Apply()
    {
        terrain.Apply();
        plan.Apply();
    }

    #endregion


    public void ConnectNeighbors()
    {
        for (int i = 0; i < 6; i++)
        {
            if (!NeighborChunks[i])
            {
                AdjacentDirection chunkDir = (AdjacentDirection)i;
                CubeChunk neighbor = grid.GetAdjacentChunk(this, chunkDir);
                if (neighbor)
                {
                    NeighborChunks[i] = neighbor;
                    neighbor.NeighborChunks[(int)chunkDir.ChunkOpposite()] = this;
                }
            }
        }
    }

    public void DisconnectNeighbors()
    {
        for(int i=0;i<6;i++)
        {
            if(NeighborChunks[i])
            {
                NeighborChunks[i].NeighborChunks[(int)(((AdjacentDirection)i).ChunkOpposite())] = null;
                NeighborChunks[i] = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (NeedRefresh)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.white;
        }

        Gizmos.DrawWireCube(transform.position, Vector3.one * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CUBE_SIDE_LENGTH);
    }

    public void GeneratorTerrain()
    {
        cubes= CubeMetrics.GetTerrainData(this);
    }

    public void Refresh()
    {
        RefreshSelf();
        for(int i=0;i<NeighborChunks.Length;i++)
        {
            if(NeighborChunks[i])
            {
                NeighborChunks[i].RefreshSelf();
            }
        }

    }

    public void RefreshSelf()
    {
        enabled = true;  
    }

    public void SetVisible(bool b)
    {
        terrain.transform.GetComponent<Renderer>().enabled = b;
        plan.transform.GetComponent<Renderer>().enabled = b;
    }

    private void LateUpdate()
    {   
        TriangulateByData();
        enabled = false;
        NeedRefresh = false;
    }

    //private void OnDrawGizmosSelected()
    //{

    //    Gizmos.color = Color.blue;
    //    if (cubes != null)
    //    {
    //        for (int y = 0; y < CubeMetrics.CHUNK_WIDTH; y++)
    //        {
    //            for (int z = 0; z < CubeMetrics.CHUNK_WIDTH; z++)
    //            {
    //                for (int x = 0; x < CubeMetrics.CHUNK_WIDTH; x++)
    //                {
    //                    if (cubes[x + y * CubeMetrics.CHUNK_WIDTH * CubeMetrics.CHUNK_WIDTH + z * CubeMetrics.CHUNK_WIDTH] != 0)
    //                    {
    //                        Vector3 pos = new Vector3(transform.position.x + x * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f,
    //                                                transform.position.y + y * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f,
    //                                                transform.position.z + z * CubeMetrics.CUBE_SIDE_LENGTH + CubeMetrics.CUBE_SIDE_LENGTH / 2f);
    //                        Gizmos.DrawWireCube(pos - new Vector3(16f, 16f, 16f), Vector3.one * CubeMetrics.CUBE_SIDE_LENGTH);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
}
