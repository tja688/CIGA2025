
using UnityEngine;

public class TestItem : ItemBase
{

    public override void OnActivate()
    {
        base.OnActivate();
        Debug.Log("OnActivate"+this.name);
    }
}
