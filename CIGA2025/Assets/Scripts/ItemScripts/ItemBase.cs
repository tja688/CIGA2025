using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ItemBase : MonoBehaviour, ISelectable
{
    public Bounds SelectionBounds { get; set; }

    private void Start()
    {
        SelectionBounds =  GetComponent<BoxCollider2D>().bounds;
    }
}
