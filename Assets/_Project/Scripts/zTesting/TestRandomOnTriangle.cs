using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRandomOnTriangle : MonoBehaviour
{
    public GameObject _CubeObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        List<Vector3> limitPoint = new List<Vector3>();
        for (int i = 0; i < this.transform.childCount; i++)
        {
            limitPoint.Add(this.transform.GetChild(i).position);
        }

        for (int i = 0; i < 2000; i++)
        {
            float x = 0;
            float z = 0;

            (x, z) = StaticFunction.RandomPointOnPolygon(limitPoint.ToArray());

            Vector3 candidate = new Vector3(x, transform.position.y, z);

            GameObject.Instantiate(_CubeObj, candidate, Quaternion.identity);
            yield return new WaitForSeconds(8f);
        }
    }
}

