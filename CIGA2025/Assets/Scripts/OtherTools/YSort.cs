// YSort.cs
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public float yOffset = 0f; 
    public bool isStatic = false; 
    public int precisionMultiplier = 100;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (isStatic)
        {
            UpdateSortingOrder();
        }
    }

    private void LateUpdate() 
    {
        if (!isStatic)
        {
            UpdateSortingOrder();
        }
    }

    private void UpdateSortingOrder()
    {
        var yPositionForSorting = transform.position.y + yOffset;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-yPositionForSorting * precisionMultiplier);
    }
}