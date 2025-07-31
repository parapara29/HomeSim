using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedPrefab
{
    public GameObject Prefab;
    public float Weight;
}

public class InfiniteMap : MonoBehaviour
{
    public int width = 3;
    public int height = 3;
    public float tileSize = 1f;
    public List<WeightedPrefab> prefabs;

    private GameObject[,] tiles;

    private void Start()
    {
        tiles = new GameObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = Instantiate(ChoosePrefab(), new Vector3(x * tileSize, 0f, y * tileSize), Quaternion.identity, transform);
            }
        }
    }

    private GameObject ChoosePrefab()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            return null;
        }

        float total = 0f;
        foreach (var p in prefabs)
        {
            total += Mathf.Max(0f, p.Weight);
        }

        float value = Random.Range(0f, total);
        foreach (var p in prefabs)
        {
            value -= Mathf.Max(0f, p.Weight);
            if (value <= 0f)
            {
                return p.Prefab;
            }
        }

        return prefabs[prefabs.Count - 1].Prefab;
    }

    private GameObject SwapPrefab(GameObject tile)
    {
        GameObject prefab = ChoosePrefab();
        if (prefab == null)
        {
            return tile;
        }

        Vector3 pos = tile.transform.position;
        Quaternion rot = tile.transform.rotation;
        Destroy(tile);
        return Instantiate(prefab, pos, rot, transform);
    }

    public void ShiftRight()
    {
        for (int y = 0; y < height; y++)
        {
            GameObject toMove = tiles[0, y];
            toMove = SwapPrefab(toMove);
            toMove.transform.position += new Vector3(width * tileSize, 0f, 0f);

            for (int x = 0; x < width - 1; x++)
            {
                tiles[x, y].transform.position -= new Vector3(tileSize, 0f, 0f);
                tiles[x, y] = tiles[x + 1, y];
            }

            tiles[width - 1, y] = toMove;
        }
    }

    public void ShiftLeft()
    {
        for (int y = 0; y < height; y++)
        {
            GameObject toMove = tiles[width - 1, y];
            toMove = SwapPrefab(toMove);
            toMove.transform.position -= new Vector3(width * tileSize, 0f, 0f);

            for (int x = width - 1; x > 0; x--)
            {
                tiles[x, y].transform.position += new Vector3(tileSize, 0f, 0f);
                tiles[x, y] = tiles[x - 1, y];
            }

            tiles[0, y] = toMove;
        }
    }

    public void ShiftUp()
    {
        for (int x = 0; x < width; x++)
        {
            GameObject toMove = tiles[x, 0];
            toMove = SwapPrefab(toMove);
            toMove.transform.position += new Vector3(0f, 0f, height * tileSize);

            for (int y = 0; y < height - 1; y++)
            {
                tiles[x, y].transform.position -= new Vector3(0f, 0f, tileSize);
                tiles[x, y] = tiles[x, y + 1];
            }

            tiles[x, height - 1] = toMove;
        }
    }

    public void ShiftDown()
    {
        for (int x = 0; x < width; x++)
        {
            GameObject toMove = tiles[x, height - 1];
            toMove = SwapPrefab(toMove);
            toMove.transform.position -= new Vector3(0f, 0f, height * tileSize);

            for (int y = height - 1; y > 0; y--)
            {
                tiles[x, y].transform.position += new Vector3(0f, 0f, tileSize);
                tiles[x, y] = tiles[x, y - 1];
            }

            tiles[x, 0] = toMove;
        }
    }
}