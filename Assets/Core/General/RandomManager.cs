// ... existing code ...
using UnityEngine;

public class RandomManager : Singleton<RandomManager>
{
    private int _seed;


    public void LoadSeed(int seed)
    {
        _seed = seed;
        Random.InitState(_seed);
    }

    public void GenerateSeed()
    {
        _seed = Random.Range(0, 1000000);
        Random.InitState(_seed);

    }

    public int Range(int min, int max)
    {
        return Random.Range(min, max);
    }

    public float Range(float min, float max)
    {
        return Random.Range(min, max);
    }

    public bool Chance(float probability)
    {
        return Random.value < probability;
    }
}