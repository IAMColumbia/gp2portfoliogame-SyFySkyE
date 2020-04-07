﻿/*
 * Based off of David Crook's Object Pooling From https://blogs.msdn.microsoft.com/dave_crooks_dev_blog/2014/07/21/object-pooling-for-unity3d/
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecePool 
{
    private static volatile PiecePool instance;
    private static object syncRoot = new System.Object();

    // Collection of stored Pieces.
    private List<NormalPiece> pooledPieces;

    // Pool Size Parameters
    private int initialPoolSize;
    private int maxPoolSize;

    // Copies piece array in case we need to increase object pool
    private NormalPiece normalPiece; // TODO Should we make it so we never need this? Ie, if we fill the pool with say, thirty pieces, will we ever need to refill the pool with new Instantiates?

    public GameObject PoolObject { get; internal set; }

    public static PiecePool Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new PiecePool();
                    }
                }
            }
            return instance;
        }
    }

    private PiecePool() { }

    public void CreatePool(NormalPiece piece, int initialPoolSize, int maxPoolSize)
    {
        pooledPieces = new List<NormalPiece>();
        normalPiece = piece;
        for (int i = 0; i < initialPoolSize; i++)
        {
            Debug.Log("Instantiated Piece");
            NormalPiece newPiece = GameObject.Instantiate(normalPiece, Vector3.zero, Quaternion.identity);

            pooledPieces.Add(newPiece);
            newPiece.gameObject.SetActive(false);            

            GameObject.DontDestroyOnLoad(newPiece);
        }

        this.initialPoolSize = initialPoolSize;
        this.maxPoolSize = maxPoolSize;        
    }

    public NormalPiece GetObject()
    {
        for (int i = 0; i < pooledPieces.Count; i++)
        {
            pooledPieces[i].gameObject.SetActive(true);            
            pooledPieces[i].SetupPiece();
            pooledPieces.Remove(pooledPieces[i]);
            return pooledPieces[i];
        }

        if (this.maxPoolSize > this.pooledPieces.Count)
        {
            Debug.LogWarning("Instantiated Piece, pool too small");
            NormalPiece newPiece = GameObject.Instantiate(normalPiece, Vector3.zero, Quaternion.identity);
            newPiece.gameObject.SetActive(true);
            return newPiece;
        }
        Debug.LogError("Cannot retrieve piece from pool");
        return null;
    }

    public void AddPieceBackToPool(NormalPiece currentPiece)
    {
        currentPiece.transform.SetParent(PiecePool.Instance.PoolObject.transform);
        currentPiece.SetupPiece();
        currentPiece.gameObject.SetActive(false);        
        if (this.maxPoolSize > this.pooledPieces.Count)        
        {
            Debug.Log("Piece Added Back to Pool");
            pooledPieces.Add(currentPiece);
        }
        else
        {
            Debug.LogError("Tried adding piece back to pool but pool is full! Destroying object...");
            GameObject.Destroy(currentPiece);
        }
    }
}
