using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class SizeUI : MonoBehaviour
{
    public List<RectTransform> rectTransform=new();
    private Vector2 _size;
    public int x;
    public int z;
    void Start()
    {
        rectTransform = GetComponentsInChildren<RectTransform>().ToList();
        
    }

    [Button]
    public void GetComp()
    {
        rectTransform = GetComponentsInChildren<RectTransform>().ToList();
    }
    // Update is called once per frame
    void Update()
    {
                
                
        _size = new Vector2(x, z);
        foreach (RectTransform rect in rectTransform)
        {   
            if(rect!=GetComponent<RectTransform>())
            rect.sizeDelta = _size;
            
        }
    }
}
