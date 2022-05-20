using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatchouliFloat : MonoBehaviour {

    DateTime created = DateTime.Now;
    Vector3 initialPosition;
    [SerializeField] float maxHeightDiff = 100;
    [SerializeField] float floatSpeed = .1f;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = initialPosition + new Vector3(0, Mathf.Sin((float)(DateTime.Now - created).TotalMilliseconds * floatSpeed) * maxHeightDiff, 0);
    }
}
